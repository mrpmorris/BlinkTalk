using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlinkTalk
{
	public static class TransformExtensions
	{
		public static IEnumerable<GameObject> GetChildren(this Transform instance)
		{
			if (instance == null)
				throw new ArgumentNullException(nameof(instance));

			var result = new List<GameObject>();
			for (int i = 0; i < instance.childCount; i++)
				result.Add(instance.GetChild(i).gameObject);
			return result;
		}
	}
}
