using System;
using System.Collections.Generic;
using BlinkTalk.Application.Text;

namespace BlinkTalk.Application.Input.Strategies
{
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
        private IScanController _controller = null!;
        private SentenceBuilder _sentence = null!;
        private IReadOnlyList<string> _words = Array.Empty<string>();
        private int _selectedIndex = -1;
        private FocusCycler? _cycler;

        public void Initialize(IScanController controller)
        {
            _controller = controller;
            _sentence = controller.Sentence;
            RestartFocusCycler();
        }

        public void ReceiveIndication()
        {
            if (_selectedIndex >= 0 && _selectedIndex < _words.Count)
                // Suggestions are stored lowercase in the DB and only displayed uppercase via CSS;
                // insert the uppercase form so it matches letters typed on the keyboard.
                _sentence.PushWord(_words[_selectedIndex].ToUpperInvariant());
            _cycler?.Stop();
            RestartFocusCycler();
        }

        public void ChildStrategyActivated(IInputStrategy childStrategy) { }

        public void Terminated() => _cycler?.Stop();

        private void FocusIndexChanged(int focusIndex)
        {
            _selectedIndex = focusIndex;
            _controller.SetHighlight(HighlightTarget.ForWord(focusIndex));
            if (_cycler!.FocusChangeCount > _words.Count + 1)
                _controller.Pop();
        }

        private void RestartFocusCycler()
        {
            _selectedIndex = -1;
            _words = _sentence.SuggestedWords;
            if (_words.Count == 0)
            {
                _controller.Pop();
                return;
            }
            _cycler ??= _controller.NewCycler(FocusIndexChanged, firstCycleMultiplier: Consts.FirstCycleDelayMultiplier);
            _cycler.Start(_words.Count);
        }
    }
}
