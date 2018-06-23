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
        [Space]
        public ScrollRect KeyboardSelector;
        public Text InputText;
        [Space]
        public Button IndicateButton;
        public Image Highlighter;

        private Stack<IInputStrategy> InputStrategies = new Stack<IInputStrategy>();
        private RectTransform TargetRectTransform;
        RectTransform ITypingController.GetInputSelectionPanel() => InputSelectionPanel;
        RectTransform ITypingController.GetWordSelectionPanel() => WordSelectionPanel;
        RectTransform ITypingController.GetKeyboardSelectionPanel() => KeyboardSelectionPanel;
        ScrollRect ITypingController.GetKeyboardScrollRect() => KeyboardSelector;

        private void Start()
        {
            this.EnsureAssigned(x => x.Highlighter);
            this.EnsureAssigned(x => x.InputSelectionPanel);
            this.EnsureAssigned(x => x.WordSelectionPanel);
            this.EnsureAssigned(x => x.KeyboardSelectionPanel);
            this.EnsureAssigned(x => x.KeyboardSelector);
            this.EnsureAssigned(x => x.InputText);
            this.EnsureAssigned(x => x.IndicateButton).onClick.AddListener(OnIndicateButtonClick);
            this.EnsureAssigned(x => x.Highlighter);

            StartInputStrategy<SectionSelectorInputStrategy>();
            StartCoroutine(PulseHighlighter());
        }

        private void OnIndicateButtonClick()
        {
            InputStrategies.Peek().ReceiveIndication();
        }

        public void SetIndicatorRect(RectTransform target)
        {
            TargetRectTransform = target;
        }
        
        public void StartInputStrategy<TStrategy>()
            where TStrategy : MonoBehaviour, IInputStrategy
        {
            if (InputStrategies.Count > 0)
                InputStrategies.Peek().ChildStrategyActivated();
            TStrategy inputStrategy = gameObject.AddComponent<TStrategy>();
            inputStrategy.Initialize(this);
            InputStrategies.Push(inputStrategy);
        }

        public void InputStrategyFinished()
        {
            IInputStrategy inputStrategyToTerminate = InputStrategies.Pop();
            inputStrategyToTerminate.Terminate();
            Destroy((MonoBehaviour)inputStrategyToTerminate);
            if (InputStrategies.Count > 0)
                InputStrategies.Peek().Initialize(this);
        }

        private IEnumerator PulseHighlighter()
        {
            while (true)
            {
                Color startColor;
                Color endColor;
                float factor;
                float doubleTime = Time.time % 1f * 2f;
                if (doubleTime < 1)
                {
                    startColor = Color.blue;
                    endColor = Color.white;
                    factor = doubleTime;
                }
                else
                {
                    startColor = Color.white;
                    endColor = Color.blue;
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

    }
}