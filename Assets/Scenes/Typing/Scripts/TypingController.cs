using BlinkTalk.Typing.InputStrategies.KeyboardInputStrategies;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BlinkTalk.Typing
{
	 public class TypingController : MonoBehaviour, ITypingController
	 {
		  public bool HasIndicated { get; private set; }
		  public ScrollRect KeyboardSelector;
		  public Text InputText;
		  public RectTransform KeyHighlighter;
		  [Space]
		  public Button IndicateButton;

		  private RectTransform KeyboardSelectorClientArea;
		  private RectTransform[] KeyboardSelectionGroups;
		  private IKeyboardInputStrategy KeyboardInputStrategy;
		  private TypingControllerInputState State { get { return _state; } set { SetState(value); } }
		  private TypingControllerInputState _state;

		  #region ITypingController
		  ScrollRect ITypingController.GetKeyboardSelector() => KeyboardSelector;
		  Text ITypingController.GetInputText() => InputText;
		  RectTransform ITypingController.GetKeyHighlighter() => KeyHighlighter;
		  Button ITypingController.GetIndicateButton() => IndicateButton;
		  #endregion

		  public void AddLetter(char letter)
		  {
				InputText.text += letter;
		  }

		  private void Start()
		  {
				this.EnsureAssigned(x => x.KeyboardSelector);
				this.EnsureAssigned(x => x.InputText);
				this.EnsureAssigned(x => x.IndicateButton).onClick.AddListener(OnIndicateButtonClick);
				this.EnsureAssigned(x => x.KeyHighlighter);

				KeyboardSelectorClientArea = this.EnsureAssigned(x => x.KeyboardSelector.content);
				KeyboardSelectionGroups = KeyboardSelectorClientArea.GetChildRectTransforms();

				KeyboardInputStrategy = (IKeyboardInputStrategy)gameObject.AddComponent<KeyboardInputStrategy>();
				KeyboardInputStrategy.Initialize(this);
				State = TypingControllerInputState.Keyboard;
		  }

		  private void SetState(TypingControllerInputState value)
		  {
				_state = value;
				switch (State)
				{
					 case TypingControllerInputState.Keyboard:
						  KeyboardInputStrategy.Activate();
						  break;

					 default:
						  throw new NotImplementedException(State + "");
				}
		  }

		  private void OnIndicateButtonClick()
		  {
				HasIndicated = true;
				StartCoroutine(SetHasIndicatedToFalse());
		  }

		  private IEnumerator SetHasIndicatedToFalse()
		  {
				yield return new WaitForEndOfFrame();
				HasIndicated = false;
		  }

		  public void AddSpace()
		  {
				InputText.text += " ";
		  }

		  public void BackSpace()
		  {
				if (InputText.text.Length > 0)
					 InputText.text = InputText.text.Remove(InputText.text.Length - 1);
		  }

		  public void ClearText()
		  {
				InputText.text = "";
		  }
	 }
}
