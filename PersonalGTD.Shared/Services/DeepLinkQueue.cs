using System.Collections.Concurrent;

namespace PersonalGTD.Shared.Services;

/// <summary>
/// File d'attente thread-safe de deep links OAuth reçus depuis la couche native (Android).
///
/// Contexte : <c>AuthService</c> est enregistré en scope <b>Scoped</b> et dépend d'<c>IJSRuntime</c>,
/// qui n'est résolvable que dans le scope du circuit Blazor. Il est donc impossible (et dangereux)
/// de le résoudre depuis le provider racine (<c>IPlatformApplication.Current.Services</c>) dans
/// <c>MainActivity.HandleIntent</c>.
///
/// Solution : la couche native enfile simplement l'URL dans cette file statique, et la couche
/// Blazor (<c>AuthListener</c>) la consomme avec l'instance <c>AuthService</c> du circuit.
/// </summary>
public static class DeepLinkQueue
{
    private static readonly ConcurrentQueue<string> _queue = new();

    /// <summary>
    /// Déclenché (sur le thread appelant, ici le thread UI Android) à chaque enfilement.
    /// La couche Blazor s'y abonne pour traiter immédiatement les liens arrivés après le démarrage du circuit.
    /// </summary>
    public static event Action? LinkEnqueued;

    /// <summary>
    /// Enfile un deep link OAuth (appelé depuis la couche native, thread-safe).
    /// </summary>
    public static void Enqueue(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        _queue.Enqueue(url);
        LinkEnqueued?.Invoke();
    }

    /// <summary>
    /// Vide la file et retourne tous les deep links en attente (appelé depuis le circuit Blazor).
    /// </summary>
    public static IReadOnlyList<string> Drain()
    {
        var links = new List<string>();
        while (_queue.TryDequeue(out var link))
        {
            links.Add(link);
        }
        return links;
    }
}
