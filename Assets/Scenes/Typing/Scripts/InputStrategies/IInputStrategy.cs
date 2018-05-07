namespace BlinkTalk.Typing.InputStrategies
{
	public interface IInputStrategy
	{
		bool Live { get; set; }
		void Initialize(TypingController controller);
	}
}
