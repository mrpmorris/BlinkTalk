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
        SentenceBuilder GetSentenceBuilder();

        TStrategy StartInputStrategy<TStrategy>()
            where TStrategy : MonoBehaviour, IInputStrategy;
        void InputStrategyFinished();
        void SetIndicatorRect(RectTransform target);
    }
}
