namespace PersonalGTD.Shared;

/// <summary>
/// Interface d'abstraction pour la configuration Supabase.
/// Permet à chaque plateforme de fournir ses propres credentials
/// sans coupler le projet Shared à une implémentation spécifique.
/// </summary>
public interface ISupabaseSettings
{
    /// <summary>
    /// URL du projet Supabase (ex: https://xxxx.supabase.co)
    /// </summary>
    string Url { get; }

    /// <summary>
    /// Clé anon publique du projet Supabase
    /// </summary>
    string AnonKey { get; }

    /// <summary>
    /// Whether to automatically connect to Supabase Realtime
    /// </summary>
    bool AutoConnectRealtime { get; }
}
