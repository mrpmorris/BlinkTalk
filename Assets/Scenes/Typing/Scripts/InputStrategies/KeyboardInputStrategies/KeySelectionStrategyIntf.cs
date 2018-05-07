using UnityEngine;

namespace BlinkTalk.Typing.InputStrategies.KeyboardInputStrategies
{
	public interface IKeySelectionStrategy
	{
		string SelectedKeyText { get; }
		void Initialize(ITypingController controller, IKeyboardInputStrategy keyboardInputStrategy);
		void Activate(RectTransform row);
		void MayRemainActive(bool value);
	}
}
