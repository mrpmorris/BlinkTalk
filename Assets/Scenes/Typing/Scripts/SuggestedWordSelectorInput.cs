using UnityEngine;

namespace BlinkTalk.Typing
{
    public class SuggestedWordSelectorInput : MonoBehaviour, IInputStrategy
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
            WordSuggestions = Controller.GetWordSelectionPanel().GetComponentsInChildren<WordSuggestionPresenter>();
            if (WordSuggestions.Length == 0)
            {
                Controller.InputStrategyFinished();
                return;
            }
            if (FocusCycler == null)
                FocusCycler = new FocusCycler(this, WordSuggestions.Length, FocusIndexChanged);
            FocusCycler.Start();
        }


        void IInputStrategy.ReceiveIndication()
        {
            Controller.GetSentenceBuilder().PushWord(SelectedWord.Word);
            Controller.InputStrategyFinished();
        }

        private void FocusIndexChanged(int focusIndex)
        {
            SelectedWord = WordSuggestions[focusIndex];
            Controller.SetIndicatorRect(SelectedWord.GetComponent<RectTransform>());
        }
    }
}