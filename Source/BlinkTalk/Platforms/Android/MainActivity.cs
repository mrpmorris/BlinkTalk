using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using AndroidX.Core.View;
using AView = Android.Views.View;

namespace BlinkTalk;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // From Android 15 (API 35) edge-to-edge is enforced, so the WebView draws behind the
        // system bars and the bottom-anchored SPEAK button is hidden by the navigation bar.
        // We keep the bars visible but inset the content by their size, so nothing is obscured
        // and edge taps land on the app rather than on the nav bar. (A pure-CSS env(safe-area-*)
        // fix doesn't help here — on Android those insets only cover display cutouts, not the
        // navigation bar.)
        var window = Window;
        if (window?.DecorView is null)
            return;

        WindowCompat.SetDecorFitsSystemWindows(window, false);

        // Light (white) bar icons over the app's dark background.
        var insetsController = WindowCompat.GetInsetsController(window, window.DecorView);
        insetsController.AppearanceLightStatusBars = false;
        insetsController.AppearanceLightNavigationBars = false;

        var content = window.DecorView.FindViewById(Android.Resource.Id.Content);
        if (content is not null)
        {
            // Matches --bt-bg, so the inset strip beside the bars blends with the app.
            content.SetBackgroundColor(Android.Graphics.Color.ParseColor("#101216"));
            ViewCompat.SetOnApplyWindowInsetsListener(content, new SystemBarInsetsListener());
        }
    }

    // Pads the content view by the system-bar insets so the app never draws underneath them.
    private sealed class SystemBarInsetsListener : Java.Lang.Object, IOnApplyWindowInsetsListener
    {
        public WindowInsetsCompat OnApplyWindowInsets(AView view, WindowInsetsCompat insets)
        {
            var bars = insets.GetInsets(WindowInsetsCompat.Type.SystemBars());
            view.SetPadding(bars.Left, bars.Top, bars.Right, bars.Bottom);
            return insets;
        }
    }
}
