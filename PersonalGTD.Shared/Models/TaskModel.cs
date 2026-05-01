namespace PersonalGTD.Shared.Models;

public class TaskModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Context { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
}
