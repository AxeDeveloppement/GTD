using Microsoft.JSInterop;

namespace PersonalGTD.Shared.Services;

/// <summary>
/// Implémentation Web de ISessionStorage via localStorage (JSInterop).
/// </summary>
public class WebSessionStorage : ISessionStorage
{
    private readonly IJSRuntime _js;

    public WebSessionStorage(IJSRuntime js)
    {
        _js = js;
    }

    public async Task<string?> GetItemAsync(string key)
    {
        return await _js.InvokeAsync<string?>("localStorage.getItem", key);
    }

    public async Task SetItemAsync(string key, string value)
    {
        await _js.InvokeVoidAsync("localStorage.setItem", key, value);
    }

    public async Task RemoveItemAsync(string key)
    {
        await _js.InvokeVoidAsync("localStorage.removeItem", key);
    }
}
