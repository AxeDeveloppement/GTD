using Postgrest.Attributes;
using Postgrest.Models;

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
}
