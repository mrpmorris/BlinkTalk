using System;
using BlinkTalk.Application.Abstractions;
using BlinkTalk.Application.Text;

namespace BlinkTalk.Application.Input
{
    /// <summary>
    /// Coordinates the scanning hierarchy and exposes the logical state the UI renders. The
    /// logical equivalent of the original TypingController, with all Unity UI types removed.
    /// </summary>
    public interface IScanController
    {
        SentenceBuilder Sentence { get; }
        KeyboardLayout Keyboard { get; }
        ITextToSpeechService Speech { get; }

        /// <summary>What the scanner is currently highlighting; the UI maps this to a CSS class.</summary>
        HighlightTarget Highlight { get; }

        /// <summary>Depth of the strategy stack (1-based), used to pick the highlight colour.</summary>
        int Depth { get; }

        /// <summary>Creates a cycler wired to this controller's dispatcher, delay source and scan speed.</summary>
        FocusCycler NewCycler(Action<int> focusChanged, double firstCycleMultiplier = 1,
            Func<int, bool>? mayFocus = null, Action? onExhausted = null);

        /// <summary>Push a new scanning level (the original StartInputStrategy).</summary>
        TStrategy Push<TStrategy>() where TStrategy : IInputStrategy, new();

        /// <summary>Pop the current scanning level back to its parent (the original InputStrategyFinished).</summary>
        void Pop();

        void SetHighlight(HighlightTarget target);

        /// <summary>Raised whenever the highlight, stack depth or sentence view-model changes.</summary>
        event Action? StateChanged;
    }
}
