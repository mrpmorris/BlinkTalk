using System;
using UnityEngine;

namespace BlinkTalk.Typing
{
    public class KeyboardColumnSelectorInputStrategy : MonoBehaviour, IKeyboardColumnSelectorInputStrategy
    {
        private ITypingController Controller;
        private KeyboardKey FocusedKeyboardKey;
        private RectTransform[] KeyRectTransforms;
        private FocusCycler FocusCycler;
        private int FocusChangeCount;
        private SentenceBuilder SentenceBuilder;

        void IInputStrategy.Initialize(ITypingController controller)
        {
            Controller = controller;
            SentenceBuilder = Controller.GetSentenceBuilder();
            FocusChangeCount = 0;
        }

        void IKeyboardColumnSelectorInputStrategy.SetActiveRow(RectTransform keyboardRow)
        {
            KeyRectTransforms = keyboardRow.GetChildRectTransforms();
            if (FocusCycler == null)
                FocusCycler = new FocusCycler(this, KeyRectTransforms.Length, FocusIndexChanged);
            FocusCycler.Start();
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
            FocusChangeCount++;
            if (FocusChangeCount > KeyRectTransforms.Length + 2)
                Controller.InputStrategyFinished();
        }
    }
}