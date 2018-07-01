using UnityEngine;
using UnityEngine.UI;

namespace BlinkTalk.Typing
{
    public interface ITypingController
    {
        RectTransform GetInputSelectionPanel();
        RectTransform GetWordSelectionPanel();
        RectTransform GetKeyboardSelectionPanel();
        ScrollRect GetKeyboardSelector();
        RectTransform GetKeyboardSelectorClientArea();
        string GetSpokenText();

        TStrategy StartInputStrategy<TStrategy>()
            where TStrategy : MonoBehaviour, IInputStrategy;
        void InputStrategyFinished();
        void SetIndicatorRect(RectTransform target);
        void ReceiveKeyPress(KeyCode keyCode);
    }
}
