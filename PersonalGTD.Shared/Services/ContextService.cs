using PersonalGTD.Shared.Models;
using Supabase;

namespace PersonalGTD.Shared.Services;

public class ContextService : IContextService
{
    private readonly Client _supabase;

    public ContextService(Client supabase)
    {
        _supabase = supabase;
    }

    public async Task<List<GtdContext>> GetContextsAsync()
    {
        var response = await _supabase.From<GtdContext>().Get();
        return response.Models;
    }

    public async Task<GtdContext?> GetContextAsync(Guid id)
    {
        var response = await _supabase.From<GtdContext>().Where(x => x.Id == id).Get();
        return response.Models.FirstOrDefault();
    }

    public async Task AddContextAsync(GtdContext context)
    {
        await _supabase.From<GtdContext>().Insert(context);
    }

    public async Task UpdateContextAsync(GtdContext context)
    {
        await _supabase.From<GtdContext>().Update(context);
    }

    public async Task DeleteContextAsync(Guid id)
    {
        await _supabase.From<GtdContext>().Where(x => x.Id == id).Delete();
    }
}
