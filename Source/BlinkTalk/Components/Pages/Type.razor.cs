using BlinkTalk.Application.Input;
using BlinkTalk.Application.Text;
using BlinkTalk.Services.Indicators;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace BlinkTalk.Components.Pages;

public partial class Type
{
	private readonly ScanController Controller;
	private readonly IJSRuntime JS;
	private readonly KeyboardIndicator Keyboard;
	private int LastScrolledWord = -1; // data-word-index last scrolled into view, or -1 for "none"
	private string? LastScrolledSentence; // sentence text last scrolled into view, or null for "none"
	private readonly NavigationManager Navigation;
	private readonly PointerIndicator Pointer;
	private ElementReference Root;
	private IJSObjectReference? WordsModule;

	public Type(ScanController controller, PointerIndicator pointer, KeyboardIndicator keyboard, NavigationManager navigation, IJSRuntime js)
	{
		Controller = controller;
		Pointer = pointer;
		Keyboard = keyboard;
		Navigation = navigation;
		JS = js;
	}

	// The already-committed words, rendered as a normal sentence. SentenceText is the committed
	// words plus (when typing) " " + CurrentWord, so strip that suffix to get the committed part.
	private string CommittedText
	{
		get
		{
			string full = SentenceText;
			return CurrentWord.Length == 0 ? full : full.Substring(0, full.Length - CurrentWord.Length - 1);
		}
	}

	// The word currently being typed (shown in a box); empty just after a space.
	private string CurrentWord => Controller.Sentence.CurrentWord;

	// --- Highlight helpers (map the controller's HighlightTarget to CSS classes) ---

	/// <summary>The letter decorators on offer, shown in the popup while one is being picked.</summary>
	private IReadOnlyList<string> Decorators => Controller.Keyboard.Decorators;

	private string DepthColor => Controller.Depth switch {
		<= 1 => "#2f6bff", // blue
		2 => "#2ec16b",    // green
		3 => "#d44ce0",    // magenta
		_ => "#e6c52f"     // yellow — depth 4, picking a decorator
	};

	private HighlightTarget H => Controller.Highlight;

	private string KeyboardContextClass =>
		H.Kind is HighlightKind.KeyboardRow or HighlightKind.Key or HighlightKind.Decorator ? "bt-context" : "";

	private IReadOnlyList<IReadOnlyList<KeyboardKey>> Rows => Controller.Keyboard.Rows;

	private string SentenceText => Controller.Sentence.ToString();

	private string TextDirection => Controller.Keyboard.IsRightToLeft ? "rtl" : "ltr";

	private IReadOnlyList<string> Words => Controller.Sentence.SuggestedWords;

	private string WordsContextClass =>
		H.Kind == HighlightKind.WordSuggestion ? "bt-context" : "";

	// Blazor disposes a component that implements IAsyncDisposable by calling DisposeAsync, so the
	// unsubscribe and module teardown live there; Dispose is the stub expected of callers that only
	// support IDisposable, and stays empty rather than duplicating the teardown.
	void IDisposable.Dispose()
	{
	}

	async ValueTask IAsyncDisposable.DisposeAsync()
	{
		Controller.StateChanged -= OnStateChanged;
		if (WordsModule is not null)
		{
			try { await WordsModule.DisposeAsync(); } catch { /* ignore */ }
		}
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			// Make the surface focusable so the keyboard switch (Space/Enter) is captured.
			try { await Root.FocusAsync(); } catch { /* focus best-effort */ }
		}

		// Bring the highlighted suggestion into view: centred in the panel when there is room on
		// both sides, clamped to the panel edges otherwise. Skipped while the same word stays
		// highlighted — the suggestion list is static during word selection, so another render of
		// the same word is not another scroll. Leaving word selection resets, so the first word of
		// the next session always scrolls.
		var highlight = Controller.Highlight;
		if (highlight.Kind == HighlightKind.WordSuggestion)
		{
			if (highlight.WordIndex != LastScrolledWord)
			{
				LastScrolledWord = highlight.WordIndex;
				try
				{
					WordsModule ??= await JS.InvokeAsync<IJSObjectReference>("import", "./js/blinktalk-words.js");
					await WordsModule.InvokeVoidAsync("scrollWordIntoView", highlight.WordIndex);
				}
				catch { /* scrolling is cosmetic; scanning must never stop for it */ }
			}
		}
		else
		{
			LastScrolledWord = -1;
		}

		// Keep the sentence scrolled to its end: the panel is a single fixed-height line (see
		// .bt-sentence in blinktalk.css), so a long sentence clips, never grows. Its newest text
		// is at the inline end, and the current-word box is the last thing rendered, so scrolling
		// it into view end-aligned shows the whole tail. Skipped while the text is unchanged — the
		// same-text guard as LastScrolledWord above.
		string sentence = SentenceText;
		if (!string.IsNullOrEmpty(sentence) && sentence != LastScrolledSentence)
		{
			LastScrolledSentence = sentence;
			try
			{
				WordsModule ??= await JS.InvokeAsync<IJSObjectReference>("import", "./js/blinktalk-words.js");
				await WordsModule.InvokeVoidAsync("scrollSentenceToEnd");
			}
			catch { /* scrolling is cosmetic; scanning must never stop for it */ }
		}
	}

	protected override void OnInitialized()
	{
		Controller.StateChanged += OnStateChanged;
		Controller.Start();
	}

	private string DecoratorClass(int decoratorIndex) =>
		H.Kind == HighlightKind.Decorator && H.DecoratorIndex == decoratorIndex ? "bt-highlight" : "";

	private void GoToSettings() => Navigation.NavigateTo("/settings");

	private string KeyClass(int rowIndex, int colIndex) =>
		H.Kind == HighlightKind.Key && H.RowIndex == rowIndex && H.ColumnIndex == colIndex ? "bt-highlight" : "";

	private void OnIndicate() => Pointer.Trigger();

	private void OnKeyDown(KeyboardEventArgs e)
	{
		if (e.Key == " " || e.Key == "Spacebar" || e.Key == "Enter")
			Keyboard.Trigger();
	}

	private void OnStateChanged() => InvokeAsync(StateHasChanged);

	private string RowClass(int rowIndex) =>
		H.Kind == HighlightKind.KeyboardRow && H.RowIndex == rowIndex ? "bt-highlight" : "";

	private string SectionClass(Section s) =>
		H.Kind == HighlightKind.Section && H.Section == s ? "bt-highlight" : "";

	private string WordClass(int index) =>
		H.Kind == HighlightKind.WordSuggestion && H.WordIndex == index ? "bt-highlight" : "";
}
