using BlinkTalk.Typing.InputStrategies;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BlinkTalk.Typing
{
	public class TypingController : MonoBehaviour
	{
		public ScrollRect KeyboardSelector;
		public ScrollRect MainContentSelector;
		[Header("Text input feature")]
		public RectTransform TextInputFeature;
		public Text InputText;

		private RectTransform KeyboardSelectorClientArea;
		private RectTransform[] KeyboardSelectionGroups;
		private KeyboardInputStrategy KeyboardInputStrategy;

		private void Start()
		{
			this.EnsureAssigned(x => x.KeyboardSelector);
			this.EnsureAssigned(x => x.MainContentSelector);
			this.EnsureAssigned(x => x.TextInputFeature);
			this.EnsureAssigned(x => x.InputText);

			KeyboardSelectorClientArea = this.EnsureAssigned(x => x.KeyboardSelector.content);
			KeyboardSelectionGroups = KeyboardSelectorClientArea.GetChildren().Select(x => x.GetComponent<RectTransform>()).ToArray();

			KeyboardInputStrategy = gameObject.AddComponent<KeyboardInputStrategy>();
			KeyboardInputStrategy.Initialize(this);

			StartCoroutine(KeyboardInputStrategy.GetEnumerator());
		}
	}
}
