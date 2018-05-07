using UnityEngine;
using UnityEngine.UI;

namespace BlinkTalk.Typing.InputStrategies.KeyboardInputStrategies
{
	public class KeySelectionStrategy : MonoBehaviour, IKeySelectionStrategy
	{
		private bool Live;
		private int SelectedKeyIndex;
		private int CycleNumber;
		private float LastShuffleTime;
		private Vector2 TargetHighlighterPosition;
		private ITypingController Controller;
		private RectTransform KeyHighlighter;
		private RectTransform SelectedKey;
		private RectTransform[] KeySelection;

		public string SelectedKeyText => SelectedKey.GetComponent<Text>().text; 

		public void Initialize(ITypingController controller)
		{
			Controller = controller;
			KeyHighlighter = controller.GetKeyHighlighter();
		}

		public void Activate(RectTransform row)
		{
			Live = true;
			KeySelection = row.GetChildRectTransforms();
			transform.SetParent(row.transform.parent, true);
			SetCurrentKeySelectionIndex(0);
			CycleNumber = 0;
		}

		private void Update()
		{
			if (Controller.HasIndicated)
				Live = false;

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
