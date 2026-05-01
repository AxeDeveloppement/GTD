namespace PersonalGTD.Shared.Models;

public class GtdItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public GtdStatus Status { get; set; } = GtdStatus.Inbox;
    
    public Guid? ProjectId { get; set; }
    public Guid? ContextId { get; set; }
    public int EnergyLevel { get; set; } = 3; // 1 to 5
    public TimeSpan EstimatedTime { get; set; } = TimeSpan.Zero;
}
