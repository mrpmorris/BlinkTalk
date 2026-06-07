using BlinkTalk.Application.Input;
using BlinkTalk.Application.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace BlinkTalk.Components.Pages;

public partial class Type
{
	private ElementReference Root;

	private IReadOnlyList<string> Words => Controller.Sentence.SuggestedWords;
	private IReadOnlyList<IReadOnlyList<KeyCode>> Rows => Controller.Keyboard.Rows;
	private string SentenceText => Controller.Sentence.ToString();

	// The word currently being typed (shown in a box); empty just after a space.
	private string CurrentWord => Controller.Sentence.CurrentWord;

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

	protected override void OnInitialized()
	{
		Controller.StateChanged += OnStateChanged;
		Controller.Start();
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			// Make the surface focusable so the keyboard switch (Space/Enter) is captured.
			try { await Root.FocusAsync(); } catch { /* focus best-effort */ }
		}
	}

	private void OnStateChanged() => InvokeAsync(StateHasChanged);

	private void OnIndicate() => Pointer.Trigger();

	private void OnKeyDown(KeyboardEventArgs e)
	{
		if (e.Key == " " || e.Key == "Spacebar" || e.Key == "Enter")
			Keyboard.Trigger();
	}

	private void GoToSettings() => Navigation.NavigateTo("/settings");

	// --- Highlight helpers (map the controller's HighlightTarget to CSS classes) ---

	private string DepthColor => Controller.Depth switch {
		<= 1 => "#2f6bff", // blue
		2 => "#2ec16b",    // green
		3 => "#d44ce0",    // magenta
		_ => "#e6c52f"     // yellow
	};

	private HighlightTarget H => Controller.Highlight;

	private string SectionClass(Section s) =>
		H.Kind == HighlightKind.Section && H.Section == s ? "bt-highlight" : "";

	private string WordsContextClass =>
		H.Kind == HighlightKind.WordSuggestion ? "bt-context" : "";

	private string KeyboardContextClass =>
		H.Kind == HighlightKind.KeyboardRow || H.Kind == HighlightKind.Key ? "bt-context" : "";

	private string WordClass(int index) =>
		H.Kind == HighlightKind.WordSuggestion && H.WordIndex == index ? "bt-highlight" : "";

	private string RowClass(int rowIndex) =>
		H.Kind == HighlightKind.KeyboardRow && H.RowIndex == rowIndex ? "bt-highlight" : "";

	private string KeyClass(int rowIndex, int colIndex) =>
		H.Kind == HighlightKind.Key && H.RowIndex == rowIndex && H.ColumnIndex == colIndex ? "bt-highlight" : "";

	public void Dispose() => Controller.StateChanged -= OnStateChanged;
}