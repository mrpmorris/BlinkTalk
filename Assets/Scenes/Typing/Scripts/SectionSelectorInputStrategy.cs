using System;
using UnityEngine;

namespace BlinkTalk.Typing
{
    public class SectionSelectorInputStrategy : MonoBehaviour, IInputStrategy
    {
        private int FocusIndex;
        private ITypingController Controller;
        private FocusCycler FocusCycler;
        private SentenceBuilder SentenceBuilder;

        void IInputStrategy.Initialize(ITypingController controller)
        {
            Controller = controller;
            SentenceBuilder = controller.GetSentenceBuilder();
            if (FocusCycler == null)
                FocusCycler = new FocusCycler(this, 3, FocusIndexChanged);
            FocusCycler.Start();
        }

        void IInputStrategy.ReceiveIndication()
        {
            switch (FocusIndex)
            {
                case 0:
                    FocusCycler.Stop();
                    Controller.StartInputStrategy<SuggestedWordSelectorInput>();
                    break;
                case 1:
                    FocusCycler.Stop();
                    Controller.StartInputStrategy<KeyboardRowSelectorInput>();
                    break;
                case 2:
                    string sentence = SentenceBuilder.Commit();
                    TextToSpeech.Speak(sentence);
                    break;
                default: throw new NotImplementedException(FocusIndex + "");
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
            FocusIndex = focusIndex;
            RectTransform focusTarget = null;
            switch (FocusIndex)
            {
                case 0:
                    focusTarget = Controller.GetWordSelectionPanel();
                    break;
                case 1:
                    focusTarget = Controller.GetKeyboardSelectionPanel();
                    break;
                case 2:
                    focusTarget = Controller.GetInputSelectionPanel();
                    break;
                default:
                    throw new NotImplementedException(focusIndex + "");
            }
            Controller.SetIndicatorRect(focusTarget);
        }
    }
}
