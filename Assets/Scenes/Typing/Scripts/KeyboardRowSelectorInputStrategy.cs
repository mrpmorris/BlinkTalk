using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BlinkTalk.Typing
{
    public class KeyboardRowSelectorInput : MonoBehaviour, IInputStrategy
    {
        private ITypingController Controller;
        private RectTransform FocusedRow;
        private RectTransform[] Rows;
        private float[] RowPositions;
        private readonly FocusCycler FocusCycler;
        private ScrollRect KeyboardSelector;
        private RectTransform ClientArea;
        private float TargetScrollPosition;

        public KeyboardRowSelectorInput()
        {
            FocusCycler = new FocusCycler(this, FocusIndexChanged, firstCycleDelayMultiplier: 1.5f);
        }

        void IInputStrategy.Initialize(ITypingController controller)
        {
            Controller = controller;
            KeyboardSelector = controller.GetKeyboardSelector();
            ClientArea = Controller
                .GetKeyboardSelectorClientArea()
                .GetComponent<RectTransform>();
            this.EnsureAssigned(x => x.ClientArea);
            Rows = Controller
                .GetKeyboardSelectorClientArea()
                .GetComponentsInChildren<HorizontalLayoutGroup>()
                .Select(x => x.GetComponent<RectTransform>())
                .ToArray();
            RowPositions = RowPositions ?? Rows.Select(x => x.localPosition.y).ToArray();

            FocusIndexChanged(Rows.Length / 2); // Start at index 1, so we can see index 0 scroll into view
            LerpClientAreaPosition(1);

            FocusCycler.Start(Rows.Length);
            Controller.SetIndicatorRect(Controller.GetKeyboardSelectionPanel());
        }

        void IInputStrategy.ReceiveIndication()
        {
            FocusCycler.Stop();
            IKeyboardColumnSelectorInputStrategy columnSelectorInputStrategy =
                Controller.StartInputStrategy<KeyboardColumnSelectorInputStrategy>();
            columnSelectorInputStrategy.SetActiveRow(FocusedRow);
        }

        void IInputStrategy.ChildStrategyActivated(IInputStrategy inputStrategy)
        {
            FocusCycler.Stop();
        }

        void IInputStrategy.Terminated()
        {
            FocusCycler.Stop();
        }

        private void FocusIndexChanged(int focusIndex)
        {
            FocusedRow = Rows[focusIndex];
            Canvas.ForceUpdateCanvases();
            Vector2 targetVector = 
                (Vector2)KeyboardSelector.transform.InverseTransformPoint(ClientArea.position)
                - (Vector2)KeyboardSelector.transform.InverseTransformPoint(FocusedRow.position);
            TargetScrollPosition = targetVector.y - FocusedRow.sizeDelta.y / 2;
            if (FocusCycler.FocusChangeCount > Rows.Length + 1)
                Controller.InputStrategyFinished();
        }

        void Update()
        {
            LerpClientAreaPosition(Consts.FocusLerpFactor());
        }

        void LerpClientAreaPosition(float factor)
        {
            float x = ClientArea.anchoredPosition.x;
            float y = Mathf.Lerp(ClientArea.anchoredPosition.y, TargetScrollPosition, factor);
            ClientArea.anchoredPosition = new Vector2(x, y);
        }
    }
}
