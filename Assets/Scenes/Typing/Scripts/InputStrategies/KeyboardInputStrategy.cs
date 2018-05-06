using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BlinkTalk.Typing.InputStrategies
{
	public class KeyboardInputStrategy: MonoBehaviour, IInputStrategy
	{
		private int CurrentSelectionGroupIndex;
		private RectTransform CurrentSelectionGroup;

		private Vector2 TargetScrollPosition;
		private ScrollRect KeyboardScrollRect;
		private RectTransform KeyboardSelectorContent;
		private RectTransform[] KeyboardSelectionGroups;

		public void Initialize(TypingController controller)
		{
			KeyboardScrollRect = controller.KeyboardSelector;
			KeyboardSelectorContent = KeyboardScrollRect.content;
			KeyboardSelectionGroups = KeyboardSelectorContent.GetChildren().Select(x => x.GetComponent<RectTransform>()).ToArray();
			SetCurrentSelectionGroupIndex(0);
		}

		public IEnumerator GetEnumerator()
		{
			while (true)
			{
				yield return new WaitForSeconds(2);
				SetCurrentSelectionGroupIndex(CurrentSelectionGroupIndex + 1);
			}
		}

		private void Update()
		{
			KeyboardSelectorContent.localPosition = 
				Vector3.Lerp(KeyboardSelectorContent.localPosition, TargetScrollPosition, 0.5f);
		}

		private void SetCurrentSelectionGroupIndex(int index)
		{
			Canvas.ForceUpdateCanvases();
			CurrentSelectionGroupIndex = index % KeyboardSelectionGroups.Length;
			CurrentSelectionGroup = KeyboardSelectionGroups[CurrentSelectionGroupIndex];
			TargetScrollPosition = KeyboardScrollRect.GetSnapToPositionToBringChildIntoView(CurrentSelectionGroup);
			Debug.Log("TargetScrollPosition = " + TargetScrollPosition);
		}

	}
}
