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
		private ScrollRect KeyboardScrollRect;
		private RectTransform KeyboardSelectorClientArea;
		private RectTransform[] KeyboardSelectionGroups;

		public IEnumerator Execute()
		{
			SetCurrentSelectionGroupIndex(0);
			while (true)
			{
				SetCurrentSelectionGroupIndex(CurrentSelectionGroupIndex + 1);
				yield return new WaitForSeconds(2);
			}
		}

		internal void Initialize(ScrollRect keyboardScrollRect, RectTransform[] keyboardSelectionGroups)
		{
			KeyboardScrollRect = keyboardScrollRect;
			KeyboardSelectorClientArea = keyboardScrollRect.content;
			KeyboardSelectionGroups = keyboardSelectionGroups;
			SetCurrentSelectionGroupIndex(keyboardSelectionGroups.Length - 1);
		}

		private void Update()
		{
			KeyboardSelectorClientArea.position = Vector3.Lerp(KeyboardSelectorClientArea.position, TargetScrollPosition, 0.5f);
		}

		private void SetCurrentSelectionGroupIndex(int index)
		{
			CurrentSelectionGroupIndex = index % KeyboardSelectionGroups.Length;
			CurrentSelectionGroup = KeyboardSelectionGroups[CurrentSelectionGroupIndex];
			TargetScrollPosition = KeyboardScrollRect.GetContentSnapToPosition(CurrentSelectionGroup);
		}
	}
}
