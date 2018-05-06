using System.Collections;

namespace BlinkTalk.Typing.InputStrategies
{
	public interface IInputStrategy
	{
		void Initialize(TypingController controller);
		IEnumerator GetEnumerator();
	}
}
