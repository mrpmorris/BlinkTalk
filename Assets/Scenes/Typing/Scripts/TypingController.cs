using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BlinkTalk.Typing
{
    public class TypingController : MonoBehaviour, ITypingController
    {
        [Header("Sections")]
        public RectTransform InputSelectionPanel;
        public RectTransform WordSelectionPanel;
        public RectTransform KeyboardSelectionPanel;
        [Header("Keyboard")]
        public ScrollRect KeyboardSelector;
        public RectTransform KeyboardSelectorClientArea;
        [Space]
        public Button IndicateButton;
        public Image Highlighter;
        public Text InputText;

        private SentenceBuilder SentenceBuilder = new SentenceBuilder();
        private Stack<IInputStrategy> InputStrategies = new Stack<IInputStrategy>();
        private RectTransform TargetRectTransform;
        private Color[] IndicatorColors = new Color[] { Color.blue, Color.green, Color.magenta, Color.yellow };
        RectTransform ITypingController.GetInputSelectionPanel() => InputSelectionPanel;
        RectTransform ITypingController.GetWordSelectionPanel() => WordSelectionPanel;
        RectTransform ITypingController.GetKeyboardSelectionPanel() => KeyboardSelectionPanel;
        ScrollRect ITypingController.GetKeyboardSelector() => KeyboardSelector;
        RectTransform ITypingController.GetKeyboardSelectorClientArea() => KeyboardSelectorClientArea;
        public SentenceBuilder GetSentenceBuilder() => SentenceBuilder;

        public TypingController()
        {
            SentenceBuilder.ViewModelChanged += (s, e) => UpdateDisplayText();
        }

        private void Start()
        {
            this.EnsureAssigned(x => x.Highlighter);
            this.EnsureAssigned(x => x.InputSelectionPanel);
            this.EnsureAssigned(x => x.WordSelectionPanel);
            this.EnsureAssigned(x => x.KeyboardSelectionPanel);
            this.EnsureAssigned(x => x.KeyboardSelector);
            this.EnsureAssigned(x => x.KeyboardSelectorClientArea);
            this.EnsureAssigned(x => x.InputText);
            this.EnsureAssigned(x => x.IndicateButton).onClick.AddListener(OnIndicateButtonClick);
            this.EnsureAssigned(x => x.Highlighter);

            SentenceBuilder.Initialize();
            StartInputStrategy<SectionSelectorInputStrategy>();
            StartCoroutine(PulseHighlighter());
            UpdateDisplayText();
            TextToSpeech.Speak("Blink talk");

            Persistence.PersistenceService.Initialize("English");
        }

        private void OnIndicateButtonClick()
        {
            InputStrategies.Peek().ReceiveIndication();
        }

        public void SetIndicatorRect(RectTransform target)
        {
            TargetRectTransform = target;
        }
        
        public TStrategy StartInputStrategy<TStrategy>()
            where TStrategy : MonoBehaviour, IInputStrategy
        {
            ResetHighlighterPosition();

            TStrategy inputStrategy = gameObject.AddComponent<TStrategy>();
            if (InputStrategies.Count > 0)
                InputStrategies.Peek().ChildStrategyActivated(inputStrategy);
            inputStrategy.Initialize(this);
            InputStrategies.Push(inputStrategy);
            return inputStrategy;
        }

        public void InputStrategyFinished()
        {
            IInputStrategy inputStrategyToTerminate = InputStrategies.Pop();
            inputStrategyToTerminate.Terminated();
            Destroy((MonoBehaviour)inputStrategyToTerminate);
            if (InputStrategies.Count > 0)
                InputStrategies.Peek().Initialize(this);
            ResetHighlighterPosition();
        }

        private void ResetHighlighterPosition()
        {
            Highlighter.rectTransform.localPosition = new Vector2(-480, -320);
            Highlighter.rectTransform.sizeDelta = new Vector2(960, 640);
        }

        private IEnumerator PulseHighlighter()
        {
            while (true)
            {
                Color colorFromInputDepth = IndicatorColors[InputStrategies.Count - 1];
                Color startColor;
                Color endColor;
                float factor;
                float doubleTime = Time.time % 1f * 2f;
                if (doubleTime < 1)
                {
                    startColor = colorFromInputDepth;
                    endColor = Color.white;
                    factor = doubleTime;
                }
                else
                {
                    startColor = Color.white;
                    endColor = colorFromInputDepth;
                    factor = doubleTime - 1;
                }

                Highlighter.color = Color.Lerp(startColor, endColor, factor);
                yield return new WaitForEndOfFrame();
            }
        }

        void Update()
        {
            if (TargetRectTransform != null)
            {
                RectTransform highlighterRect = Highlighter.rectTransform;
                var position = Vector2.Lerp(highlighterRect.position, TargetRectTransform.position, Consts.FocusLerpFactor());
                Highlighter.rectTransform.position = position;
                var size = Vector2.Lerp(highlighterRect.rect.size, TargetRectTransform.rect.size, Consts.FocusLerpFactor());
                highlighterRect.sizeDelta = size;
            }
        }

        public void ReceiveKeyPress(KeyCode keyCode)
        {
            SentenceBuilder.Input(keyCode);
            UpdateDisplayText();
        }

        private void UpdateDisplayText()
        {
            InputText.text = SentenceBuilder.ToString();
            InputText.color = SentenceBuilder.ShouldClearOnNextInput ? Color.gray : Color.white;
        }
    }
}