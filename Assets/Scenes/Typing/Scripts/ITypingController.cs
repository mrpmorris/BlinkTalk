using UnityEngine;

namespace BlinkTalk.Typing
{
    public interface ITypingController
    {
        void StartInputStrategy<TStrategy>()
            where TStrategy : MonoBehaviour, IInputStrategy;
        void InputStrategyFinished();
    }
}
