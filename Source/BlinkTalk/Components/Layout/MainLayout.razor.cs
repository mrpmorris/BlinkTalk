using BlinkTalk.Application;
using BlinkTalk.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlinkTalk.Components.Layout;

/// <summary>
/// Stamps the document language from <see cref="AppLanguage.Current"/> once the WebView is
/// live, and again whenever it has changed since. index.html is a static asset and Blazor's own
/// blazor-culture mechanism is WebAssembly-only, so the &lt;html lang&gt; attribute has to be set
/// through interop — a head outlet cannot do it, because it renders children of &lt;head&gt; and this
/// is an attribute on its parent. This drives screen readers, hyphenation and WebView spellcheck; the
/// spoken voice is chosen separately by MauiTtsService.
/// </summary>
public partial class MainLayout : LayoutComponentBase
{
	/// <summary>
	/// The culture code currently on the document, so a re-render that has not changed language does
	/// not pay for an interop call. Null until the first stamp, which is why an unchanged language
	/// still stamps once.
	/// </summary>
	private string? StampedCultureCode;

	[Inject]
	private IJSRuntime JS { get; set; } = null!;

	/// <summary>
	/// Every render rather than just the first: the language can change while the app is running, and
	/// this layout re-renders when the route does — so the attribute is right by the time the person
	/// leaves the settings page, which is the only page the language can be changed from.
	/// </summary>
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		string cultureCode = AppLanguage.Current.Name;
		if (cultureCode == StampedCultureCode)
			return;

		StampedCultureCode = cultureCode;
		await JS.InvokeVoidAsync("document.documentElement.setAttribute", "lang", cultureCode);
	}
}
