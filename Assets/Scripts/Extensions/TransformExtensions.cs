using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlinkTalk
{
	public static class TransformExtensions
	{
		public static GameObject[] GetChildGameObjects(this RectTransform instance)
		{
			if (instance == null)
				throw new ArgumentNullException(nameof(instance));

			var result = new List<GameObject>();
			for (int i = 0; i < instance.childCount; i++)
				result.Add(instance.GetChild(i).gameObject);
			return result.ToArray();
		}

		public static RectTransform[] GetChildRectTransforms(this RectTransform instance)
		{
			return instance.GetChildGameObjects()
				.Select(x => x.GetComponent<RectTransform>())
				.ToArray();
		}
	}
}
