using System;
using System.Threading.Tasks;
using BlinkTalk.Application.Abstractions;
using Microsoft.Maui.ApplicationModel;

namespace BlinkTalk.Services;

/// <summary>Marshals scanner callbacks onto the UI thread (the BlazorWebView runs on it).</summary>
public sealed class MauiUiDispatcher : IUiDispatcher
{
    public Task InvokeAsync(Action action) => MainThread.InvokeOnMainThreadAsync(action);
}
