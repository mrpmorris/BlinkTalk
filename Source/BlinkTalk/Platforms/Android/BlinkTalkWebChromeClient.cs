#if ANDROID
using Android.Webkit;
namespace BlinkTalk;

/// <summary>
/// Grants in-page getUserMedia (camera/mic) requests inside the Android WebView. Without this,
/// the system WebView denies the request and MediaPipe/getUserMedia fails with
/// "Permission denied". The OS-level CAMERA permission is requested separately (MAUI Permissions);
/// both are required on Android.
/// </summary>
internal sealed class BlinkTalkWebChromeClient : WebChromeClient
{
	public override void OnPermissionRequest(PermissionRequest? request)
	{
		if (request is not null)
			request.Grant(request.GetResources());
	}
}
#endif