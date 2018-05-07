using System;
using System.Collections;
using System.Linq.Expressions;
using UnityEngine;

namespace BlinkTalk
{
	public static class MonoBehaviourExtensions
	{
		public static TValue EnsureAssigned<TInstance, TValue>(this TInstance instance, Expression<Func<TInstance, TValue>> value)
			where TInstance: MonoBehaviour
			where TValue: class
		{
			if (instance == null)
				throw new ArgumentNullException(nameof(instance));
			if (value == null)
				throw new ArgumentNullException(nameof(value));

			Func<TInstance, TValue> getValue = value.Compile();
			TValue result = getValue(instance);
			if (result == null)
				throw new NullReferenceException("EnsureAssigned assertion failed: " + value.Body.ToString());
			return result;
		}

		public static void ExecuteAtEndOfFrame(this MonoBehaviour instance, Action action)
		{
			if (action == null)
				throw new ArgumentNullException(nameof(action));
			instance.StartCoroutine(ExecuteAtEndOfFrameCoroutine(action));
		}

		private static IEnumerator ExecuteAtEndOfFrameCoroutine(Action action)
		{
			yield return new WaitForEndOfFrame();
			action();
		}

	}
}
