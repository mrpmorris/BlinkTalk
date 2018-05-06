using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BlinkTalk.Typing.InputStrategies
{
	public class KeyboardInputStrategy: MonoBehaviour, IInputStrategy, IIndicationHandler
	{
		private bool IsSelectingKey;
		private int CurrentSelectionGroupIndex;
		private float LastShuffleTime;
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

		private void Update()
		{
			if (!IsSelectingKey && Time.time - LastShuffleTime >= 2f)
			{
				SetCurrentSelectionGroupIndex(CurrentSelectionGroupIndex + 1);
				LastShuffleTime = Time.time;
			}
			KeyboardSelectorContent.localPosition = 
				Vector3.Lerp(KeyboardSelectorContent.localPosition, TargetScrollPosition, 0.5f);
		}

		private void SetCurrentSelectionGroupIndex(int index)
		{
			Canvas.ForceUpdateCanvases();
			CurrentSelectionGroupIndex = index % KeyboardSelectionGroups.Length;
			CurrentSelectionGroup = KeyboardSelectionGroups[CurrentSelectionGroupIndex];
			TargetScrollPosition = KeyboardScrollRect.GetSnapToPositionToBringChildIntoView(CurrentSelectionGroup);
		}

		public void OnIndicate()
		{
			IsSelectingKey = true;
		}
	}
}
