using Postgrest.Attributes;
using Postgrest.Models;

namespace PersonalGTD.Shared.Models;

[Table("tasks")]
public class GtdItem : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("note")]
    public string Note { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Column("status")]
    public GtdStatus Status { get; set; } = GtdStatus.Inbox;
    
    [Column("project_id")]
    public Guid? ProjectId { get; set; }

    [Column("context_id")]
    public Guid? ContextId { get; set; }

    [Column("energy_level")]
    public int EnergyLevel { get; set; } = 3; // 1 to 5

    [Column("estimated_time")]
    public TimeSpan EstimatedTime { get; set; } = TimeSpan.Zero;

    [Column("due_date")]
    public DateTime? DueDate { get; set; }
}
