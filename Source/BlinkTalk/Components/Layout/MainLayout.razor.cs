using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlinkTalk.Components.Layout;

/// <summary>
/// Stamps the document language from <see cref="CultureInfo.CurrentCulture"/> once the WebView is
/// live. index.html is a static asset and Blazor's own blazor-culture mechanism is WebAssembly-only,
/// so the &lt;html lang&gt; attribute has to be set through interop. This drives screen readers,
/// hyphenation and WebView spellcheck; the spoken voice is chosen separately by MauiTtsService.
/// </summary>
public partial class MainLayout : LayoutComponentBase
{
	[Inject]
	private IJSRuntime JS { get; set; } = null!;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		await JS.InvokeVoidAsync(
			"document.documentElement.setAttribute",
			"lang",
			CultureInfo.CurrentCulture.Name);
	}
}
