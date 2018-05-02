using UnityEngine;
using UnityEngine.UI;

namespace BlinkTalk
{
	public static class ScrollRectExtensions
	{
		public static Vector2 GetContentSnapToPosition(this ScrollRect instance, RectTransform target)
		{
			Canvas.ForceUpdateCanvases();
			Vector2 result =
				(Vector2)instance.transform.InverseTransformPoint(instance.content.position)
				- (Vector2)instance.transform.InverseTransformPoint(target.position);
			return result;
		}
	}
}
