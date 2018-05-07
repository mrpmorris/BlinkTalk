using UnityEngine;
using UnityEngine.UI;

namespace BlinkTalk.Typing.InputStrategies.KeyboardInputStrategies
{
	public class RowSelectionStrategy: MonoBehaviour, IInputStrategy
	{
		public bool Live { get; set; }
		public RectTransform SelectedRow { get; private set; }

		private int SelectedRowIndex;
		private float LastShuffleTime;

		private Vector2 TargetScrollPosition;
		private ScrollRect KeyboardScrollRect;
		private RectTransform KeyboardSelectorContent;
		private RectTransform[] RowSelection;

		public void Initialize(TypingController controller)
		{
			KeyboardScrollRect = controller.KeyboardSelector;
			KeyboardSelectorContent = KeyboardScrollRect.content;
			RowSelection = KeyboardSelectorContent.GetChildRectTransforms();
			Reset();
		}

		public void Reset()
		{
			SetSelectedRowIndex(0);
		}

		private void Update()
		{
			if (Live && Time.time - LastShuffleTime >= TypingInputSettings.RowPauseTime)
				SetSelectedRowIndex(SelectedRowIndex + 1);

			KeyboardSelectorContent.localPosition =
				Vector3.Lerp(KeyboardSelectorContent.localPosition, TargetScrollPosition, TypingInputSettings.RowLerpFactor);
		}

		private void SetSelectedRowIndex(int index)
		{
			Canvas.ForceUpdateCanvases();
			SelectedRowIndex = index % RowSelection.Length;
			SelectedRow = RowSelection[SelectedRowIndex];
			TargetScrollPosition = KeyboardScrollRect.GetSnapToPositionToBringChildIntoView(SelectedRow);
			LastShuffleTime = Time.time;
		}

	}
}
