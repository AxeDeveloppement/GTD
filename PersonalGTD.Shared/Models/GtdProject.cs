namespace PersonalGTD.Shared.Models;

public class GtdProject
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Color { get; set; } = "#4f46e5"; // Default Indigo
}
