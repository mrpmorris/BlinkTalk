using System;
using BlinkTalk.Application.Text;

namespace BlinkTalk.Application.Input.Strategies
{
    /// <summary>
    /// Top-level strategy: scans the three sections (WordSelector, Keyboard, Speak). Indicating
    /// drills into the word or keyboard scanners, or commits and speaks the sentence. Ported from
    /// the original SectionSelectorInputStrategy, including the "don't re-offer Word selection
    /// immediately after leaving it" rule via <see cref="_skipWordSelection"/>.
    /// </summary>
    public sealed class SectionSelectorInputStrategy : IInputStrategy
    {
        private bool _skipWordSelection;
        private Section _focusedSection;
        private IScanController _controller = null!;
        private SentenceBuilder _sentence = null!;
        private FocusCycler? _cycler;

        public void Initialize(IScanController controller)
        {
            _controller = controller;
            _sentence = controller.Sentence;
            _cycler?.Stop();
            _cycler = controller.NewCycler(FocusIndexChanged, mayFocus: MayFocusOnSection);
            _cycler.Start(3);
        }

        public void ReceiveIndication()
        {
            switch (_focusedSection)
            {
                case Section.WordSelector:
                    _cycler?.Stop();
                    _controller.Push<WordSuggestionSelectorInputStrategy>();
                    break;
                case Section.Keyboard:
                    _cycler?.Stop();
                    _controller.Push<KeyboardRowSelectorInputStrategy>();
                    break;
                case Section.Speak:
                    string sentence = _sentence.Commit();
                    _ = _controller.Speech.SpeakAsync(sentence);
                    break;
                default:
                    throw new NotImplementedException(_focusedSection.ToString());
            }
        }

        public void ChildStrategyActivated(IInputStrategy childStrategy)
        {
            if (childStrategy is WordSuggestionSelectorInputStrategy)
                _skipWordSelection = true;
            _cycler?.Stop();
        }

        public void Terminated() => _cycler?.Stop();

        private void FocusIndexChanged(int focusIndex)
        {
            _skipWordSelection = false;
            _focusedSection = (Section)focusIndex;
            _controller.SetHighlight(HighlightTarget.ForSection(_focusedSection));
        }

        private bool MayFocusOnSection(int focusIndex)
        {
            switch ((Section)focusIndex)
            {
                case Section.Keyboard: return true;
                case Section.Speak: return !_sentence.IsEmpty;
                case Section.WordSelector: return !_skipWordSelection && _sentence.SuggestedWords.Count > 0;
                default: throw new NotImplementedException(((Section)focusIndex).ToString());
            }
        }
    }
}
