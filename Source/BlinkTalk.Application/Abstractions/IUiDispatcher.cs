using System;
using System.Threading.Tasks;

namespace BlinkTalk.Application.Abstractions
{
    /// <summary>
    /// Marshals an action onto the UI thread. The scanning timer runs on the thread pool;
    /// every callback that touches shared state (the strategy stack, the sentence, the
    /// highlight) is funnelled through here so all mutation happens single-threaded on the
    /// UI thread — the same guarantee Unity gave for free by running everything on the main thread.
    /// </summary>
    public interface IUiDispatcher
    {
        Task InvokeAsync(Action action);
    }

    /// <summary>An inline dispatcher that runs actions synchronously. Used by unit tests.</summary>
    public sealed class InlineUiDispatcher : IUiDispatcher
    {
        public Task InvokeAsync(Action action)
        {
            action();
            return Task.CompletedTask;
        }
    }
}
