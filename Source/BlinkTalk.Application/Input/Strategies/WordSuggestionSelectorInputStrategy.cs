using System;
using System.Collections.Generic;
using BlinkTalk.Application.Text;

namespace BlinkTalk.Application.Input.Strategies;

/// <summary>
/// Scans the suggested words. Indicating a word appends it to the sentence and then restarts
/// scanning over the freshly recomputed suggestions (so the user can pick several words in a
/// row). Auto-exits after cycling through the words about once without a selection
/// (FocusChangeCount > words + 1). Ported from the original WordSuggestionSelectorInput; the
/// "wait a frame for the UI to instantiate word items" step is unnecessary here because the
/// suggestions are available directly from the SentenceBuilder.
/// </summary>
public sealed class WordSuggestionSelectorInputStrategy : IInputStrategy
{
    private IScanController Controller = null!;
    private SentenceBuilder Sentence = null!;
    private IReadOnlyList<string> Words = Array.Empty<string>();
    private int SelectedIndex = -1;
    private FocusCycler? Cycler;

    public void Initialize(IScanController controller)
    {
        Controller = controller;
        Sentence = controller.Sentence;
        RestartFocusCycler();
    }

    public void ReceiveIndication()
    {
        if (SelectedIndex >= 0 && SelectedIndex < Words.Count)
            // Suggestions are stored lowercase in the DB and only displayed uppercase via CSS;
            // insert the uppercase form so it matches letters typed on the keyboard.
            Sentence.PushWord(Words[SelectedIndex].ToUpperInvariant());
        Cycler?.Stop();
        RestartFocusCycler();
    }

    public void ChildStrategyActivated(IInputStrategy childStrategy) { }

    public void Terminated() => Cycler?.Stop();

    private void FocusIndexChanged(int focusIndex)
    {
        SelectedIndex = focusIndex;
        Controller.SetHighlight(HighlightTarget.ForWord(focusIndex));
        if (Cycler!.FocusChangeCount > Words.Count + 1)
            Controller.Pop();
    }

    private void RestartFocusCycler()
    {
        SelectedIndex = -1;
        Words = Sentence.SuggestedWords;
        if (Words.Count == 0)
        {
            Controller.Pop();
            return;
        }
        Cycler ??= Controller.NewCycler(FocusIndexChanged, firstCycleMultiplier: Consts.FirstCycleDelayMultiplier);
        Cycler.Start(Words.Count);
    }
}
