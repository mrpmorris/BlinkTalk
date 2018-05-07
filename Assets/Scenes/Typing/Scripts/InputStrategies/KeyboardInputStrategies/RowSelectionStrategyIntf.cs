using UnityEngine;

namespace BlinkTalk.Typing.InputStrategies.KeyboardInputStrategies
{
	public interface IRowSelectionStrategy
	{
		RectTransform SelectedRow { get; }
		void Initialize(ITypingController controller);
		void Activate();
	}
}
