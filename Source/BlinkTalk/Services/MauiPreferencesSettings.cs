using BlinkTalk.Application.Abstractions;
using Microsoft.Maui.Storage;

namespace BlinkTalk.Services;

/// <summary>Persists settings via MAUI Preferences (per-platform key/value store).</summary>
public sealed class MauiPreferencesSettings : ISettingsStore
{
    public bool GetBool(string key, bool defaultValue) => Preferences.Default.Get(key, defaultValue);
    public double GetDouble(string key, double defaultValue) => Preferences.Default.Get(key, defaultValue);
    public string GetString(string key, string defaultValue) => Preferences.Default.Get(key, defaultValue);
    public void SetBool(string key, bool value) => Preferences.Default.Set(key, value);
    public void SetDouble(string key, double value) => Preferences.Default.Set(key, value);
    public void SetString(string key, string value) => Preferences.Default.Set(key, value);
}
