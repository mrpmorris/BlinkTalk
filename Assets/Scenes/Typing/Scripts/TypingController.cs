using BlinkTalk.Typing.InputStrategies.KeyboardInputStrategies;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BlinkTalk.Typing
{
	public class TypingController : MonoBehaviour
	{
		public bool HasIndicated { get; private set; }
		public ScrollRect KeyboardSelector;
		public Text InputText;
		public RectTransform KeyHighlighter;
		[Space]
		public Button IndicateButton;

		private RectTransform KeyboardSelectorClientArea;
		private RectTransform[] KeyboardSelectionGroups;
		private KeyboardInputStrategy KeyboardInputStrategy;
		private TypingControllerInputState State { get { return _state; } set { SetState(value); } }
		private TypingControllerInputState _state;

		private void Start()
		{
			this.EnsureAssigned(x => x.KeyboardSelector);
			this.EnsureAssigned(x => x.InputText);
			this.EnsureAssigned(x => x.IndicateButton).onClick.AddListener(OnIndicateButtonClick);
			this.EnsureAssigned(x => x.KeyHighlighter);

			KeyboardSelectorClientArea = this.EnsureAssigned(x => x.KeyboardSelector.content);
			KeyboardSelectionGroups = KeyboardSelectorClientArea.GetChildRectTransforms();

			KeyboardInputStrategy = gameObject.AddComponent<KeyboardInputStrategy>();
			KeyboardInputStrategy.Initialize(this);
			State = TypingControllerInputState.Keyboard;
		}

		private void SetState(TypingControllerInputState value)
		{
			_state = value;
			KeyboardInputStrategy.Live = value == TypingControllerInputState.Keyboard;
		}

		private void OnIndicateButtonClick()
		{
			HasIndicated = true;
			StartCoroutine(SetHasIndicatedToFalse());
		}

		private IEnumerator SetHasIndicatedToFalse()
		{
			yield return new WaitForEndOfFrame();
			HasIndicated = false;
		}

	}
}
