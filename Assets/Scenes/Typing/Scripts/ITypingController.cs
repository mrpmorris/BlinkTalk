using UnityEngine;
using UnityEngine.UI;

namespace BlinkTalk.Typing
{
    public interface ITypingController
    {
        RectTransform GetInputSelectionPanel();
        RectTransform GetWordSelectionPanel();
        RectTransform GetKeyboardSelectionPanel();
        ScrollRect GetKeyboardScrollRect();

        void StartInputStrategy<TStrategy>()
            where TStrategy : MonoBehaviour, IInputStrategy;
        void InputStrategyFinished();
        void SetIndicatorRect(RectTransform target);
    }
}
