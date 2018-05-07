using BlinkTalk.Typing;

namespace BlinkTalk.Typing.InputStrategies.KeyboardInputStrategies
{
	public interface IKeyboardInputStrategy
	{
		bool Live { get; set; }
		void Initialize(TypingController controller);
	}
}
