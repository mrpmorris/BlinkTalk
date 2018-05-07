using UnityEngine;

namespace BlinkTalk.Typing.InputStrategies.KeyboardInputStrategies
{
	public interface IKeySelectionStrategy
	{
		bool Live { get; set; }
		string SelectedKeyText { get; }
		void Initialize(TypingController controller);
		void Reset(RectTransform row);
	}
}
