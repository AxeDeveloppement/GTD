// MobileSessionStorage déplacé vers le projet Android pour éviter la dépendance MAUI dans Shared.
// Ce fichier restaure une implémentation identique dans PersonalGTD.Shared si vous préférez l'avoir
// directement dans le projet Shared (attention aux références MAUI en build non-mobile).

using System.Threading.Tasks;

namespace PersonalGTD.Shared.Services;

public class MobileSessionStorage : ISessionStorage
{
    public Task<string?> GetItemAsync(string key)
    {
        // Implémentation neutre pour Shared : renvoie null (plateforme doit fournir une vraie implémentation)
        return Task.FromResult<string?>(null);
    }

    public Task SetItemAsync(string key, string value)
    {
        // Pas d'effet dans cette implémentation Shared
        return Task.CompletedTask;
    }

    public Task RemoveItemAsync(string key)
    {
        return Task.CompletedTask;
    }
}
