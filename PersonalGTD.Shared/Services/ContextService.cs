using PersonalGTD.Shared.Models;

namespace PersonalGTD.Shared.Services;

public class ContextService : IContextService
{
    private readonly List<GtdContext> _contexts = new();

    public ContextService()
    {
        // Seed some default contexts
        _contexts.Add(new GtdContext { Name = "@Bureau", Color = "#3b82f6" });
        _contexts.Add(new GtdContext { Name = "@Maison", Color = "#10b981" });
        _contexts.Add(new GtdContext { Name = "@Téléphone", Color = "#8b5cf6" });
        _contexts.Add(new GtdContext { Name = "@Courses", Color = "#f59e0b" });
    }

    public Task<List<GtdContext>> GetContextsAsync()
    {
        return Task.FromResult(_contexts.ToList());
    }

    public Task<GtdContext?> GetContextAsync(Guid id)
    {
        return Task.FromResult(_contexts.FirstOrDefault(c => c.Id == id));
    }

    public Task AddContextAsync(GtdContext context)
    {
        _contexts.Add(context);
        return Task.CompletedTask;
    }

    public Task UpdateContextAsync(GtdContext context)
    {
        var index = _contexts.FindIndex(c => c.Id == context.Id);
        if (index != -1)
        {
            _contexts[index] = context;
        }
        return Task.CompletedTask;
    }

    public Task DeleteContextAsync(Guid id)
    {
        _contexts.RemoveAll(c => c.Id == id);
        return Task.CompletedTask;
    }
}
