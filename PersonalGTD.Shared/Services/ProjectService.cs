using PersonalGTD.Shared.Models;

namespace PersonalGTD.Shared.Services;

public class ProjectService : IProjectService
{
    private readonly List<GtdProject> _projects = new();

    public Task<List<GtdProject>> GetProjectsAsync()
    {
        return Task.FromResult(_projects.ToList());
    }

    public Task<GtdProject?> GetProjectAsync(Guid id)
    {
        return Task.FromResult(_projects.FirstOrDefault(p => p.Id == id));
    }

    public Task AddProjectAsync(GtdProject project)
    {
        _projects.Add(project);
        return Task.CompletedTask;
    }

    public Task UpdateProjectAsync(GtdProject project)
    {
        var index = _projects.FindIndex(p => p.Id == project.Id);
        if (index != -1)
        {
            _projects[index] = project;
        }
        return Task.CompletedTask;
    }

    public Task DeleteProjectAsync(Guid id)
    {
        _projects.RemoveAll(p => p.Id == id);
        return Task.CompletedTask;
    }
}
