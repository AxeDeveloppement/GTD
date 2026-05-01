using PersonalGTD.Shared.Models;
using Supabase;

namespace PersonalGTD.Shared.Services;

public class TaskService : ITaskService
{
    private readonly Client _supabase;

    public TaskService(Client supabase)
    {
        _supabase = supabase;
    }

    public async Task<IEnumerable<GtdItem>> GetItemsAsync()
    {
        var response = await _supabase.From<GtdItem>().Get();
        return response.Models;
    }

    public async Task<GtdItem?> GetItemAsync(Guid id)
    {
        var response = await _supabase.From<GtdItem>().Where(x => x.Id == id).Get();
        return response.Models.FirstOrDefault();
    }

    public async Task AddItemAsync(GtdItem item)
    {
        await _supabase.From<GtdItem>().Insert(item);
    }

    public async Task UpdateItemAsync(GtdItem item)
    {
        await _supabase.From<GtdItem>().Update(item);
    }

    public async Task DeleteItemAsync(Guid id)
    {
        await _supabase.From<GtdItem>().Where(x => x.Id == id).Delete();
    }
}
