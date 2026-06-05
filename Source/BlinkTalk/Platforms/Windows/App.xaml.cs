using System;
using System.IO;
using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BlinkTalk.WinUI;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : MauiWinUIApplication
{
	/// <summary>
	/// Initializes the singleton application object.  This is the first line of authored code
	/// executed, and as such is the logical equivalent of main() or WinMain().
	/// </summary>
	public App()
	{
		// WebView2 defaults its user-data folder to a "<exe>.WebView2" directory next to the
		// executable. When the app is installed under Program Files that location is read-only
		// for standard users, so WebView2 fails to initialize and the BlazorWebView renders a
		// blank screen. Redirect it to a per-user writable folder before any WebView is created.
		if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBVIEW2_USER_DATA_FOLDER")))
		{
			string userDataFolder = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
				"BlinkTalk",
				"WebView2");
			Directory.CreateDirectory(userDataFolder);
			Environment.SetEnvironmentVariable("WEBVIEW2_USER_DATA_FOLDER", userDataFolder);
		}

		this.InitializeComponent();
	}

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}

