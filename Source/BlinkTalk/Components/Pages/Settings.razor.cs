using BlinkTalk.Application.Input;
using BlinkTalk.Services;
using Microsoft.AspNetCore.Components;

namespace BlinkTalk.Components.Pages;

public partial class Settings
{
	private double ScanSpeed { get; set; }

	private readonly CameraIndicatorConfig Camera;
	private readonly ScanController Controller;
	private readonly NavigationManager Navigation;

	public Settings(ScanController controller, CameraIndicatorConfig camera, NavigationManager navigation)
	{
		Controller = controller;
		Camera = camera;
		Navigation = navigation;
	}

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