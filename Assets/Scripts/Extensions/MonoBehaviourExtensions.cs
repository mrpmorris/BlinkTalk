using System;
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
				throw new NullReferenceException(value.Body.ToString());
			return result;
		}
	}
}
