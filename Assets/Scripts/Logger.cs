using UnityEngine;
using UnityEngine.SceneManagement;

namespace BlinkTalk
{
	public class Logger : MonoBehaviour
	{
		private static bool ExceptionHandlerHooked;
		private static bool HasUnhandledException;

		public static void Log(string message)
		{
			if (HasUnhandledException)
				return;

			Debug.Log(message);
		}

		private void Awake()
		{
			Log("Logging enabled");
			if (ExceptionHandlerHooked)
				return;

			Application.logMessageReceived += Application_logMessageReceived;
			ExceptionHandlerHooked = true;
		}

		private void Application_logMessageReceived(string condition, string stackTrace, LogType type)
		{
			if (type != LogType.Assert && type != LogType.Error && type != LogType.Exception)
				return;

			Debug.LogError(condition);
			Debug.LogError(stackTrace);
			HasUnhandledException = true;
			Application.logMessageReceived -= Application_logMessageReceived;
			SceneManager.LoadScene("ErrorScene");
		}
	}
}