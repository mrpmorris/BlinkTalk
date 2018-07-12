using System.Collections;
using System.Linq;
using UnityEngine;

namespace BlinkTalk.Typing
{
    public class WordSuggestionSelectorInput : MonoBehaviour, IInputStrategy
    {
        private WordSuggestionPresenter SelectedWord;
        private ITypingController Controller;
        private FocusCycler FocusCycler;
        private WordSuggestionPresenter[] WordSuggestions;

        void IInputStrategy.ChildStrategyActivated(IInputStrategy inputStrategy) { }
        void IInputStrategy.Terminated() { }

        void IInputStrategy.Initialize(ITypingController controller)
        {
            Controller = controller;
            StartCoroutine(RestartFocusCycler());
        }

        void IInputStrategy.ReceiveIndication()
        {
            Controller.GetSentenceBuilder().PushWord(SelectedWord.Word);
            FocusCycler.Stop();
            StartCoroutine(RestartFocusCycler());
        }

        private void FocusIndexChanged(int focusIndex)
        {
            SelectedWord = WordSuggestions[focusIndex];
            Controller.SetIndicatorRect(SelectedWord.GetComponent<RectTransform>());
            if (FocusCycler.FocusChangeCount > WordSuggestions.Length + 1)
                Controller.InputStrategyFinished();
        }

        private IEnumerator RestartFocusCycler()
        {
            SelectedWord = null;
            yield return new WaitForEndOfFrame();
            WordSuggestions = Controller.GetWordSelectionPanel().GetComponentsInChildren<WordSuggestionPresenter>()
                .Where(x => x)
                .ToArray();
            if (WordSuggestions.Length == 0)
            {
                Controller.InputStrategyFinished();
            } else
            {
                FocusCycler = new FocusCycler(this, FocusIndexChanged);
                FocusCycler.Start(WordSuggestions.Length);
            }
        }
    }
}