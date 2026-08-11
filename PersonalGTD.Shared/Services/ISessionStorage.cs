namespace PersonalGTD.Shared.Services;

/// <summary>
/// Abstraction de stockage session/caché qui fonctionne à la fois sur Web (localStorage via JSInterop)
/// et sur Mobile (Preferences natif MAUI).
/// </summary>
public interface ISessionStorage
{
    Task<string?> GetItemAsync(string key);
    Task SetItemAsync(string key, string value);
    Task RemoveItemAsync(string key);
}
