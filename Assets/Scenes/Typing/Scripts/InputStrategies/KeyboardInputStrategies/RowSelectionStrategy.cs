using UnityEngine;
using UnityEngine.UI;

namespace BlinkTalk.Typing.InputStrategies.KeyboardInputStrategies
{
	public class RowSelectionStrategy: MonoBehaviour, IRowSelectionStrategy
	{
		public RectTransform SelectedRow { get; private set; }

		public bool Live;
		private int SelectedRowIndex;
		private float LastShuffleTime;
		private Vector2 TargetScrollPosition;
		private ScrollRect KeyboardScrollRect;
		private RectTransform KeyboardSelectorContent;
		private RectTransform[] RowSelection;
		private ITypingController Controller;

		public void Initialize(ITypingController controller)
		{
			Controller = controller;
			KeyboardScrollRect = Controller.GetKeyboardSelector();
			KeyboardSelectorContent = KeyboardScrollRect.content;
			RowSelection = KeyboardSelectorContent.GetChildRectTransforms();
			SetSelectedRowIndex(0);
		}

		public void Activate()
		{
			Live = true;
			SetSelectedRowIndex(0);
		}

		private void Update()
		{
			if (Controller.HasIndicated)
				Live = false;

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
