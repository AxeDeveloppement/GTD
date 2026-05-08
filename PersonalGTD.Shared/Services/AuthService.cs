using Microsoft.JSInterop;
using Supabase;
using Supabase.Gotrue;
using System.Text.Json;
using System;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

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
                    // Persister la session Supabase via localStorage
                    var sessionJson = JsonSerializer.Serialize(session);
                    try { await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "supabase_session", sessionJson); } catch { }
                    
                    var user = session.User;
                    if (user != null)
                    {
                        var username = MapEmailToUsername(user.Email);
                        if (username != null)
                        {
                            CurrentUser = username;
                            try { await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "gtd_user", username); } catch { }
                        }
                    }
                }
                NotifyAuthenticationStateChanged();
            }
            else if (state == Supabase.Gotrue.Constants.AuthState.SignedOut)
            {
                // Uniquement si on est vraiment déconnecté
                if (_supabase.Auth.CurrentSession == null)
                {
                    CurrentUser = null;
                    try { await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "supabase_session"); } catch { }
                    try { await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "gtd_user"); } catch { }
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
            // 1. Initialiser le client Supabase
            await _supabase.InitializeAsync();

            // 2. Tenter de récupérer un token capturé par l'index.html (Cas GitHub Pages / Redirect)
            var capturedHash = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "supabase_auth_token");
            if (!string.IsNullOrEmpty(capturedHash))
            {
                await ProcessHashFragment(capturedHash);
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "supabase_auth_token");
            }

            // 3. Charger la session persistée classique via localStorage
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

            // 4. Vérification finale
            await Task.Delay(300); 
            var currentSession = _supabase.Auth.CurrentSession;
            var user = currentSession?.User ?? _supabase.Auth.CurrentUser;

            if (user != null)
            {
                var username = MapEmailToUsername(user.Email) ?? user.Email;
                if (!string.IsNullOrEmpty(username))
                {
                    CurrentUser = username;
                    try { await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "gtd_user", username); } catch { }
                    
                    if (currentSession != null)
                    {
                        var sessionJson = JsonSerializer.Serialize(currentSession);
                        try { await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "supabase_session", sessionJson); } catch { }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Auth initialization error: {ex.Message}");
        }
        finally
        {
            IsInitialized = true;
            NotifyAuthenticationStateChanged();
        }
    }

    private async Task ProcessHashFragment(string hash)
    {
        try
        {
            // Nettoyer le #
            var cleanHash = hash.TrimStart('#');
            var parts = cleanHash.Split('&');
            string? accessToken = null;
            string? refreshToken = null;

            foreach (var part in parts)
            {
                var kvp = part.Split('=');
                if (kvp.Length != 2) continue;
                
                if (kvp[0] == "access_token") accessToken = kvp[1];
                if (kvp[0] == "refresh_token") refreshToken = kvp[1];
            }

            if (!string.IsNullOrEmpty(accessToken))
            {
                await _supabase.Auth.SetSession(accessToken, refreshToken ?? "");
            }
        }
        catch { }
    }

    public async Task<string?> GetGoogleSignInUrl(string callbackUrl)
    {
        try
        {
            // Détection Android pour utiliser le scheme personnalisé
            var isAndroid = RuntimeInformation.IsOSPlatform(OSPlatform.Create("ANDROID")) || 
                           RuntimeInformation.OSDescription.Contains("android", StringComparison.OrdinalIgnoreCase);
            
            if (isAndroid)
            {
                callbackUrl = "gtdapp://auth";
            }

            var options = new SignInOptions
            {
                RedirectTo = callbackUrl
            };
            var result = await _supabase.Auth.SignIn(Supabase.Gotrue.Constants.Provider.Google, options);
            return result?.Uri?.ToString();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting Google sign in URL: {ex.Message}");
            return null;
        }
    }

    public async Task ProcessDeepLink(string url)
    {
        if (string.IsNullOrEmpty(url)) return;

        try
        {
            // S'assurer que Supabase est initialisé
            if (!IsInitialized)
            {
                await InitializeAsync();
            }

            var uri = new Uri(url);
            var fragment = uri.Fragment;
            
            if (string.IsNullOrEmpty(fragment) && url.Contains("#"))
            {
                fragment = url.Substring(url.IndexOf('#'));
            }

            if (!string.IsNullOrEmpty(fragment))
            {
                await ProcessHashFragment(fragment);
                NotifyAuthenticationStateChanged();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing deep link: {ex.Message}");
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
