using UnityEngine;
using UnityEngine.UI;

namespace BlinkTalk.Typing
{
	 public interface ITypingController
	 {
		  bool HasIndicated { get; }
		  ScrollRect GetKeyboardSelector();
		  Text GetInputText();
		  RectTransform GetKeyHighlighter();
		  Button GetIndicateButton();
		  void AddLetter(char letter);
	 }
}
