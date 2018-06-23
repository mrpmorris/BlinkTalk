using UnityEngine;
using UnityEngine.UI;

namespace BlinkTalk.Typing
{
    public class KeyboardRowSelectorInput : MonoBehaviour, IInputStrategy
    {
        private ITypingController Controller;
        private VerticalLayoutGroup ClientArea;

        void IInputStrategy.Initialize(ITypingController controller)
        {
            Controller = controller;
            gameObject.GetComponentInChildren<VerticalLayoutGroup>();
            this.EnsureAssigned(x => x.ClientArea);
        }

        void IInputStrategy.ReceiveIndication()
        {
        }

        void IInputStrategy.Terminate()
        {
        }
    }
}
