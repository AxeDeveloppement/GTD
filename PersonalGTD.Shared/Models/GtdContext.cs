namespace PersonalGTD.Shared.Models;

public class GtdContext
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#94a3b8"; // Default Slate
}
