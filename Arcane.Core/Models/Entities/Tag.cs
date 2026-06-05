namespace Arcane.Core.Models.Entities;

public sealed class Tag : BaseEntity
{
    public required string Name     { get; set; }
    public string ColorHex          { get; set; } = "#7C3AED";

    public List<EntryTag> EntryTags { get; set; } = [];
}
