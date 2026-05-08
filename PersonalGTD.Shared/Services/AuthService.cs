using Microsoft.JSInterop;
using Supabase;
using Supabase.Gotrue;
using System.Text.Json;
using System;
using System.Threading.Tasks;

namespace PersonalGTD.Shared.Services;

public class AuthService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly Supabase.Client _supabase;
    public string? CurrentUser { get; private set; }
    public bool IsInitialized { get; private set; }

    public event Action? OnAuthStateChanged;

    public void NotifyAuthenticationStateChanged()
    {
        OnAuthStateChanged?.Invoke();
    }

    public AuthService(IJSRuntime jsRuntime, Supabase.Client supabase)
    {
        _jsRuntime = jsRuntime;
        _supabase = supabase;
        
        // Listen to auth state changes (OAuth, session recovery, etc.)
        _supabase.Auth.AddStateChangedListener(OnSupabaseAuthStateChanged);
    }

    private async void OnSupabaseAuthStateChanged(object sender, Supabase.Gotrue.Constants.AuthState state)
    {
        try
        {
            if (state == Supabase.Gotrue.Constants.AuthState.SignedIn || state == Supabase.Gotrue.Constants.AuthState.TokenRefreshed)
            {
                var session = _supabase.Auth.CurrentSession;
                if (session != null)
                {
                    // Persister la session Supabase
                    var sessionJson = JsonSerializer.Serialize(session);
                    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "supabase_session", sessionJson);
                    
                    var user = session.User;
                    if (user != null)
                    {
                        var username = MapEmailToUsername(user.Email);
                        if (username != null)
                        {
                            CurrentUser = username;
                            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "gtd_user", username);
                        }
                    }
                }
                NotifyAuthenticationStateChanged();
            }
            else if (state == Supabase.Gotrue.Constants.AuthState.SignedOut)
            {
                // Uniquement si on est vraiment déconnecté (pas lors d'un rafraîchissement raté temporaire)
                if (_supabase.Auth.CurrentSession == null)
                {
                    CurrentUser = null;
                    await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "supabase_session");
                    await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "gtd_user");
                    NotifyAuthenticationStateChanged();
                }
            }
        }
        catch (Exception)
        {
            // Ignorer les erreurs de JS interop lors de la fermeture ou transition
        }
    }

    public async Task InitializeAsync()
    {
        if (IsInitialized) return;
        
        try
        {
            // 1. Tenter de charger une session existante du localStorage IMMÉDIATEMENT
            var savedUser = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "gtd_user");
            if (!string.IsNullOrEmpty(savedUser))
            {
                CurrentUser = savedUser;
                NotifyAuthenticationStateChanged();
            }

            var savedSessionJson = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "supabase_session");
            if (!string.IsNullOrEmpty(savedSessionJson))
            {
                try 
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var session = JsonSerializer.Deserialize<Session>(savedSessionJson, options);
                    if (session != null && !string.IsNullOrEmpty(session.AccessToken))
                    {
                        await _supabase.Auth.SetSession(session.AccessToken, session.RefreshToken ?? "");
                    }
                } catch { }
            }

            // 2. Laisser le temps au client de parser le fragment d'URL (OAuth redirect)
            await Task.Delay(500); 

            // 3. Vérifier la session actuelle
            var currentSession = _supabase.Auth.CurrentSession;
            var user = currentSession?.User ?? _supabase.Auth.CurrentUser;

            if (user != null)
            {
                var username = MapEmailToUsername(user.Email);
                if (username != null)
                {
                    CurrentUser = username;
                    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "gtd_user", username);
                    
                    if (currentSession != null)
                    {
                        var sessionJson = JsonSerializer.Serialize(currentSession);
                        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "supabase_session", sessionJson);
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
            NotifyAuthenticationStateChanged();
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
                    NotifyAuthenticationStateChanged();
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
            NotifyAuthenticationStateChanged();
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
        NotifyAuthenticationStateChanged();
    }
}
