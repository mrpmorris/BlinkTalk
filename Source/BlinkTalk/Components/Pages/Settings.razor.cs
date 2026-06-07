using Microsoft.AspNetCore.Components;

namespace BlinkTalk.Components.Pages;

public partial class Settings
{
	private double ScanSpeed { get; set; }

	protected override void OnInitialized()
	{
		ScanSpeed = Controller.CycleDelaySeconds;
	}

	private void GoBack() => Navigation.NavigateTo("/type");

	private void GoToCamera() => Navigation.NavigateTo("/camera");

	private void OnScanSpeedChanged(ChangeEventArgs e)
	{
		if (double.TryParse(e.Value?.ToString(), out double seconds))
		{
			ScanSpeed = seconds;
			Controller.CycleDelaySeconds = seconds;
		}
	}
}