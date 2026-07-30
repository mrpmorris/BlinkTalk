using System.Globalization;
using BlinkTalk.Application.Abstractions;
using BlinkTalk.Resources;
using BlinkTalk.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Microsoft.Maui.Platform;

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
	private string Status = Localization.Camera_StartingCamera;
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
				Error = Localization.Camera_CameraPermissionWasDenied;
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
			Status = Localization.Camera_CameraReady;
		}
		catch (Exception ex)
		{
			Error = string.Format(Localization.Camera_CouldNotStartTheCameraX0Format, ex.Message);
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
		"eyeLookUpLeft" or "eyeLookUpRight" => Localization.Gesture_LookUp,
		"eyeLookDownLeft" or "eyeLookDownRight" or "eyeBlinkLeft" or "eyeBlinkRight" => Localization.Gesture_Blink,
		"eyeLookInLeft" or "eyeLookInRight" or "eyeLookOutLeft" or "eyeLookOutRight" => Localization.Gesture_LookSideways,
		"browInnerUp" or "browOuterUpLeft" or "browOuterUpRight" => Localization.Gesture_RaiseEyebrows,
		"mouthSmileLeft" or "mouthSmileRight" => Localization.Gesture_Smile,
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

	// The range input always reports "1.2" regardless of language, so parse invariantly: a culture that
	// reads "." as a group separator would take that as 12 seconds.
	private async Task OnDwellChanged(ChangeEventArgs e)
	{
		if (double.TryParse(e.Value?.ToString(), CultureInfo.InvariantCulture, out double seconds))
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
				await SayAsync(Localization.Camera_Instructions_LookAtCamera);
				var neutral = await Module.InvokeAsync<BlendStat[]>("captureWindow", 3000);

				await SayAsync(Localization.Camera_Instructions_MakeIndicatingGesture);
				var active = await Module.InvokeAsync<BlendStat[]>("captureWindow", 3000);

				await SayAsync(Localization.Camera_Instructions_NowRelax);

				var (signal, threshold, separation) = ChooseSignal(neutral, active);
				if (signal is null)
				{
					Status = Localization.Camera_Instructions_CouldNotDetectGesture;
					await SayAsync(Status);
					continue; // restart the training run
				}

				Config.SaveTraining(signal, threshold);
				SignalDescription = Describe(signal);
				Status = string.Format(Localization.Camera_DetectedX0GestureFormat, SignalDescription);
				await SayAsync(Status);
				await ArmDetectAsync();
				StartMeterLoop();
				break; // done
			}
		}
		catch (Exception ex)
		{
			Error = string.Format(Localization.Camera_TrainingFailedX0Format, ex.Message);
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
