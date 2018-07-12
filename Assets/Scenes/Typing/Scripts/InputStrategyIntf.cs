using UnityEngine;

namespace BlinkTalk.Typing
{
    public interface IInputStrategy
    {
        void Initialize(ITypingController controller);
        void ChildStrategyActivated(IInputStrategy inputStrategy);
        void ReceiveIndication();
        void Terminated();
    }
}
