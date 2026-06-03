// Camera-based indicator detection for BlinkTalk.
//
// Uses the WebView's getUserMedia for the live feed and Google MediaPipe FaceLandmarker
// (WASM) for per-frame face "blendshape" scores. Blendshapes include ready-made eye signals
// such as eyeLookUpLeft/Right, eyeBlinkLeft/Right, eyeLookDown/In/Out, browInnerUp, etc., so a
// user can indicate by looking up, blinking, a wink, a brow raise — whatever they can reliably
// repeat. Training picks the blendshape whose score separates most between "relaxed" and
// "indicating", and detection fires when that score crosses the learned threshold.
//
// NOTE: the MediaPipe runtime and model are loaded from a CDN on first use (cached by the
// WebView afterwards). For fully offline use these should be bundled into wwwroot — see CLAUDE.md.

const VISION_VERSION = "0.10.20";
const VISION_MODULE = `https://cdn.jsdelivr.net/npm/@mediapipe/tasks-vision@${VISION_VERSION}/vision_bundle.mjs`;
const VISION_WASM = `https://cdn.jsdelivr.net/npm/@mediapipe/tasks-vision@${VISION_VERSION}/wasm`;
const MODEL_URL = "https://storage.googleapis.com/mediapipe-models/face_landmarker/face_landmarker/float16/1/face_landmarker.task";

let landmarker = null;
let stream = null;
let video = null;
let running = false;
let rafId = 0;
let latest = null;            // most recent blendshape categories [{categoryName, score}]

let mode = "preview";          // "preview" | "detect"
let detect = null;             // { signal, threshold, refractoryMs }
let dotnetRef = null;
let wasAbove = false;
let refractoryUntil = 0;

// Start the camera and the face landmarker, attaching the feed to the given <video> element.
// Returns true on success; throws (string) on failure so the caller can surface it.
export async function start(videoEl, ref) {
    dotnetRef = ref;
    video = videoEl;

    if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
        throw "Camera API (getUserMedia) is not available in this WebView.";
    }

    const vision = await import(VISION_MODULE);
    const fileset = await vision.FilesetResolver.forVisionTasks(VISION_WASM);
    landmarker = await vision.FaceLandmarker.createFromOptions(fileset, {
        baseOptions: { modelAssetPath: MODEL_URL },
        runningMode: "VIDEO",
        outputFaceBlendshapes: true,
        numFaces: 1
    });

    stream = await navigator.mediaDevices.getUserMedia({ video: { facingMode: "user" }, audio: false });
    video.srcObject = stream;
    await video.play();

    running = true;
    loop();
    return true;
}

function loop() {
    if (!running) return;
    try {
        if (video && video.readyState >= 2) {
            const result = landmarker.detectForVideo(video, performance.now());
            const faces = result && result.faceBlendshapes;
            latest = faces && faces.length ? faces[0].categories : null;

            if (mode === "detect" && detect && latest) {
                const cat = latest.find(c => c.categoryName === detect.signal);
                const score = cat ? cat.score : 0;
                const now = performance.now();
                const above = score >= detect.threshold;
                // Rising edge + refractory window so one gesture = one indication.
                if (above && !wasAbove && now >= refractoryUntil) {
                    refractoryUntil = now + (detect.refractoryMs || 800);
                    beep(1046, 110); // immediate audible confirmation (eyes may be off-screen)
                    if (dotnetRef) dotnetRef.invokeMethodAsync("OnCameraIndicated");
                }
                wasAbove = above;
            }
        }
    } catch (e) {
        // Keep the loop alive across transient per-frame errors.
        console.error("blinktalk-camera loop error", e);
    }
    rafId = requestAnimationFrame(loop);
}

// Whether a face is currently detected (used to give training feedback).
export function faceDetected() {
    return !!latest;
}

// The current score (0..1) for a given blendshape signal — used to show a live meter while detecting.
export function currentScore(signal) {
    if (!latest) return 0;
    const cat = latest.find(c => c.categoryName === signal);
    return cat ? cat.score : 0;
}

// Collect blendshape statistics over a time window (ms). Returns [{ name, mean, max }].
// Used by training to compare a "relaxed" window against an "indicating" window.
export function captureWindow(ms) {
    return new Promise((resolve) => {
        const acc = {};
        let frames = 0;
        const t0 = performance.now();
        const tick = () => {
            if (latest) {
                frames++;
                for (const c of latest) {
                    const e = acc[c.categoryName] || (acc[c.categoryName] = { sum: 0, max: 0 });
                    e.sum += c.score;
                    if (c.score > e.max) e.max = c.score;
                }
            }
            if (performance.now() - t0 < ms) {
                requestAnimationFrame(tick);
            } else {
                const out = Object.keys(acc).map(name => ({
                    name,
                    mean: frames ? acc[name].sum / frames : 0,
                    max: acc[name].max
                }));
                resolve(out);
            }
        };
        requestAnimationFrame(tick);
    });
}

// Switch to live detection with a trained signal/threshold.
export function setDetect(signal, threshold, refractoryMs) {
    detect = { signal, threshold, refractoryMs: refractoryMs || 800 };
    mode = "detect";
    wasAbove = false;
    refractoryUntil = 0;
}

export function setPreview() {
    mode = "preview";
    detect = null;
}

// Short tone via Web Audio — used for "start capturing" / "stop" cues during training and to
// confirm a detected gesture, since the user may be looking away from the screen.
let audioCtx = null;
export function beep(frequency, durationMs) {
    try {
        audioCtx = audioCtx || new (window.AudioContext || window.webkitAudioContext)();
        const osc = audioCtx.createOscillator();
        const gain = audioCtx.createGain();
        osc.type = "sine";
        osc.frequency.value = frequency || 880;
        gain.gain.value = 0.15;
        osc.connect(gain);
        gain.connect(audioCtx.destination);
        const now = audioCtx.currentTime;
        osc.start(now);
        osc.stop(now + (durationMs || 150) / 1000);
    } catch {
        /* audio unavailable; ignore */
    }
}

export function stop() {
    running = false;
    if (rafId) cancelAnimationFrame(rafId);
    rafId = 0;
    if (stream) {
        stream.getTracks().forEach(t => t.stop());
        stream = null;
    }
    if (video) {
        video.srcObject = null;
        video = null;
    }
    if (landmarker) {
        try { landmarker.close(); } catch { /* ignore */ }
        landmarker = null;
    }
    latest = null;
    dotnetRef = null;
}
