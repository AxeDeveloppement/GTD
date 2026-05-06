using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace PersonalGTD.Shared.Models;

[Table("projects")]
public class GtdProject : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string Description { get; set; } = string.Empty;

    [Column("color")]
    public string Color { get; set; } = "#4f46e5"; // Default Indigo

    [Column("owner_username")]
    public string? OwnerUsername { get; set; }

    [Column("status")]
    public GtdStatus Status { get; set; } = GtdStatus.NextAction;
}
