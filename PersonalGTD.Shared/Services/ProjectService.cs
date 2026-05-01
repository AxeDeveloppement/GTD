using PersonalGTD.Shared.Models;
using Supabase;

namespace PersonalGTD.Shared.Services;

public class ProjectService : IProjectService
{
    private readonly Client _supabase;

    public ProjectService(Client supabase)
    {
        _supabase = supabase;
    }

    public async Task<List<GtdProject>> GetProjectsAsync()
    {
        var response = await _supabase.From<GtdProject>().Get();
        return response.Models;
    }

    public async Task<GtdProject?> GetProjectAsync(Guid id)
    {
        var response = await _supabase.From<GtdProject>().Where(x => x.Id == id).Get();
        return response.Models.FirstOrDefault();
    }

    public async Task AddProjectAsync(GtdProject project)
    {
        await _supabase.From<GtdProject>().Insert(project);
    }

    public async Task UpdateProjectAsync(GtdProject project)
    {
        await _supabase.From<GtdProject>().Update(project);
    }

    public async Task DeleteProjectAsync(Guid id)
    {
        await _supabase.From<GtdProject>().Where(x => x.Id == id).Delete();
    }
}
