using PersonalGTD.Shared.Models;

namespace PersonalGTD.Shared.Services;

public interface ITaskService
{
    Task<IEnumerable<TaskModel>> GetTasksAsync();
    Task AddTaskAsync(TaskModel task);
    Task UpdateTaskAsync(TaskModel task);
    Task DeleteTaskAsync(string id);
}
