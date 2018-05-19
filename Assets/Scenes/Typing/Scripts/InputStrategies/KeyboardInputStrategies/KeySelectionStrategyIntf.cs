using UnityEngine;

namespace BlinkTalk.Typing.InputStrategies.KeyboardInputStrategies
{
	 public interface IKeySelectionStrategy
	 {
		  void Initialize(ITypingController controller, IKeyboardInputStrategy keyboardInputStrategy);
		  void Activate(RectTransform row);
		  void MayRemainActive(bool value);
		  void Indicate();
	 }
}
