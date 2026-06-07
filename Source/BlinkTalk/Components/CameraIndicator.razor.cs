using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlinkTalk.Components;

public partial class CameraIndicator
{
	private ElementReference _video;
	private IJSObjectReference? _module;
	private DotNetObjectReference<CameraIndicator>? _selfRef;
	private bool _started;
	private bool _dwelling; // true between OnDwellStarted and OnDwellEnded, so teardown can balance

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender || _started || !Config.IsEnabled)
			return;
		_started = true;
		try
		{
			if (Microsoft.Maui.Devices.DeviceInfo.Platform == Microsoft.Maui.Devices.DevicePlatform.Android)
			{
				var status = await Microsoft.Maui.ApplicationModel.Permissions.RequestAsync<Microsoft.Maui.ApplicationModel.Permissions.Camera>();
				if (status != Microsoft.Maui.ApplicationModel.PermissionStatus.Granted)
					return; // helper button still works
			}

			_module = await JS.InvokeAsync<IJSObjectReference>("import", "./js/blinktalk-camera.js");
			_selfRef = DotNetObjectReference.Create(this);
			await _module.InvokeAsync<bool>("start", _video, _selfRef);
			await _module.InvokeVoidAsync("setDetect", Config.Signal, Config.Threshold, Config.DwellSeconds * 1000, 800);
		}
		catch
		{
			// If the camera can't start (no permission/hardware), the helper button still works.
		}
	}

	[JSInvokable]
	public Task OnCameraIndicated()
	{
		Indicator.Trigger();
		return Task.CompletedTask;
	}

	[JSInvokable]
	public Task OnDwellStarted()
	{
		_dwelling = true;
		Indicator.TriggerDwellStarted();
		return Task.CompletedTask;
	}

	[JSInvokable]
	public Task OnDwellEnded()
	{
		_dwelling = false;
		Indicator.TriggerDwellEnded();
		return Task.CompletedTask;
	}

	public async ValueTask DisposeAsync()
	{
		// If a gesture is still held as we tear down, balance the counter so the scan doesn't
		// stay paused forever. JS stop() deliberately stays silent, so this is the sole balancer.
		if (_dwelling)
		{
			_dwelling = false;
			try { Indicator.TriggerDwellEnded(); } catch { /* ignore */ }
		}
		if (_module is not null)
		{
			try { await _module.InvokeVoidAsync("stop"); } catch { /* ignore */ }
			try { await _module.DisposeAsync(); } catch { /* ignore */ }
		}
		_selfRef?.Dispose();
	}
}