using Arcane.Core.Models.Entities;
using Arcane.Core.Models.Enums;

namespace Arcane.Core.Models.DTOs;

/// <summary>
/// Represents a fully decrypted entry, safe to pass to ViewModels.
/// Title and Content are plaintext strings here — decryption happened in the service layer.
/// </summary>
public sealed record EntryDto(
    Guid                           Id,
    string                         Title,        // decrypted
    string                         Content,      // decrypted (Markdown)
    MoodLevel?                     Mood,
    bool                           IsFavorite,
    DateTime                       CreatedAt,
    DateTime                       UpdatedAt,
    IReadOnlyList<Tag>             Tags,
    IReadOnlyList<AttachmentMetaDto> Attachments
);

public sealed record AttachmentMetaDto(
    Guid   Id,
    string FileName,      // decrypted
    string MimeType,
    long   FileSizeBytes
);

public sealed record CreateEntryRequest(
    string         Title,
    string         Content,
    MoodLevel?     Mood,
    IList<Guid>    TagIds
);

public sealed record UpdateEntryRequest(
    string         Title,
    string         Content,
    MoodLevel?     Mood,
    IList<Guid>    TagIds,
    bool           IsFavorite
);
