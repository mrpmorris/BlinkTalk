using BlinkTalk.Typing.InputCoroutines;
using System;
using System.Collections;
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
		private ControllerState State = ControllerState.Typing;
		private Coroutine CurrentInputCoroutine;
		// Input coroutines
		private KeyboardInputCoroutine KeyboardInputCoroutine;

		private void SetState(ControllerState newState)
		{
			StopCurrentInputCoroutine();

			State = newState;
			switch (State)
			{
				case ControllerState.Typing:
					SetCurrentInputCoroutine(KeyboardInputCoroutine.Execute());
					break;
				case ControllerState.WordPicklist:
					break;
				default:
					throw new NotImplementedException(State.ToString());
			}
		}

		private void SetCurrentInputCoroutine(IEnumerator coroutine)
		{
			StartCoroutine(coroutine);
		}

		private void StopCurrentInputCoroutine()
		{
			if (CurrentInputCoroutine != null)
			{
				StopCoroutine(CurrentInputCoroutine);
				CurrentInputCoroutine = null;
			}
		}

		private void Start()
		{
			this.EnsureAssigned(x => x.KeyboardSelector);
			this.EnsureAssigned(x => x.MainContentSelector);
			this.EnsureAssigned(x => x.TextInputFeature);
			this.EnsureAssigned(x => x.InputText);

			KeyboardSelectorClientArea = this.EnsureAssigned(x => x.KeyboardSelector.content);
			KeyboardSelectionGroups = KeyboardSelectorClientArea.GetChildren().Select(x => x.GetComponent<RectTransform>()).ToArray();

			KeyboardInputCoroutine = gameObject.AddComponent<KeyboardInputCoroutine>();
			KeyboardInputCoroutine.Initialize(KeyboardSelectorClientArea, KeyboardSelectionGroups);

		}
	}
}
