using Microsoft.Maui.Storage;
using PersonalGTD.Shared.Services;

namespace PersonalGTD.Android.Services;

/// <summary>
/// Implémentation Mobile de ISessionStorage via Preferences natif MAUI.
/// Placée dans le projet Android pour éviter des références MAUI dans PersonalGTD.Shared.
/// </summary>
public class MobileSessionStorage : ISessionStorage
{
    public Task<string?> GetItemAsync(string key)
    {
        return Task.FromResult(Preferences.Default.Get(key, (string?)null));
    }

    public Task SetItemAsync(string key, string value)
    {
        Preferences.Default.Set(key, value);
        return Task.CompletedTask;
    }

    public Task RemoveItemAsync(string key)
    {
        Preferences.Default.Remove(key);
        return Task.CompletedTask;
    }
}
