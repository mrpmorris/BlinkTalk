using UnityEngine;

namespace BlinkTalk.Typing
{
    public class SectionSelectorInputStrategy: MonoBehaviour, IInputStrategy
    {
        private ITypingController Controller;

        void IInputStrategy.Initialize(ITypingController controller)
        {
            Controller = controller;
        }

        void IInputStrategy.ReceiveIndication()
        {
            Debug.Log("Indicate section");
        }

        void IInputStrategy.Terminate()
        {
        }
    }
}
