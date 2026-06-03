namespace BlinkTalk;

// Fully qualified: the BlinkTalk.Application namespace would otherwise shadow the MAUI Application type.
public partial class App : Microsoft.Maui.Controls.Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new MainPage()) { Title = "BlinkTalk" };
	}
}
