namespace BlinkTalk.Application.Input
{
    /// <summary>
    /// One level of the scanning hierarchy. Strategies are pushed/popped on a stack by the
    /// controller. Ported directly from the original IInputStrategy.
    /// </summary>
    public interface IInputStrategy
    {
        void Initialize(IScanController controller);
        void ChildStrategyActivated(IInputStrategy childStrategy);
        void ReceiveIndication();
        void Terminated();
    }
}
