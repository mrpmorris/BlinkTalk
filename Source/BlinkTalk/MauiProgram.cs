using BlinkTalk.Services;
using BlinkTalk.Services.Indicators;
using BlinkTalk.Application.Abstractions;
using BlinkTalk.Application.Input;
using BlinkTalk.Application.Persistence;
using BlinkTalk.Application.Prediction;
using BlinkTalk.Application.Text;
using Microsoft.Extensions.Logging;

namespace BlinkTalk;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		builder.Services.AddMauiBlazorWebView();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

#if ANDROID
		// Allow the in-page camera feed: grant WebView getUserMedia requests and let media autoplay.
		Microsoft.AspNetCore.Components.WebView.Maui.BlazorWebViewHandler.BlazorWebViewMapper.AppendToMapping(
			"BlinkTalkCameraPermissions",
			(handler, view) =>
			{
				handler.PlatformView.Settings.MediaPlaybackRequiresUserGesture = false;
				handler.PlatformView.SetWebChromeClient(new BlinkTalkWebChromeClient());
			});
#endif

		RegisterBlinkTalkServices(builder.Services);

		return builder.Build();
	}

	private static void RegisterBlinkTalkServices(IServiceCollection services)
	{
		// Platform abstractions
		services.AddSingleton<IClock, SystemClock>();
		services.AddSingleton<IUiDispatcher, MauiUiDispatcher>();
		services.AddSingleton<ISettingsStore, MauiPreferencesSettings>();
		services.AddSingleton<ITextToSpeechService, MauiTtsService>();

		// Database: copy the bundled English.db to writable storage, open it, run maintenance.
		services.AddSingleton<IDatabaseProvisioner, MauiDatabaseProvisioner>();
		services.AddSingleton<ISqliteDatabase>(sp =>
		{
			var provisioner = sp.GetRequiredService<IDatabaseProvisioner>();
			string path = provisioner.EnsureDatabase("English");
			var database = new MicrosoftDataSqliteDatabase(path);
			new AutoMigratingDatabase(database, sp.GetRequiredService<IClock>()).Migrate();
			return database;
		});

		// Prediction + sentence building
		services.AddSingleton<IWordService, WordService>();
		services.AddSingleton<IPhraseService, PhraseService>();
		services.AddSingleton(KeyboardLayout.CreateDefault());
		services.AddScoped<SentenceBuilder>();

		// Indicators (input sources for the single switch). Scoped so they share the controller's
		// lifetime; each is also exposed as IIndicator so the controller subscribes to all three.
		services.AddScoped<PointerIndicator>();
		services.AddScoped<KeyboardIndicator>();
		services.AddScoped<CameraGestureIndicator>();
		services.AddScoped<IIndicator>(sp => sp.GetRequiredService<PointerIndicator>());
		services.AddScoped<IIndicator>(sp => sp.GetRequiredService<KeyboardIndicator>());
		services.AddScoped<IIndicator>(sp => sp.GetRequiredService<CameraGestureIndicator>());

		// Scanning controller (drives the UI)
		services.AddScoped<ScanController>();

		// Camera-based indicator detection config. Singleton so the (non-persisted) "enabled this
		// session" flag lives for the whole run and resets to false on the next launch.
		services.AddSingleton<CameraIndicatorConfig>();
	}
}
