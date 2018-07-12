using System;
using UnityEngine;

namespace BlinkTalk.Typing
{
    public class KeyboardColumnSelectorInputStrategy : MonoBehaviour, IKeyboardColumnSelectorInputStrategy
    {
        private ITypingController Controller;
        private KeyboardKey FocusedKeyboardKey;
        private RectTransform[] KeyRectTransforms;
        private readonly FocusCycler FocusCycler;
        private SentenceBuilder SentenceBuilder;

        public KeyboardColumnSelectorInputStrategy()
        {
            FocusCycler = new FocusCycler(this, FocusIndexChanged);
        }

        void IInputStrategy.Initialize(ITypingController controller)
        {
            Controller = controller;
            SentenceBuilder = Controller.GetSentenceBuilder();
        }

        void IKeyboardColumnSelectorInputStrategy.SetActiveRow(RectTransform keyboardRow)
        {
            KeyRectTransforms = keyboardRow.GetChildRectTransforms();
            FocusCycler.Start(KeyRectTransforms.Length);
        }

        void IInputStrategy.ReceiveIndication()
        {
            FocusCycler.Stop();
            SentenceBuilder.Input(FocusedKeyboardKey.KeyCode);
            Controller.InputStrategyFinished();
        }

        void IInputStrategy.ChildStrategyActivated(IInputStrategy childStrategy)
        {
        }

        void IInputStrategy.Terminated()
        {
            FocusCycler.Stop();
        }

        void FocusIndexChanged(int focusIndex)
        {
            FocusedKeyboardKey = KeyRectTransforms[focusIndex].GetComponent<KeyboardKey>();
            Controller.SetIndicatorRect(KeyRectTransforms[focusIndex]);
            if (FocusCycler.FocusChangeCount > KeyRectTransforms.Length + 2)
                Controller.InputStrategyFinished();
        }
    }
}