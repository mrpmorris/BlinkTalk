using System;
using BlinkTalk.Application.Text;

namespace BlinkTalk.Application.Input.Strategies;

/// <summary>
/// Top-level strategy: scans the three sections (WordSelector, Keyboard, Speak). Indicating
/// drills into the word or keyboard scanners, or commits and speaks the sentence. Ported from
/// the original SectionSelectorInputStrategy, including the "don't re-offer Word selection
/// immediately after leaving it" rule via <see cref="SkipWordSelection"/>.
/// </summary>
public sealed class SectionSelectorInputStrategy : IInputStrategy
{
    private IScanController Controller = null!;
    private FocusCycler? Cycler;
    private Section FocusedSection;
    private SentenceBuilder Sentence = null!;
    private bool SkipWordSelection;

    public void ChildStrategyActivated(IInputStrategy childStrategy)
    {
        if (childStrategy is WordSuggestionSelectorInputStrategy)
            SkipWordSelection = true;
        Cycler?.Stop();
    }

    public void Initialize(IScanController controller)
    {
        Controller = controller;
        Sentence = controller.Sentence;
        Cycler?.Stop();
        Cycler = controller.NewCycler(FocusIndexChanged, mayFocus: MayFocusOnSection);
        Cycler.Start(3);
    }

    public void ReceiveIndication()
    {
        switch (FocusedSection)
        {
            case Section.WordSelector:
                Cycler?.Stop();
                Controller.Push<WordSuggestionSelectorInputStrategy>();
                break;
            case Section.Keyboard:
                Cycler?.Stop();
                Controller.Push<KeyboardRowSelectorInputStrategy>();
                break;
            case Section.Speak:
                string sentence = Sentence.Commit();
                _ = Controller.Speech.SpeakAsync(sentence);
                break;
            default:
                throw new NotImplementedException(FocusedSection.ToString());
        }
    }

    public void Terminated() => Cycler?.Stop();

    private void FocusIndexChanged(int focusIndex)
    {
        SkipWordSelection = false;
        FocusedSection = (Section)focusIndex;
        Controller.SetHighlight(HighlightTarget.ForSection(FocusedSection));
    }

    private bool MayFocusOnSection(int focusIndex)
    {
        switch ((Section)focusIndex)
        {
            case Section.Keyboard: return true;
            case Section.Speak: return !Sentence.IsEmpty;
            case Section.WordSelector: return !SkipWordSelection && Sentence.SuggestedWords.Count > 0;
            default: throw new NotImplementedException(((Section)focusIndex).ToString());
        }
    }
}
