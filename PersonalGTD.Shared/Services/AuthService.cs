using Supabase;
using Supabase.Gotrue;
using System.Text.Json;
using System.Text.Json.Serialization;

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
            var session = _supabase.Auth.CurrentSession;
            if (session != null)
            {
                try 
                {
                    // Persister la session Supabase
                    var sessionJson = JsonSerializer.Serialize(session);
                    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "supabase_session", sessionJson);
                    
                    var user = session.User;
                    if (user != null)
                    {
                        var username = MapEmailToUsername(user.Email);
                        if (username != null && CurrentUser != username)
                        {
                            CurrentUser = username;
                            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "gtd_user", username);
                        }
                    }
                } catch { }
            }
            OnAuthStateChanged?.Invoke();
        }
        else if (state == AuthState.SignedOut)
        {
            CurrentUser = null;
            try 
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "supabase_session");
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
            // 1. Tenter de charger une session existante du localStorage
            var savedSessionJson = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "supabase_session");
            if (!string.IsNullOrEmpty(savedSessionJson))
            {
                try 
                {
                    var session = JsonSerializer.Deserialize<Session>(savedSessionJson);
                    if (session != null && !string.IsNullOrEmpty(session.AccessToken))
                    {
                        // Charger la session dans le client Supabase
                        await _supabase.Auth.SetSession(session.AccessToken, session.RefreshToken ?? "");
                    }
                } catch { }
            }

            // 2. Laisser le temps au client de parser le fragment d'URL (OAuth redirect)
            // Augmenté à 500ms pour plus de fiabilité sur GitHub Pages
            await Task.Delay(500); 

            var currentSession = _supabase.Auth.CurrentSession;
            if (currentSession?.User != null)
            {
                var username = MapEmailToUsername(currentSession.User.Email);
                if (username != null)
                {
                    CurrentUser = username;
                    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "gtd_user", username);
                }
            }
            else 
            {
                // Fallback username-only persistence
                var user = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "gtd_user");
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
