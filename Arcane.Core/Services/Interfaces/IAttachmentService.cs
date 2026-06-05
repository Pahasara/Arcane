using Arcane.Core.Models.Entities;

namespace Arcane.Core.Services.Interfaces;

public interface IAttachmentService
{
    Task<Attachment> AddAttachmentAsync(Guid entryId, string filePath, byte[] masterKey);
    Task<byte[]>     GetDecryptedBytesAsync(Guid attachmentId, byte[] masterKey);
    Task             DeleteAsync(Guid attachmentId);
}
