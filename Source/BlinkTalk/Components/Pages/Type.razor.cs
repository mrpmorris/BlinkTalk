using BlinkTalk.Application.Input;
using BlinkTalk.Application.Text;
using BlinkTalk.Services.Indicators;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace BlinkTalk.Components.Pages;

public partial class Type
{
	private readonly ScanController Controller;
	private readonly KeyboardIndicator Keyboard;
	private readonly NavigationManager Navigation;
	private readonly PointerIndicator Pointer;
	private ElementReference Root;

	public Type(ScanController controller, PointerIndicator pointer, KeyboardIndicator keyboard, NavigationManager navigation)
	{
		Controller = controller;
		Pointer = pointer;
		Keyboard = keyboard;
		Navigation = navigation;
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

	/// <summary>The accents on offer, or null when no accent is being picked.</summary>
	private AccentSelectionState? Accents => Controller.AccentState;

	private string DepthColor => Controller.Depth switch {
		<= 1 => "#2f6bff", // blue
		2 => "#2ec16b",    // green
		3 => "#d44ce0",    // magenta
		_ => "#e6c52f"     // yellow — depth 4, picking an accent
	};

	private HighlightTarget H => Controller.Highlight;

	private string KeyboardContextClass =>
		H.Kind is HighlightKind.KeyboardRow or HighlightKind.Key or HighlightKind.AccentMark ? "bt-context" : "";

	private IReadOnlyList<IReadOnlyList<KeyCode>> Rows => Controller.Keyboard.Rows;

	private string SentenceText => Controller.Sentence.ToString();

	private string TextDirection => Controller.Keyboard.IsRightToLeft ? "rtl" : "ltr";

	private IReadOnlyList<string> Words => Controller.Sentence.SuggestedWords;

	private string WordsContextClass =>
		H.Kind == HighlightKind.WordSuggestion ? "bt-context" : "";

	public void Dispose() => Controller.StateChanged -= OnStateChanged;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			// Make the surface focusable so the keyboard switch (Space/Enter) is captured.
			try { await Root.FocusAsync(); } catch { /* focus best-effort */ }
		}
	}

	protected override void OnInitialized()
	{
		Controller.StateChanged += OnStateChanged;
		Controller.Start();
	}

	/// <summary>
	/// The scanned mark, plus the one already chosen — which stays marked while the scan moves on to
	/// the letters, so the person can see which accent they are about to apply.
	/// </summary>
	private string AccentMarkClass(int markIndex)
	{
		string chosen = Accents?.ChosenMarkIndex == markIndex ? " bt-accent-chosen" : "";
		bool scanned = H.Kind == HighlightKind.AccentMark && H.MarkIndex == markIndex;
		return (scanned ? "bt-highlight" : "") + chosen;
	}

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
