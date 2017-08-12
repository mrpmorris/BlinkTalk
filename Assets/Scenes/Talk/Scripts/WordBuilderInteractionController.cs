using Assets.Scripts.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Assets.Scripts
{
    public class WordBuilderInteractionController : IInteractionController
    {
        public InteractionControllerViewModel viewModel { get; private set; }
        event EventHandler IInteractionController.completed { add { } remove { } }
        public event EventHandler<InteractionControllerEventArgs> childControllerAdded;
        public event EventHandler<InteractionControllerViewModelEventArgs> viewModelChanged;

        private LetterSelector letterSelector = new LetterSelector();
        private string[] suggestedWords = new string[0];
        private WordSelectorInteractionController wordSelector;

        private enum ActionButtonKind
        {
            Back,
            Delete,
            Speak,
            Blank
        }

        public WordBuilderInteractionController()
        {
            this.viewModel = new InteractionControllerViewModel();
            letterSelector.letterSelected += LetterSelected;
            UpdateSuggestedWords();
            UpdateViewModel();
        }

        public void ClickAction()
        {
            switch (GetActionButtonKind())
            {
                case ActionButtonKind.Back:
                    letterSelector.DrillUp();
                    break;
                case ActionButtonKind.Delete:
                    viewModel.wordInputText = viewModel.wordInputText.Remove(viewModel.wordInputText.Length - 1);
                    break;
                case ActionButtonKind.Speak:
                    SentenceBuilder.SpeakSentence();
                    ResetState();
                    break;
                case ActionButtonKind.Blank:
                    break;
                default:
                    throw new NotImplementedException(GetActionButtonKind() + "");
            }
            UpdateViewModel();
        }

        public void ClickSelection1()
        {
            if (suggestedWords.Length == 0)
                return;

            if (suggestedWords.Length == 1)
            {
                SentenceBuilder.AddWord(suggestedWords[0]);
                ResetState();
                return;
            }

            string word1 = suggestedWords[0];
            string word2 = suggestedWords.Length > 1 ? suggestedWords[1] : "";
            string word3 = suggestedWords.Length > 2 ? suggestedWords[2] : "";

            wordSelector = new WordSelectorInteractionController(
                word1: word1,
                word2: word2,
                word3: word3);
            wordSelector.completed += WordSelectorCompleted;
            OnChildControllerAdded(wordSelector);
        }

        public void ClickSelection2()
        {
            letterSelector.DrillDownLeft();
            UpdateViewModel();
        }

        public void ClickSelection3()
        {
            letterSelector.DrillDownRight();
            UpdateViewModel();
        }

        private void WordSelectorCompleted(object sender, EventArgs e)
        {
            wordSelector.completed -= WordSelectorCompleted;
            ResetState();
        }

        private void ResetState()
        {
            viewModel.wordInputText = "";
            letterSelector.DrillUpCompletely();
            UpdateViewModel();
        }

        private void OnChildControllerAdded(IInteractionController controller)
        {
            var childControllerAdded = this.childControllerAdded;
            if (childControllerAdded != null)
                childControllerAdded(this, new InteractionControllerEventArgs(controller));
        }

        private void UpdateViewModel()
        {
            UpdateSuggestedWords();
            viewModel.selection1Text = string.Join("\r\n", suggestedWords);
            viewModel.selection2Text = letterSelector.GetLeftRange();
            viewModel.selection3Text = letterSelector.GetRightRange();
            viewModel.actionText = letterSelector.CanDrillUp ? "Back" : "Delete";
            viewModel.sentenceText = SentenceBuilder.GetSentence();
            switch (GetActionButtonKind())
            {
                case ActionButtonKind.Back:
                    viewModel.actionText = "Back";
                    break;
                case ActionButtonKind.Blank:
                    viewModel.actionText = "";
                    break;
                case ActionButtonKind.Delete:
                    viewModel.actionText = "Delete";
                    break;
                case ActionButtonKind.Speak:
                    viewModel.actionText = "Speak";
                    break;
                default:
                    throw new NotImplementedException(GetActionButtonKind() + "");
            }
            var viewModelChanged = this.viewModelChanged;
            if (viewModelChanged != null)
                viewModelChanged(this, new InteractionControllerViewModelEventArgs(viewModel));
        }

        private void LetterSelected(object sender, LetterSelector.LetterSelectedEventArgs e)
        {
            viewModel.wordInputText += e.letter;
            UpdateSuggestedWords();
            UpdateViewModel();
        }

        private void UpdateSuggestedWords()
        {
            var result = new List<string>();
            char[] potentialNextChars = letterSelector.GetFullRange().ToCharArray();
            List<string> suggestionsFromDB = WordList.GetSuggestions(viewModel.wordInputText, potentialNextChars);
            if (!string.IsNullOrEmpty(viewModel.wordInputText) && !suggestionsFromDB.Contains(viewModel.wordInputText.ToLowerInvariant()))
                result.Add(viewModel.wordInputText.ToLowerInvariant());
            result.AddRange(suggestionsFromDB);
            suggestedWords = result.Take(3).ToArray();
        }

        private ActionButtonKind GetActionButtonKind()
        {
            if (letterSelector.CanDrillUp)
                return ActionButtonKind.Back;
            if (!string.IsNullOrEmpty(viewModel.wordInputText))
                return ActionButtonKind.Delete;
            if (!string.IsNullOrEmpty(SentenceBuilder.GetSentence()))
                return ActionButtonKind.Speak;
            return ActionButtonKind.Blank;

        }

    }
}
