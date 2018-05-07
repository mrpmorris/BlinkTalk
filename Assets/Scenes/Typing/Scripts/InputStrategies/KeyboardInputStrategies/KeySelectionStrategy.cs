using UnityEngine;

namespace BlinkTalk.Typing.InputStrategies.KeyboardInputStrategies
{
	public class KeySelectionStrategy : MonoBehaviour, IInputStrategy
	{
		public bool Live { get; set; }

		private int SelectedKeyIndex;
		private float LastShuffleTime;
		private Vector2 TargetHighlighterPosition;
		private TypingController Controller;
		private RectTransform KeyHighlighter;
		private RectTransform SelectedKey;
		private RectTransform[] KeySelection;

		public void Initialize(TypingController controller)
		{
			Controller = controller;
			KeyHighlighter = controller.KeyHighlighter;
		}

		public void SetRow(RectTransform row)
		{
			KeySelection = row.GetChildRectTransforms();
			transform.parent = row.transform.parent;
			SetCurrentKeySelectionIndex(0);
			LastShuffleTime = Time.time;
		}

		private void Update()
		{
			if (!Live)
			{
				KeyHighlighter.position = new Vector3(0 - KeyHighlighter.rect.width, KeyHighlighter.position.y);
				return;
			}

			if (Live && Time.time - LastShuffleTime >= TypingInputSettings.KeyPauseTime)
			{
				SetCurrentKeySelectionIndex(SelectedKeyIndex + 1);
				LastShuffleTime = Time.time;
			}
			KeyHighlighter.anchoredPosition =
				Vector2.Lerp(KeyHighlighter.anchoredPosition, TargetHighlighterPosition, TypingInputSettings.KeyLerpFactor);
		}

		private void SetCurrentKeySelectionIndex(int index)
		{
			SelectedKeyIndex = index % KeySelection.Length;
			SelectedKey = KeySelection[SelectedKeyIndex];
			TargetHighlighterPosition = SelectedKey.anchoredPosition;
		}
	}
}
