using System;
using UnityEngine;
using UnityEngine.UI;

namespace BlinkTalk.Typing.InputStrategies.KeyboardInputStrategies
{
	 public class KeySelection: MonoBehaviour
	 {
		  public KeySelectionKind Kind;

		  public void Execute(ITypingController controller)
		  {
				switch (Kind)
				{
					 case KeySelectionKind.BackSpace:
						  controller.BackSpace();
						  break;
					 case KeySelectionKind.Clear:
						  controller.ClearText();
						  break;
					 case KeySelectionKind.Letter:
						  var text = GetComponent<Text>();
						  controller.AddLetter(text.text[0]);
						  break;
					 case KeySelectionKind.Space:
						  controller.AddSpace();
						  break;
					 case KeySelectionKind.Unused:
						  break;
					 default:
						  throw new NotImplementedException(Kind + "");
				}
		  }
	 }

	 public enum KeySelectionKind
	 {
		  Letter = 0,
		  Space = 1,
		  BackSpace = 2,
		  Clear = 3,
		  Unused = 4
	 }
}
