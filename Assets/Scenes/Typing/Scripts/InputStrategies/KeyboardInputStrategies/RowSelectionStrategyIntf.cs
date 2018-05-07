using UnityEngine;

namespace BlinkTalk.Typing.InputStrategies.KeyboardInputStrategies
{
	public interface IRowSelectionStrategy
	{
		bool Live { get; set; }
		RectTransform SelectedRow { get; }
		void Initialize(TypingController controller);
		void Reset();
	}
}
