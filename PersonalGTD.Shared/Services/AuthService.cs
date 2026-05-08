using Microsoft.JSInterop;
using Supabase;
using Supabase.Gotrue;

namespace PersonalGTD.Shared.Services;

public class AuthService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly Supabase.Client _supabase;
    public string? CurrentUser { get; private set; }
    public bool IsInitialized { get; private set; }

    public event Action? OnAuthStateChanged;

    public AuthService(IJSRuntime jsRuntime, Supabase.Client supabase)
    {
        _jsRuntime = jsRuntime;
        _supabase = supabase;
        
        // Listen to auth state changes (OAuth, session recovery, etc.)
        _supabase.Auth.AddStateChangedListener(OnSupabaseAuthStateChanged);
    }

    private async void OnSupabaseAuthStateChanged(object sender, AuthState state)
    {
        if (state == AuthState.SignedIn || state == AuthState.TokenRefreshed)
        {
            var user = _supabase.Auth.CurrentUser;
            if (user != null)
            {
                var username = MapEmailToUsername(user.Email);
                if (username != null && CurrentUser != username)
                {
                    CurrentUser = username;
                    try 
                    {
                        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "gtd_user", username);
                    } catch { }
                    OnAuthStateChanged?.Invoke();
                }
            }
        }
        else if (state == AuthState.SignedOut)
        {
            CurrentUser = null;
            try 
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "gtd_user");
            } catch { }
            OnAuthStateChanged?.Invoke();
        }
    }

    public async Task InitializeAsync()
    {
        if (IsInitialized) return;
        
        try
        {
            // Give Supabase client time to parse hash fragment if present
            if (IsInitialized == false) await Task.Delay(200);

            var session = _supabase.Auth.CurrentSession;
            if (session?.User != null)
            {
                var username = MapEmailToUsername(session.User.Email);
                if (username != null)
                {
                    CurrentUser = username;
                    try { await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "gtd_user", username); } catch {}
                }
            }
            else 
            {
                // Fallback to localStorage if no active Supabase session
                var userTask = _jsRuntime.InvokeAsync<string>("localStorage.getItem", "gtd_user").AsTask();
                if (await Task.WhenAny(userTask, Task.Delay(1000)) == userTask)
                {
                    var user = await userTask;
                    if (!string.IsNullOrEmpty(user))
                    {
                        CurrentUser = user;
                    }
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

    public async Task<string?> GetGoogleSignInUrl(string callbackUrl)
    {
        try
        {
            var options = new SignInOptions
            {
                RedirectTo = callbackUrl
            };
            var result = await _supabase.Auth.SignIn(Supabase.Gotrue.Constants.Provider.Google, options);
            return result?.Uri?.ToString();
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> HandleCallbackAsync()
    {
        try
        {
            // The Supabase client automatically picks up the session from the URL fragment in WASM
            // but we might need to wait for it or trigger it.
            // Actually, with the C# client in WASM, it usually handles the session from the URL.
            
            var session = _supabase.Auth.CurrentSession;
            if (session?.User != null)
            {
                var email = session.User.Email;
                var username = MapEmailToUsername(email);
                
                if (username != null)
                {
                    CurrentUser = username;
                    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "gtd_user", username);
                    OnAuthStateChanged?.Invoke();
                    return true;
                }
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    private string? MapEmailToUsername(string? email)
    {
        if (string.IsNullOrEmpty(email)) return null;

        if (email.Equals("axel.developpement@gmail.com", StringComparison.OrdinalIgnoreCase))
            return "Axel";
        
        if (email.Equals("depeyrelaurence3001@gmail.com", StringComparison.OrdinalIgnoreCase))
            return "Dounette";
        
        return email; // Default to email if no mapping
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
            await _supabase.Auth.SignOut();
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "gtd_user");
        }
        catch { }
        OnAuthStateChanged?.Invoke();
    }
}
