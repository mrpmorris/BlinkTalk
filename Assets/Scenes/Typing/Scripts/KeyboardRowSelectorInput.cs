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
        private FocusCycler FocusCycler;
        private RectTransform ClientArea;
        private float TargetScrollPosition;
        private int FocusChangeCount;

        void IInputStrategy.Initialize(ITypingController controller)
        {
            Controller = controller;
            ClientArea = Controller
                .GetKeyboardScrollRect()
                .GetComponentInChildren<VerticalLayoutGroup>()
                .GetComponent<RectTransform>();
            this.EnsureAssigned(x => x.ClientArea);
            Rows = Controller
                .GetKeyboardScrollRect()
                .GetComponentsInChildren<HorizontalLayoutGroup>()
                .Select(x => x.GetComponent<RectTransform>())
                .ToArray();
            if (FocusCycler == null)
                FocusCycler = new FocusCycler(this, Rows.Length, FocusIndexChanged);
            FocusChangeCount = 0;
            FocusCycler.Start();
        }

        void IInputStrategy.ReceiveIndication()
        {
            FocusCycler.Stop();
        }

        void IInputStrategy.ChildStrategyActivated()
        {
            FocusCycler.Stop();
        }

        void IInputStrategy.Terminate()
        {
            FocusCycler.Stop();
        }

        private void FocusIndexChanged(int focusIndex)
        {
            FocusedRow = Rows[focusIndex];
            TargetScrollPosition = Controller.GetKeyboardScrollRect().GetSnapToPositionToBringChildIntoView(FocusedRow).y;
            FocusChangeCount++;
            if (FocusChangeCount > Rows.Length + 1)
                Controller.InputStrategyFinished();
        }

        void Update()
        {
            float x = ClientArea.position.x;
            float y = Mathf.Lerp(ClientArea.position.y, TargetScrollPosition, Consts.FocusLerpFactor());
            ClientArea.position = new Vector2(x, y);
        }
    }
}
