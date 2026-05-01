using PersonalGTD.Shared.Models;

namespace PersonalGTD.Shared.Services;

public interface IContextService
{
    Task<List<GtdContext>> GetContextsAsync();
    Task<GtdContext?> GetContextAsync(Guid id);
    Task AddContextAsync(GtdContext context);
    Task UpdateContextAsync(GtdContext context);
    Task DeleteContextAsync(Guid id);
}
