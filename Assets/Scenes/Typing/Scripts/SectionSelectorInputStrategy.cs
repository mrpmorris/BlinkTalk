using System;
using System.Collections;
using UnityEngine;

namespace BlinkTalk.Typing
{
    public class SectionSelectorInputStrategy : MonoBehaviour, IInputStrategy
    {
        private ITypingController Controller;
        private Coroutine SwitchFocusCoRoutine;
        private FocusCycler FocusCycler;

        void IInputStrategy.Initialize(ITypingController controller)
        {
            Controller = controller;
            if (FocusCycler == null)
                FocusCycler = new FocusCycler(this, 3, FocusIndexChanged);
            FocusCycler.Start();
        }

        void IInputStrategy.ReceiveIndication()
        {
            FocusCycler.Stop();
        }

        void IInputStrategy.Terminate()
        {
            FocusCycler.Stop();
        }

        private void FocusIndexChanged(int focusIndex)
        {
            RectTransform focusTarget = null;
            switch (focusIndex)
            {
                case 0:
                    focusTarget = Controller.GetKeyboardSelectionPanel();
                    break;
                case 1:
                    focusTarget = Controller.GetWordSelectionPanel();
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
