using System;
using System.Linq;
using UnityEngine;

namespace BlinkTalk.Typing
{
    public class SectionSelectorInputStrategy : MonoBehaviour, IInputStrategy
    {
        private Section FocusedSection;
        private ITypingController Controller;
        private FocusCycler FocusCycler;
        private SentenceBuilder SentenceBuilder;

        void IInputStrategy.Initialize(ITypingController controller)
        {
            Controller = controller;
            SentenceBuilder = controller.GetSentenceBuilder();
            if (FocusCycler == null)
                FocusCycler = new FocusCycler(this, 3, FocusIndexChanged, mayFocus: MayFocusOnSection);
            FocusCycler.Start();
        }

        void IInputStrategy.ReceiveIndication()
        {
            switch (FocusedSection)
            {
                case Section.WordSelector:
                    FocusCycler.Stop();
                    Controller.StartInputStrategy<WordSuggestionSelectorInput>();
                    break;
                case Section.Keyboard:
                    FocusCycler.Stop();
                    Controller.StartInputStrategy<KeyboardRowSelectorInput>();
                    break;
                case Section.Speak:
                    string sentence = SentenceBuilder.Commit();
                    TextToSpeech.Speak(sentence);
                    break;
                default: throw new NotImplementedException(FocusedSection + "");
            }
        }

        void IInputStrategy.ChildStrategyActivated(IInputStrategy inputStrategy)
        {
            FocusCycler.Stop();
        }

        void IInputStrategy.Terminated()
        {
            FocusCycler.Stop();
        }

        private void FocusIndexChanged(int focusIndex)
        {
            FocusedSection = (Section)focusIndex;
            RectTransform focusTarget = null;
            switch (FocusedSection)
            {
                case Section.WordSelector:
                    focusTarget = Controller.GetWordSelectionPanel();
                    break;
                case Section.Keyboard:
                    focusTarget = Controller.GetKeyboardSelectionPanel();
                    break;
                case Section.Speak:
                    focusTarget = Controller.GetInputSelectionPanel();
                    break;
                default:
                    throw new NotImplementedException(focusIndex + "");
            }
            Controller.SetIndicatorRect(focusTarget);
        }

        private bool MayFocusOnSection(int focusIndex)
        {
            switch ((Section)focusIndex)
            {
                case Section.Keyboard: return true;
                case Section.Speak: return !SentenceBuilder.IsEmpty;
                case Section.WordSelector: return SentenceBuilder.SuggestedWords.Any();
                default: throw new NotImplementedException(FocusedSection + "");
            }
        }
    }
}
