using UnityEngine;
using UnityEngine.UI;

namespace BlinkTalk.Typing.InputStrategies.KeyboardInputStrategies
{
	public class KeySelectionStrategy : MonoBehaviour, IKeySelectionStrategy
	{
		public bool Live { get; set; }

		private int SelectedKeyIndex;
		private int CycleNumber;
		private float LastShuffleTime;
		private Vector2 TargetHighlighterPosition;
		private TypingController Controller;
		private RectTransform KeyHighlighter;
		private RectTransform SelectedKey;
		private RectTransform[] KeySelection;

		public string SelectedKeyText
		{
			get
			{
				return SelectedKey.GetComponent<Text>().text; ;
			}
		}

		public void Initialize(TypingController controller)
		{
			Controller = controller;
			KeyHighlighter = controller.KeyHighlighter;
		}

		public void Reset(RectTransform row)
		{
			KeySelection = row.GetChildRectTransforms();
			transform.SetParent(row.transform.parent, true);
			SetCurrentKeySelectionIndex(0);
			CycleNumber = 0;
		}

		private void Update()
		{
			if (!Live)
			{
				KeyHighlighter.position = new Vector3(0 - KeyHighlighter.rect.width, KeyHighlighter.position.y);
				return;
			}

			if (Live && Time.time - LastShuffleTime >= TypingInputSettings.KeyPauseTime)
				SetCurrentKeySelectionIndex(SelectedKeyIndex + 1);

			KeyHighlighter.anchoredPosition =
				Vector2.Lerp(KeyHighlighter.anchoredPosition, TargetHighlighterPosition, TypingInputSettings.KeyLerpFactor);
		}

		private void SetCurrentKeySelectionIndex(int index)
		{
			LastShuffleTime = Time.time;
			SelectedKeyIndex = index % KeySelection.Length;
			SelectedKey = KeySelection[SelectedKeyIndex];
			TargetHighlighterPosition = SelectedKey.anchoredPosition;
			if (SelectedKeyIndex == 0)
				CycleNumber++;
		}
	}
}
