using UnityEngine;

namespace BlinkTalk.Typing.InputStrategies.KeyboardInputStrategies
{
	public interface IKeySelectionStrategy
	{
		string SelectedKeyText { get; }
		void Initialize(ITypingController controller);
		void Activate(RectTransform row);
	}
}
