using UnityEngine;
using UnityEngine.UI;

namespace BlinkTalk.Typing
{
	public class TypeController : MonoBehaviour
	{
		public ScrollRect LetterSelector;
		public ScrollRect MainContentSelector;

		private RectTransform LetterSelectorContent;

		private void Start()
		{
			this.EnsureAssigned(x => x.LetterSelector);
			this.EnsureAssigned(x => x.MainContentSelector);

			LetterSelectorContent = this.EnsureAssigned(x => x.LetterSelector.content);
		}
	}
}
