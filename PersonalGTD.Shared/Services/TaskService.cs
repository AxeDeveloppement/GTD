using PersonalGTD.Shared.Models;

namespace PersonalGTD.Shared.Services;

public class TaskService : ITaskService
{
    private readonly List<GtdItem> _items = new();

    public Task<IEnumerable<GtdItem>> GetItemsAsync()
    {
        return Task.FromResult<IEnumerable<GtdItem>>(_items);
    }

    public Task<GtdItem?> GetItemAsync(Guid id)
    {
        var item = _items.FirstOrDefault(i => i.Id == id);
        return Task.FromResult(item);
    }

    public Task AddItemAsync(GtdItem item)
    {
        _items.Add(item);
        return Task.CompletedTask;
    }

    public Task UpdateItemAsync(GtdItem item)
    {
        var existing = _items.FirstOrDefault(i => i.Id == item.Id);
        if (existing != null)
        {
            existing.Title = item.Title;
            existing.Note = item.Note;
            existing.Status = item.Status;
            existing.ContextId = item.ContextId;
            existing.ProjectId = item.ProjectId;
            existing.EnergyLevel = item.EnergyLevel;
            existing.EstimatedTime = item.EstimatedTime;
        }
        return Task.CompletedTask;
    }

    public Task DeleteItemAsync(Guid id)
    {
        var existing = _items.FirstOrDefault(i => i.Id == id);
        if (existing != null)
        {
            _items.Remove(existing);
        }
        return Task.CompletedTask;
    }
}
