﻿using BlinkTalk.Typing;

namespace BlinkTalk.Typing.InputStrategies.KeyboardInputStrategies
{
	public interface IKeyboardInputStrategy
	{
		void Initialize(ITypingController controller);
		void Activate();
		void MayRemainActive(bool value);
		void ChildInputStrategyExpired();
	}
}