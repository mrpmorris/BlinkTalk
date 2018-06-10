using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BlinkTalk.Typing
{
    public class TypingController : MonoBehaviour, ITypingController
    {
        public Image Highlighter;
        [Header("Sections")]
        public RectTransform InputSelectionPanel;
        public RectTransform WordSelectionPanel;
        public RectTransform KeyboardSelectionPanel;
        [Space]
        public ScrollRect KeyboardSelector;
        public Text InputText;
        public RectTransform KeyHighlighter;
        [Space]
        public Button IndicateButton;

        private Stack<IInputStrategy> InputStrategies = new Stack<IInputStrategy>();
        private RectTransform KeyboardSelectorClientArea;
        private RectTransform[] KeyboardSelectionGroups;

        private void Start()
        {
            this.EnsureAssigned(x => x.Highlighter);
            this.EnsureAssigned(x => x.InputSelectionPanel);
            this.EnsureAssigned(x => x.WordSelectionPanel);
            this.EnsureAssigned(x => x.KeyboardSelectionPanel);
            this.EnsureAssigned(x => x.KeyboardSelector);
            this.EnsureAssigned(x => x.InputText);
            this.EnsureAssigned(x => x.IndicateButton).onClick.AddListener(OnIndicateButtonClick);
            this.EnsureAssigned(x => x.KeyHighlighter);

            KeyboardSelectorClientArea = this.EnsureAssigned(x => x.KeyboardSelector.content);
            KeyboardSelectionGroups = KeyboardSelectorClientArea.GetChildRectTransforms();

            StartInputStrategy<SectionSelectorInputStrategy>();
            StartCoroutine(PulseHighlighter());
        }

        private void OnIndicateButtonClick()
        {
            InputStrategies.Peek().ReceiveIndication();
        }

        public void StartInputStrategy<TStrategy>()
            where TStrategy : MonoBehaviour, IInputStrategy
        {
            TStrategy inputStrategy = gameObject.AddComponent<TStrategy>();
            inputStrategy.Initialize(this);
            InputStrategies.Push(inputStrategy);
        }

        public void InputStrategyFinished()
        {
            IInputStrategy inputStrategyToTerminate = InputStrategies.Pop();
            inputStrategyToTerminate.Terminate();
            Destroy((MonoBehaviour)inputStrategyToTerminate);
        }

        private IEnumerator PulseHighlighter()
        {
            while (true)
            {
                Color startColor;
                Color endColor;
                float factor;
                float doubleTime = Time.time * 2;
                if (doubleTime % 2 < 1)
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

    }
}