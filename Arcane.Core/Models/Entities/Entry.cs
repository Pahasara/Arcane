using Arcane.Core.Models.Enums;

namespace Arcane.Core.Models.Entities;

public sealed class Entry : BaseEntity
{
    public required byte[] TitleEncrypted   { get; set; }
    public required byte[] TitleNonce       { get; set; }
    public required byte[] ContentEncrypted { get; set; }
    public required byte[] ContentNonce     { get; set; }

    // Stored plaintext (1–5)
    public MoodLevel? Mood       { get; set; }
    public bool       IsFavorite { get; set; }

    // Navigation properties
    public List<EntryTag>   EntryTags   { get; set; } = [];
    public List<Attachment> Attachments { get; set; } = [];
}
