using BlinkTalk.Application.Abstractions;

namespace BlinkTalk.Services;

/// <summary>Marshals scanner callbacks onto the UI thread (the BlazorWebView runs on it).</summary>
public sealed class MauiUIDispatcher : IUIDispatcher
{
    public Task InvokeAsync(Action action) => MainThread.InvokeOnMainThreadAsync(action);
}
