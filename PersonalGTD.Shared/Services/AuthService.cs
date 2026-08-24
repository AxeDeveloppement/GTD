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
    private readonly ISessionStorage _sessionStorage;
    public string? CurrentUser { get; private set; }
    public bool IsInitialized { get; private set; }

    public event Action? OnAuthStateChanged;

    public void NotifyAuthenticationStateChanged()
    {
        OnAuthStateChanged?.Invoke();
    }

    public AuthService(IJSRuntime jsRuntime, Supabase.Client supabase, ISessionStorage sessionStorage)
    {
        _jsRuntime = jsRuntime;
        _supabase = supabase;
        _sessionStorage = sessionStorage;
        
        // Listen to auth state changes (OAuth, session recovery, etc.)
        _supabase.Auth.AddStateChangedListener(OnSupabaseAuthStateChanged);
    }

    private void OnSupabaseAuthStateChanged(object sender, Supabase.Gotrue.Constants.AuthState state)
    {
        // Fire-and-forget sécurisé pour éviter de bloquer le thread UI
        // (async void sur un event handler peut causer des blocages sur BlazorWebView Android)
        try
        {
            _ = HandleAuthStateChangedAsync(state);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AuthService] Auth state change handler error (ignored): {ex.Message}");
        }
    }

    private async Task HandleAuthStateChangedAsync(Supabase.Gotrue.Constants.AuthState state)
    {
        try
        {
            if (state == Supabase.Gotrue.Constants.AuthState.SignedIn || state == Supabase.Gotrue.Constants.AuthState.TokenRefreshed)
            {
                var session = _supabase.Auth.CurrentSession;
                if (session != null)
                {
                    // Persister la session Supabase via ISessionStorage
                    var sessionJson = JsonSerializer.Serialize(session);
                    try { await _sessionStorage.SetItemAsync("supabase_session", sessionJson).ConfigureAwait(false); } catch { }

                    var user = session.User;
                    if (user != null)
                    {
                        var username = MapEmailToUsername(user.Email);
                        if (username != null)
                        {
                            CurrentUser = username;
                            try { await _sessionStorage.SetItemAsync("gtd_user", username).ConfigureAwait(false); } catch { }
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
                    try { await _sessionStorage.RemoveItemAsync("supabase_session").ConfigureAwait(false); } catch { }
                    try { await _sessionStorage.RemoveItemAsync("gtd_user").ConfigureAwait(false); } catch { }
                    NotifyAuthenticationStateChanged();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AuthService] Auth state change handler error: {ex.Message}");
        }
    }

    public async Task InitializeAsync()
    {
        if (IsInitialized) return;
        
        try
        {
            // 1. Initialiser le client Supabase avec un timeout strict de 2 secondes (optimisé mobile)
            // Un timeout plus court évite que l'app reste bloquée sur "Chargement en cours..."
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            var initTask = _supabase.InitializeAsync();
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(2), cts.Token);
            
            var completedTask = await Task.WhenAny(initTask, timeoutTask);
            if (completedTask == initTask)
            {
                try { await initTask; } catch { /* Ignore init errors */ }
            }
            else
            {
                Console.WriteLine("[AuthService] Supabase initialization timed out (2s), continuing with cached session");
                // Abort the hanging task to prevent memory leaks
                cts.Cancel();
                try { initTask.Dispose(); } catch { }
            }

            // 2. Tenter de récupérer un token capturé (Cas Redirect)
            var capturedHash = await _sessionStorage.GetItemAsync("supabase_auth_token");
            if (!string.IsNullOrEmpty(capturedHash))
            {
                await ProcessHashFragment(capturedHash);
                await _sessionStorage.RemoveItemAsync("supabase_auth_token");
            }

            // 3. Charger la session persistée classique via ISessionStorage
            var savedUser = await _sessionStorage.GetItemAsync("gtd_user");
            if (!string.IsNullOrEmpty(savedUser))
            {
                CurrentUser = savedUser;
                NotifyAuthenticationStateChanged();
            }

            var savedSessionJson = await _sessionStorage.GetItemAsync("supabase_session");
            if (string.IsNullOrEmpty(savedSessionJson))
            {
                // Fallback : aucun fallback natif ici, rely on platform-specific ISessionStorage implementation
            }

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

            // 4. Vérification finale — supprimé le Task.Delay(300) qui ralentissait le démarrage Android
            var currentSession = _supabase.Auth.CurrentSession;
            var user = currentSession?.User ?? _supabase.Auth.CurrentUser;

            if (user != null)
            {
                var username = MapEmailToUsername(user.Email) ?? user.Email;
                if (!string.IsNullOrEmpty(username))
                {
                    CurrentUser = username;
                    try { await _sessionStorage.SetItemAsync("gtd_user", username); } catch { }
                    
                    if (currentSession != null)
                    {
                        var sessionJson = JsonSerializer.Serialize(currentSession);
                        try { await _sessionStorage.SetItemAsync("supabase_session", sessionJson); } catch { }
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
        catch (Exception ex)
        {
            Console.WriteLine($"[AuthService] ProcessHashFragment error: {ex.Message}");
        }
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
            var session = _supabase.Auth.CurrentSession;
            if (session?.User != null)
            {
                var email = session.User.Email;
                var username = MapEmailToUsername(email);
                
                if (username != null)
                {
                    CurrentUser = username;
                    await _sessionStorage.SetItemAsync("gtd_user", username);
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

#if DEMO_MODE
    // CRIT-03: Demo credentials conditionnés — actif uniquement en build DEMO_MODE
    private static readonly Dictionary<string, string> DemoCredentials = new()
    {
        { "Dounette", "Axel" },
        { "Axel", "Dounette" },
        { "demo", "demo" },
    };
#endif

#if DEMO_MODE
    public async Task<bool> LoginAsync(string username, string password)
    {
        if (DemoCredentials.TryGetValue(username, out var expectedPassword) && expectedPassword == password)
        {
            CurrentUser = username;
            try
            {
                await _sessionStorage.SetItemAsync("gtd_user", username);
            }
            catch { }
            NotifyAuthenticationStateChanged();
            return true;
        }
        return false;
    }
#else
    public Task<bool> LoginAsync(string username, string password)
    {
        return Task.FromResult(false);
    }
#endif

    public async Task LogoutAsync()
    {
        CurrentUser = null;
        try
        {
            await _supabase.Auth.SignOut();
            await _sessionStorage.RemoveItemAsync("gtd_user");
        }
        catch { }
        NotifyAuthenticationStateChanged();
    }
}
