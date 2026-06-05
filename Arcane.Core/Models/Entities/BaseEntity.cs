namespace Arcane.Core.Models.Entities;

/// <summary>
/// All EF Core entities inherit this. Id is a Guid generated client-side.
/// All DateTimes are stored as UTC — convert to local time only in the UI layer.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
