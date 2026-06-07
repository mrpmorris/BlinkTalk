using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlinkTalk.Components;

public partial class CameraIndicator
{
	private ElementReference Video;
	private IJSObjectReference? Module;
	private DotNetObjectReference<CameraIndicator>? SelfRef;
	private bool Started;
	private bool Dwelling; // true between OnDwellStarted and OnDwellEnded, so teardown can balance

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender || Started || !Config.IsEnabled)
			return;
		Started = true;
		try
		{
			if (Microsoft.Maui.Devices.DeviceInfo.Platform == Microsoft.Maui.Devices.DevicePlatform.Android)
			{
				var status = await Microsoft.Maui.ApplicationModel.Permissions.RequestAsync<Microsoft.Maui.ApplicationModel.Permissions.Camera>();
				if (status != Microsoft.Maui.ApplicationModel.PermissionStatus.Granted)
					return; // helper button still works
			}

			Module = await JS.InvokeAsync<IJSObjectReference>("import", "./js/blinktalk-camera.js");
			SelfRef = DotNetObjectReference.Create(this);
			await Module.InvokeAsync<bool>("start", Video, SelfRef);
			await Module.InvokeVoidAsync("setDetect", Config.Signal, Config.Threshold, Config.DwellSeconds * 1000, 800);
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
		Dwelling = true;
		Indicator.TriggerDwellStarted();
		return Task.CompletedTask;
	}

	[JSInvokable]
	public Task OnDwellEnded()
	{
		Dwelling = false;
		Indicator.TriggerDwellEnded();
		return Task.CompletedTask;
	}

	public async ValueTask DisposeAsync()
	{
		// If a gesture is still held as we tear down, balance the counter so the scan doesn't
		// stay paused forever. JS stop() deliberately stays silent, so this is the sole balancer.
		if (Dwelling)
		{
			Dwelling = false;
			try { Indicator.TriggerDwellEnded(); } catch { /* ignore */ }
		}
		if (Module is not null)
		{
			try { await Module.InvokeVoidAsync("stop"); } catch { /* ignore */ }
			try { await Module.DisposeAsync(); } catch { /* ignore */ }
		}
		SelfRef?.Dispose();
	}
}