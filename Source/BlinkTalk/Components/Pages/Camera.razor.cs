using BlinkTalk.Application.Abstractions;
using BlinkTalk.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlinkTalk.Components.Pages;

public partial class Camera
{
	private bool Busy;
	private readonly CameraIndicatorConfig Config;
	private double DwellSeconds;
	private string? Error;
	private bool Flash;
	private double HoldFraction; // 0..1 = current hold time scaled to the 2s slider max
	private DotNetObjectReference<CameraCallbacks>? JSCallbacks;
	private readonly IJSRuntime JSRuntime;
	private CancellationTokenSource? MeterCts;
	private IJSObjectReference? Module;
	private readonly NavigationManager Navigation;
	private string? SignalDescription;
	private readonly ITextToSpeechService Speech;
	private bool Started;
	private string Status = "Starting camera…";
	private ElementReference Video;

	public Camera(IJSRuntime jsRuntime, CameraIndicatorConfig config, ITextToSpeechService speech, NavigationManager navigation)
	{
		JSRuntime = jsRuntime;
		Config = config;
		Speech = speech;
		Navigation = navigation;
	}

	async ValueTask IAsyncDisposable.DisposeAsync()
	{
		MeterCts?.Cancel();
		if (Module is not null)
		{
			try { await Module.InvokeVoidAsync("stop"); } catch { /* ignore */ }
			try { await Module.DisposeAsync(); } catch { /* ignore */ }
		}
		JSCallbacks?.Dispose();
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender) return;
		try
		{
			if (!await EnsureCameraPermissionAsync())
			{
				Error = "Camera permission was denied. Enable the camera for BlinkTalk in your device settings, then reopen this page.";
				Status = "";
				await InvokeAsync(StateHasChanged);
				return;
			}

			Module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./js/blinktalk-camera.js");
			JSCallbacks = DotNetObjectReference.Create(new CameraCallbacks(this));
			await Module.InvokeAsync<bool>("start", Video, JSCallbacks);
			Started = true;

			if (Config.IsTrained)
			{
				SignalDescription = Describe(Config.Signal);
				await ArmDetectAsync();
				StartMeterLoop();
			}
			Status = "Camera ready. Press Train and follow the prompts.";
		}
		catch (Exception ex)
		{
			Error = "Could not start the camera: " + ex.Message;
			Status = "";
		}
		await InvokeAsync(StateHasChanged);
	}

	protected override void OnInitialized() => DwellSeconds = Config.DwellSeconds;

	// Run live detection on this page so holding the gesture fills the time meter and beeps once the
	// hold reaches the dwell — letting the user calibrate the hold time before enabling the camera.
	private async Task ArmDetectAsync()
	{
		if (Module is null) return;
		try { await Module.InvokeVoidAsync("setDetect", Config.Signal, Config.Threshold, DwellSeconds * 1000, 800); }
		catch { /* page tearing down */ }
	}

	// Pick the blendshape whose score increases most from the relaxed window to the indicating
	// window. The threshold sits midway between the two means; require a minimum separation so we
	// don't lock onto noise.
	private static (string? signal, double threshold, double separation) ChooseSignal(BlendStat[] neutral, BlendStat[] active)
	{
		const double MinSeparation = 0.15;
		var neutralByName = neutral.ToDictionary(s => s.Name, s => s.Mean);

		string? best = null;
		double bestDiff = 0;
		double bestNeutralMean = 0;
		foreach (var a in active)
		{
			double neutralMean = neutralByName.TryGetValue(a.Name, out var n) ? n : 0;
			double diff = a.Mean - neutralMean;
			if (diff > bestDiff)
			{
				bestDiff = diff;
				best = a.Name;
				bestNeutralMean = neutralMean;
			}
		}

		if (best is null || bestDiff < MinSeparation)
			return (null, 0, bestDiff);

		return (best, bestNeutralMean + bestDiff * 0.5, bestDiff);
	}

	private static string Describe(string signal) => signal switch {
		"eyeLookUpLeft" or "eyeLookUpRight" => "look up",
		"eyeLookDownLeft" or "eyeLookDownRight" or "eyeBlinkLeft" or "eyeBlinkRight" => "blink",
		"eyeLookInLeft" or "eyeLookInRight" or "eyeLookOutLeft" or "eyeLookOutRight" => "look sideways",
		"browInnerUp" or "browOuterUpLeft" or "browOuterUpRight" => "raise eyebrows",
		"mouthSmileLeft" or "mouthSmileRight" => "smile",
		"jawOpen" => "open mouth",
		_ => signal
	};

	// Android requires the OS camera permission at runtime (in addition to the WebView grant).
	// Other platforms either prompt automatically (iOS/Mac WKWebView, Windows WebView2) or don't gate it.
	private static async Task<bool> EnsureCameraPermissionAsync()
	{
		if (Microsoft.Maui.Devices.DeviceInfo.Platform != Microsoft.Maui.Devices.DevicePlatform.Android)
			return true;

		var status = await Microsoft.Maui.ApplicationModel.Permissions.RequestAsync<Microsoft.Maui.ApplicationModel.Permissions.Camera>();
		return status == Microsoft.Maui.ApplicationModel.PermissionStatus.Granted;
	}

	private void GoBack() => Navigation.NavigateTo("/settings");

	// Called from JS when the trained gesture fires (held past the dwell) — visual confirmation to
	// accompany the JS beep. The dwell-edge callbacks are no-ops here: only the live Type page acts
	// on them, but detect mode raises them so we must accept them without erroring.
	private async Task OnCameraIndicated()
	{
		Flash = true;
		await InvokeAsync(StateHasChanged);
		await Task.Delay(180);
		Flash = false;
		await InvokeAsync(StateHasChanged);
	}

	private async Task OnDwellChanged(ChangeEventArgs e)
	{
		if (double.TryParse(e.Value?.ToString(), out double seconds))
		{
			DwellSeconds = seconds;
			Config.DwellSeconds = seconds;
			await ArmDetectAsync(); // re-arm so the beep triggers at the new hold time
		}
	}

	private Task OnDwellEnded() => Task.CompletedTask;

	private Task OnDwellStarted() => Task.CompletedTask;

	private void OnUseCameraChanged(ChangeEventArgs e)
	{
		Config.IsEnabled = e.Value is bool b && b;
	}

	// Show the prompt on screen (for the helper) and speak it aloud (for the user, who may be
	// looking away). Awaits speech so the start tone doesn't talk over the instruction.
	private async Task SayAsync(string text)
	{
		Status = text;
		await InvokeAsync(StateHasChanged);
		try { await Speech.SpeakAsync(text); } catch { /* TTS unavailable; on-screen text remains */ }
	}

	private void StartMeterLoop()
	{
		MeterCts?.Cancel();
		var cts = new CancellationTokenSource();
		MeterCts = cts;
		_ = Task.Run(async () =>
		{
			// Poll how long the gesture has been held and show it on the bar, scaled to the 2s slider
			// max so the bar and slider share one timeline. The beep at the dwell comes from JS.
			while (!cts.IsCancellationRequested && Module is not null)
			{
				try
				{
					double heldSeconds = await Module.InvokeAsync<double>("currentHoldSeconds");
					HoldFraction = Math.Min(1.0, heldSeconds / 2.0);
					await InvokeAsync(StateHasChanged);
				}
				catch { /* page tearing down */ }
				await Task.Delay(80);
			}
		});
	}

	private async Task TrainAsync()
	{
		if (Module is null || !Started || Busy) return;
		Busy = true;
		SignalDescription = null;
		MeterCts?.Cancel();
		await Module.InvokeVoidAsync("setPreview");

		try
		{
			// Spoken instructions only (no tones/beeps): the indicating gesture (e.g. looking up)
			// can take the user's eyes off the screen, so each step is announced. The speech itself
			// brackets each capture window — capturing happens during the pause after each prompt.
			while (true)
			{
				await SayAsync("Look at the screen and relax your face. Do not look at the camera.");
				var neutral = await Module.InvokeAsync<BlendStat[]>("captureWindow", 3000);

				await SayAsync("Now make your indicating gesture and hold it until I tell you to relax.");
				var active = await Module.InvokeAsync<BlendStat[]>("captureWindow", 3000);

				await SayAsync("Now relax.");

				var (signal, threshold, separation) = ChooseSignal(neutral, active);
				if (signal is null)
				{
					Status = "I couldn't detect a clear, repeatable gesture — let's try again.";
					await SayAsync(Status);
					continue; // restart the training run
				}

				Config.SaveTraining(signal, threshold);
				SignalDescription = Describe(signal);
				Status = $"Training successful! I detected your “{SignalDescription}” gesture. Ensure ‘Use camera’ is ticked so you can use it.";
				await SayAsync(Status);
				await ArmDetectAsync();
				StartMeterLoop();
				break; // done
			}
		}
		catch (Exception ex)
		{
			Error = "Training failed: " + ex.Message;
		}
		finally
		{
			Busy = false;
			await InvokeAsync(StateHasChanged);
		}
	}

	private record BlendStat(string Name, double Mean, double Max);

	// JS invokes these by name on the DotNetObjectReference. Holding them in a nested class keeps the
	// [JSInvokable] surface off the component; each call just forwards to the component's handler.
	private sealed class CameraCallbacks
	{
		private readonly Camera Owner;

		public CameraCallbacks(Camera owner) => Owner = owner;

		[JSInvokable]
		public Task OnCameraIndicated() => Owner.OnCameraIndicated();

		[JSInvokable]
		public Task OnDwellEnded() => Owner.OnDwellEnded();

		[JSInvokable]
		public Task OnDwellStarted() => Owner.OnDwellStarted();
	}
}
