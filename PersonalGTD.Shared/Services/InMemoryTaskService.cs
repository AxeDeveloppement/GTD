using PersonalGTD.Shared.Models;

namespace PersonalGTD.Shared.Services;

public class InMemoryTaskService : ITaskService
{
    private readonly List<TaskModel> _tasks = new();

    public Task<IEnumerable<TaskModel>> GetTasksAsync()
    {
        return Task.FromResult<IEnumerable<TaskModel>>(_tasks);
    }

    public Task AddTaskAsync(TaskModel task)
    {
        _tasks.Add(task);
        return Task.CompletedTask;
    }

    public Task UpdateTaskAsync(TaskModel task)
    {
        var existing = _tasks.FirstOrDefault(t => t.Id == task.Id);
        if (existing != null)
        {
            existing.Title = task.Title;
            existing.Context = task.Context;
            existing.IsCompleted = task.IsCompleted;
        }
        return Task.CompletedTask;
    }

    public Task DeleteTaskAsync(string id)
    {
        var existing = _tasks.FirstOrDefault(t => t.Id == id);
        if (existing != null)
        {
            _tasks.Remove(existing);
        }
        return Task.CompletedTask;
    }
}
