using UnityEngine;
using UnityEngine.UI;

namespace BlinkTalk.Typing
{
    public class WordSuggestionPresenter : MonoBehaviour
    {
        private Text TextUI;

        public string Word
        {
            get { return TextUI.text; }
            set { TextUI.text = value; }
        }

        private void OnEnable()
        { 
            TextUI = gameObject.GetComponentInChildren<Text>();
            this.EnsureAssigned(x => x.TextUI);
        }
    }
}
