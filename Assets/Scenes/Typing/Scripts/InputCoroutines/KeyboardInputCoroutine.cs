using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BlinkTalk.Typing.InputCoroutines
{
	public class KeyboardInputCoroutine: MonoBehaviour
	{
		private int CurrentSelectionGroupIndex;
		private RectTransform CurrentSelectionGroup;

		private Vector2 TargetScrollPosition;
		private RectTransform KeyboardSelectorClientArea;
		private RectTransform[] KeyboardSelectionGroups;

		public IEnumerator Execute()
		{
			yield return new WaitForSeconds(0.5f);
			yield return new WaitForSeconds(0.5f);
			yield return new WaitForSeconds(0.5f);
		}

		internal void Initialize(RectTransform keyboardSelectorClientArea, RectTransform[] keyboardSelectionGroups)
		{
			KeyboardSelectorClientArea = keyboardSelectorClientArea;
			KeyboardSelectionGroups = keyboardSelectionGroups;
			SetCurrentSelectionGroupIndex(keyboardSelectionGroups.Length - 1);
		}

		private void Update()
		{
			KeyboardSelectorClientArea.position = Vector3.Lerp(KeyboardSelectorClientArea.position, TargetScrollPosition, 0.1f);
		}

		private void SetCurrentSelectionGroupIndex(int index)
		{
			CurrentSelectionGroupIndex = index % KeyboardSelectionGroups.Length;
			CurrentSelectionGroup = KeyboardSelectionGroups[CurrentSelectionGroupIndex];
			TargetScrollPosition = CurrentSelectionGroup.rect.position;
			TargetScrollPosition = new Vector3(0, -TargetScrollPosition.y, 0);
		}
	}
}
