using UnityEngine;

namespace BlinkTalk
{
	public static class GameObjectExtensions
	{
		public static TService AddServiceComponent<TService, TImplementor>(this GameObject instance)
			where TImplementor: Component, TService
		{
			return instance.AddComponent<TImplementor>();
		}
	}
}
