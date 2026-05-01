using PersonalGTD.Shared.Models;

namespace PersonalGTD.Shared.Services;

public interface ITaskService
{
    Task<IEnumerable<GtdItem>> GetItemsAsync();
    Task<GtdItem?> GetItemAsync(Guid id);
    Task AddItemAsync(GtdItem item);
    Task UpdateItemAsync(GtdItem item);
    Task DeleteItemAsync(Guid id);
}
