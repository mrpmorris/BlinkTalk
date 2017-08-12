using Assets.Scripts.ViewModels;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Presenters
{
    public class MainViewModelPresenter : MonoBehaviour
    {
        public Text wordInputText;
        public Text selection1Text;
        public Text selection2Text;
        public Text selection3Text;
        public Text actionText;
        public Text sentenceText;

        public void UpdateUI(InteractionControllerViewModel viewModel)
        {
            wordInputText.text = viewModel.wordInputText;
            selection1Text.text = viewModel.selection1Text;
            selection2Text.text = viewModel.selection2Text;
            selection3Text.text = viewModel.selection3Text;
            actionText.text = viewModel.actionText;
            sentenceText.text = viewModel.sentenceText;
        }

        private void Start()
        {
            Debug.Assert(wordInputText, "wordInputText");
            Debug.Assert(selection1Text, "selection1Text");
            Debug.Assert(selection2Text, "selection2Text");
            Debug.Assert(selection3Text, "selection3Text");
            Debug.Assert(actionText, "actionText");
            Debug.Assert(sentenceText, "sentenceText");
        }

    }
}
