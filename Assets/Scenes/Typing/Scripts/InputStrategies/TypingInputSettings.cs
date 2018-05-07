using UnityEngine;

namespace BlinkTalk.Typing.InputStrategies
{
	public static class TypingInputSettings
	{
		public static float KeyLerpFactor => 0.01f / Time.deltaTime;
		public static float KeyPauseTime = 1.5f;
		public static float RowLerpFactor => 0.0075f / Time.deltaTime;
		public static float RowPauseTime = 2f;
	}
}
