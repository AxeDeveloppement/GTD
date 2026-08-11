using PersonalGTD.Shared.Models;
using Supabase;

namespace PersonalGTD.Shared.Services;

public class ProjectService : IProjectService
{
    private readonly Client _supabase;
    private readonly AuthService _authService;

    public ProjectService(Client supabase, AuthService authService)
    {
        _supabase = supabase;
        _authService = authService;
    }

    public async Task<List<GtdProject>> GetProjectsAsync()
    {
        var user = _authService.CurrentUser;
        if (string.IsNullOrEmpty(user)) return new List<GtdProject>();
        var response = await _supabase.From<GtdProject>().Filter("owner_username", Supabase.Postgrest.Constants.Operator.Equals, user).Get();
        return response.Models.Where(x => x.OwnerUsername == user).ToList();
    }

    public async Task<GtdProject?> GetProjectAsync(Guid id)
    {
        var user = _authService.CurrentUser;
        if (string.IsNullOrEmpty(user)) return null;
        var response = await _supabase.From<GtdProject>().Where(x => x.Id == id).Filter("owner_username", Supabase.Postgrest.Constants.Operator.Equals, user).Get();
        return response.Models.FirstOrDefault(x => x.OwnerUsername == user);
    }

    public async Task AddProjectAsync(GtdProject project)
    {
        var user = _authService.CurrentUser;
        if (string.IsNullOrEmpty(user)) return;
        project.OwnerUsername = user;
        await _supabase.From<GtdProject>().Insert(project);
    }

    public async Task UpdateProjectAsync(GtdProject project)
    {
        var user = _authService.CurrentUser;
        if (string.IsNullOrEmpty(user)) return;
        project.OwnerUsername = user;
        // CRIT-04: Ajouter le filtre owner_username pour empêcher le take-over de projet
        await _supabase
            .From<GtdProject>()
            .Where(x => x.Id == project.Id)
            .Filter("owner_username", Supabase.Postgrest.Constants.Operator.Equals, user)
            .Update(project);
    }

    public async Task DeleteProjectAsync(Guid id)
    {
        var user = _authService.CurrentUser;
        if (string.IsNullOrEmpty(user)) return;
        await _supabase.From<GtdProject>().Where(x => x.Id == id).Filter("owner_username", Supabase.Postgrest.Constants.Operator.Equals, user).Delete();
    }
}
