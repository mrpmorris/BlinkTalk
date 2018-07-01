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
        private FocusCycler FocusCycler;
        private ScrollRect KeyboardSelector;
        private RectTransform ClientArea;
        private float TargetScrollPosition;
        private int FocusChangeCount;

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
            if (FocusCycler == null)
                FocusCycler = new FocusCycler(this, Rows.Length, FocusIndexChanged);
            FocusChangeCount = 0;
            FocusCycler.Start();
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
            FocusChangeCount++;
            if (FocusChangeCount > Rows.Length + 1)
                Controller.InputStrategyFinished();
        }

        void Update()
        {
            float x = ClientArea.anchoredPosition.x;
            float y = Mathf.Lerp(ClientArea.anchoredPosition.y, TargetScrollPosition, Consts.FocusLerpFactor());
            ClientArea.anchoredPosition = new Vector2(x, y);
        }
    }
}
