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
		// The language has to be set before anything reads a resource string or a database path, which
		// is earlier than the service provider exists — hence the hand-built settings store. The same
		// instance is registered below so the app has one view of the preferences.
		var settings = new MauiPreferencesSettings();
		AppLanguage.RestorePersisted(settings);

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

		RegisterBlinkTalkServices(builder.Services, settings);

		return builder.Build();
	}

	private static void RegisterBlinkTalkServices(IServiceCollection services, ISettingsStore settings)
	{
		// Platform abstractions
		services.AddSingleton<IClock, SystemClock>();
		services.AddSingleton<IUIDispatcher, MauiUIDispatcher>();
		services.AddSingleton(settings);
		services.AddSingleton<ITextToSpeechService, MauiTtsService>();

		// Database: create the writable database from SQL, run maintenance. AppDatabase does that
		// for the current language, the first time it is asked. Word lists are no longer bundled:
		// the settings page downloads the language pack and seeds via a one-shot override, so the
		// default seed source is empty.
		services.AddSingleton<IDatabaseProvisioner, MauiDatabaseProvisioner>();
		services.AddSingleton<ISeedWordSource, EmptySeedWordSource>();
		services.AddSingleton<HttpClient>();
		services.AddSingleton<LanguagePackDownloader>();
		services.AddSingleton<AppDatabase>();
		services.AddSingleton<ISqliteDatabase>(sp => sp.GetRequiredService<AppDatabase>());

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
