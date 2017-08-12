using System;
using Assets.Scripts.ViewModels;

namespace Assets.Scripts
{
    public class WordSelectorInteractionController : IInteractionController
    {
        public InteractionControllerViewModel viewModel { get; private set; }
        public event EventHandler completed;
        event EventHandler<InteractionControllerEventArgs> IInteractionController.childControllerAdded { add { } remove { } }
        event EventHandler<InteractionControllerViewModelEventArgs> IInteractionController.viewModelChanged { add { } remove { } }

        private readonly string word1;
        private readonly string word2;
        private readonly string word3;

        public WordSelectorInteractionController(string word1, string word2, string word3)
        {
            this.word1 = word1;
            this.word2 = word2;
            this.word3 = word3;
            this.viewModel = new InteractionControllerViewModel();
            UpdateViewModel();
        }

        public void ClickAction()
        {
            OnCompleted();
        }

        public void ClickSelection1()
        {
            SentenceBuilder.AddWord(word1);
            OnCompleted();
        }

        public void ClickSelection2()
        {
            SentenceBuilder.AddWord(word2);
            OnCompleted();
        }

        public void ClickSelection3()
        {
            SentenceBuilder.AddWord(word3);
            OnCompleted();
        }

        private void UpdateViewModel()
        {
            viewModel.wordInputText = "Select a word";
            viewModel.selection1Text = word1;
            viewModel.selection2Text = word2;
            viewModel.selection3Text = word3;
            viewModel.actionText = "Back";
            viewModel.sentenceText = SentenceBuilder.GetSentence();
        }

        private void OnCompleted()
        {
            var completed = this.completed;
            if (completed != null)
                completed(this, EventArgs.Empty);
        }

    }
}
