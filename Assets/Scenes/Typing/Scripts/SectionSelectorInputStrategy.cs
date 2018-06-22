using System;
using System.Collections;
using UnityEngine;

namespace BlinkTalk.Typing
{
    public class SectionSelectorInputStrategy: MonoBehaviour, IInputStrategy
    {
        private ITypingController Controller;
        private Coroutine SwitchFocusCoRoutine;
        private int FocusIndex;

        void IInputStrategy.Initialize(ITypingController controller)
        {
            Controller = controller;
            if (SwitchFocusCoRoutine == null)
            {
                FocusIndex = 0;
                SwitchFocusCoRoutine = StartCoroutine(CycleFocus());
            }
        }

        void IInputStrategy.ReceiveIndication()
        {
            TerminateSwitchFocusCoRoutine();
        }

        void IInputStrategy.Terminate()
        {
            TerminateSwitchFocusCoRoutine();
        }

        void TerminateSwitchFocusCoRoutine()
        {
            if (SwitchFocusCoRoutine != null)
            {
                StopCoroutine(SwitchFocusCoRoutine);
                SwitchFocusCoRoutine = null;
            }
        }

        IEnumerator CycleFocus()
        {
            while (true)
            {
                RectTransform focusTarget = null;
                switch (FocusIndex)
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
                        throw new NotImplementedException(FocusIndex + "");
                }
                Controller.SetIndicatorRect(focusTarget);
                yield return new WaitForSeconds(Consts.CycleDelay);
                FocusIndex = (FocusIndex + 1) % 3;
            }
        }
    }
}
