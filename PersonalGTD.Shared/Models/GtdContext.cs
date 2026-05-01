using Postgrest.Attributes;
using Postgrest.Models;

namespace PersonalGTD.Shared.Models;

[Table("contexts")]
public class GtdContext : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("color")]
    public string Color { get; set; } = "#94a3b8"; // Default Slate
}
