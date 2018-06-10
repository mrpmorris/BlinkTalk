using UnityEngine;

namespace BlinkTalk.Typing
{
    public interface IInputStrategy
    {
        void Initialize(ITypingController controller);
        void Terminate();
        void ReceiveIndication();
    }
}
