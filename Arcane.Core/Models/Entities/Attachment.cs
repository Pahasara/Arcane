namespace Arcane.Core.Models.Entities;

/// <summary>
/// Files and images attached to entries. All sensitive fields are encrypted.
/// Files ≤1MB: encrypted bytes stored directly in DataEncrypted (in DB).
/// Files >1MB: encrypted bytes written to {AttachmentsDir}/{Id}.enc on disk;
///             path stored encrypted in ExternalPathEncrypted.
/// </summary>
public sealed class Attachment : BaseEntity
{
    public Guid  EntryId { get; set; }
    public Entry Entry   { get; set; } = null!;

    public required byte[] FileNameEncrypted { get; set; }
    public required byte[] FileNameNonce     { get; set; }

    public required string MimeType      { get; set; }
    public long            FileSizeBytes { get; set; }

    // Inline storage
    public byte[]? DataEncrypted { get; set; }
    public byte[]? DataNonce     { get; set; }

    // External storage
    public bool    IsStoredExternally    { get; set; }
    public byte[]? ExternalPathEncrypted { get; set; }
    public byte[]? ExternalPathNonce     { get; set; }
}
