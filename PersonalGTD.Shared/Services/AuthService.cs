using Microsoft.JSInterop;

namespace PersonalGTD.Shared.Services;

public class AuthService
{
    private readonly IJSRuntime _jsRuntime;
    public string? CurrentUser { get; private set; }
    public bool IsInitialized { get; private set; }

    public event Action? OnAuthStateChanged;

    public AuthService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task InitializeAsync()
    {
        if (IsInitialized) return;
        
        try
        {
            // We use a timeout to prevent hanging on Android WebView startup
            var userTask = _jsRuntime.InvokeAsync<string>("localStorage.getItem", "gtd_user").AsTask();
            if (await Task.WhenAny(userTask, Task.Delay(2000)) == userTask)
            {
                var user = await userTask;
                if (!string.IsNullOrEmpty(user))
                {
                    CurrentUser = user;
                }
            }
        }
        catch
        {
            // Fail silently
        }
        finally
        {
            IsInitialized = true;
            OnAuthStateChanged?.Invoke();
        }
    }

    public async Task<bool> LoginAsync(string username, string password)
    {
        if ((username == "Dounette" && password == "Axel") ||
            (username == "Axel" && password == "Dounette") ||
            (username == "demo" && password == "demo"))
        {
            CurrentUser = username;
            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "gtd_user", username);
            }
            catch { }
            OnAuthStateChanged?.Invoke();
            return true;
        }
        return false;
    }

    public async Task LogoutAsync()
    {
        CurrentUser = null;
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "gtd_user");
        }
        catch { }
        OnAuthStateChanged?.Invoke();
    }
}
