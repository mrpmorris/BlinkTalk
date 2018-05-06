using UnityEngine.EventSystems;

namespace BlinkTalk
{
	public interface IIndicationHandler: IEventSystemHandler
	{
		void OnIndicate();
	}
}
