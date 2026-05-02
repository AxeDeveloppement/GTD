using PersonalGTD.Shared.Models;
using Supabase;

namespace PersonalGTD.Shared.Services;

public class TaskService : ITaskService
{
    private readonly Client _supabase;
    private readonly AuthService _authService;

    public TaskService(Client supabase, AuthService authService)
    {
        _supabase = supabase;
        _authService = authService;
    }

    public async Task<IEnumerable<GtdItem>> GetItemsAsync()
    {
        var user = _authService.CurrentUser;
        var response = await _supabase.From<GtdItem>().Filter("owner_username", Postgrest.Constants.Operator.Equals, user).Get();
        return response.Models.Where(x => x.OwnerUsername == user);
    }

    public async Task<GtdItem?> GetItemAsync(Guid id)
    {
        var user = _authService.CurrentUser;
        var response = await _supabase.From<GtdItem>().Where(x => x.Id == id).Filter("owner_username", Postgrest.Constants.Operator.Equals, user).Get();
        return response.Models.FirstOrDefault(x => x.OwnerUsername == user);
    }

    public async Task AddItemAsync(GtdItem item)
    {
        item.OwnerUsername = _authService.CurrentUser;
        await _supabase.From<GtdItem>().Insert(item);
    }

    public async Task UpdateItemAsync(GtdItem item)
    {
        item.OwnerUsername = _authService.CurrentUser;
        await _supabase.From<GtdItem>().Update(item);
    }

    public async Task DeleteItemAsync(Guid id)
    {
        var user = _authService.CurrentUser;
        await _supabase.From<GtdItem>().Where(x => x.Id == id).Filter("owner_username", Postgrest.Constants.Operator.Equals, user).Delete();
    }
}
