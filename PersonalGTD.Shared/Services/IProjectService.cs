using PersonalGTD.Shared.Models;

namespace PersonalGTD.Shared.Services;

public interface IProjectService
{
    Task<List<GtdProject>> GetProjectsAsync();
    Task<GtdProject?> GetProjectAsync(Guid id);
    Task AddProjectAsync(GtdProject project);
    Task UpdateProjectAsync(GtdProject project);
    Task DeleteProjectAsync(Guid id);
}
