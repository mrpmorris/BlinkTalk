using UnityEngine;

namespace BlinkTalk.Typing
{
    public interface IKeyboardColumnSelectorInputStrategy: IInputStrategy
    {
        void SetActiveRow(RectTransform keyboardRow);
    }
}
