using UnityEngine;
using UnityEngine.UI;

namespace BlinkTalk.Typing.InputStrategies.KeyboardInputStrategies
{
	 public class KeySelectionStrategy : MonoBehaviour, IKeySelectionStrategy
	 {
		  private bool Active;
		  private int SelectedKeyIndex;
		  private int CycleNumber;
		  private float LastShuffleTime;
		  private Vector2 TargetHighlighterPosition;
		  private RectTransform KeyHighlighter;
		  private RectTransform SelectedKey;
		  private RectTransform[] KeySelection;
		  private ITypingController Controller;
		  private IKeyboardInputStrategy KeyboardInputStrategy;

		  public void Initialize(ITypingController controller, IKeyboardInputStrategy keyboardInputStrategy)
		  {
				Controller = controller;
				KeyboardInputStrategy = keyboardInputStrategy;
				KeyHighlighter = controller.GetKeyHighlighter();
		  }

		  public void Activate(RectTransform row)
		  {
				Active = true;
				KeySelection = row.GetChildRectTransforms();
				transform.SetParent(row.transform.parent, true);
				SetCurrentKeySelectionIndex(0);
				CycleNumber = 0;
		  }

		  public void MayRemainActive(bool value)
		  {
				if (!value)
					 Active = false;
		  }

		  public void Indicate()
		  {
				var keySelection = SelectedKey.GetComponent<KeySelection>();
				keySelection.Execute(Controller);
		  }

		  private void Update()
		  {
				if (!Active)
				{
					 KeyHighlighter.anchoredPosition = new Vector2(0 - KeyHighlighter.rect.width, 0);
					 return;
				}

				if (Active && Time.time - LastShuffleTime >= TypingInputSettings.KeyPauseTime)
					 SetCurrentKeySelectionIndex(SelectedKeyIndex + 1);

				KeyHighlighter.anchoredPosition =
					Vector2.Lerp(KeyHighlighter.anchoredPosition, TargetHighlighterPosition, TypingInputSettings.KeyLerpFactor);
		  }

		  private void SetCurrentKeySelectionIndex(int index)
		  {
				Canvas.ForceUpdateCanvases();
				LastShuffleTime = Time.time;
				SelectedKeyIndex = index % KeySelection.Length;
				SelectedKey = KeySelection[SelectedKeyIndex];
				TargetHighlighterPosition = SelectedKey.anchoredPosition;
				if (SelectedKeyIndex == 0)
					 CycleNumber++;
				if (CycleNumber == 1 && SelectedKeyIndex > 1)
					 KeyboardInputStrategy.ChildInputStrategyExpired();
		  }
	 }
}
