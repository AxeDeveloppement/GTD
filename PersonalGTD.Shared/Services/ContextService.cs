using PersonalGTD.Shared.Models;
using Supabase;

namespace PersonalGTD.Shared.Services;

public class ContextService : IContextService
{
    private readonly Client _supabase;
    private readonly AuthService _authService;

    public ContextService(Client supabase, AuthService authService)
    {
        _supabase = supabase;
        _authService = authService;
    }

    public async Task<List<GtdContext>> GetContextsAsync()
    {
        var user = _authService.CurrentUser;
        var response = await _supabase.From<GtdContext>().Filter("owner_username", Supabase.Postgrest.Constants.Operator.Equals, user).Get();
        return response.Models.Where(x => x.OwnerUsername == user).ToList();
    }

    public async Task<GtdContext?> GetContextAsync(Guid id)
    {
        var user = _authService.CurrentUser;
        var response = await _supabase.From<GtdContext>().Where(x => x.Id == id).Filter("owner_username", Supabase.Postgrest.Constants.Operator.Equals, user).Get();
        return response.Models.FirstOrDefault(x => x.OwnerUsername == user);
    }

    public async Task AddContextAsync(GtdContext context)
    {
        context.OwnerUsername = _authService.CurrentUser;
        await _supabase.From<GtdContext>().Insert(context);
    }

    public async Task UpdateContextAsync(GtdContext context)
    {
        context.OwnerUsername = _authService.CurrentUser;
        await _supabase.From<GtdContext>().Update(context);
    }

    public async Task DeleteContextAsync(Guid id)
    {
        var user = _authService.CurrentUser;
        await _supabase.From<GtdContext>().Where(x => x.Id == id).Filter("owner_username", Supabase.Postgrest.Constants.Operator.Equals, user).Delete();
    }
}
