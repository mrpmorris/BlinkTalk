using UnityEngine;
using UnityEngine.SceneManagement;

public class Logger : MonoBehaviour
{
	private static bool exceptionHandlerHooked;
	private static bool hasUnhandledException;

	public static void Log(string message)
	{
		if (hasUnhandledException)
			return;

		Debug.Log(message);
	}

	public void Awake()
	{
		if (exceptionHandlerHooked)
			return;

		Application.logMessageReceived += Application_logMessageReceived;
		exceptionHandlerHooked = true;
	}

	private void Application_logMessageReceived(string condition, string stackTrace, LogType type)
	{
		if (type != LogType.Assert && type != LogType.Error && type != LogType.Exception)
			return;

		Debug.LogError(condition);
		hasUnhandledException = true;
		Application.logMessageReceived -= Application_logMessageReceived;
	}
}
