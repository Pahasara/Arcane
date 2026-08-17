using System.Text;
using Arcane.Core.Data;
using Arcane.Core.Encryption;
using Arcane.Core.Models.DTOs;
using Arcane.Core.Models.Entities;
using Arcane.Core.Models.Enums;
using Arcane.Core.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Arcane.Core.Services.Implementations;

/// <summary>
/// All diary entry operations. Every sensitive field (title, content) is
/// AES-256-GCM encrypted before write and decrypted after read.
/// The master key is never stored here — it is passed in by the caller
/// (ViewModel calls VaultService.GetKey() and hands it down).
/// </summary>
public sealed class EntryService(IDbContextFactory<ArcaneDbContext> dbFactory, IEncryptionService encryption)
    : IEntryService
{
    public async Task<List<EntryDto>> GetAllAsync(byte[] masterKey, SortOrder sort = SortOrder.NewestFirst)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var entries = await db.Entries
            .Include(e => e.EntryTags).ThenInclude(et => et.Tag)
            .Include(e => e.Attachments)
            .AsNoTracking()
            .ToListAsync();

        var dtos = entries.Select(e => ToDto(e, masterKey)).ToList();

        return sort switch
        {
            SortOrder.NewestFirst  => dtos.OrderByDescending(e => e.CreatedAt).ToList(),
            SortOrder.OldestFirst  => dtos.OrderBy(e => e.CreatedAt).ToList(),
            SortOrder.Alphabetical => dtos.OrderBy(e => e.Title, StringComparer.OrdinalIgnoreCase).ToList(),
            SortOrder.LastModified => dtos.OrderByDescending(e => e.UpdatedAt).ToList(),
            _                      => dtos.OrderByDescending(e => e.CreatedAt).ToList()
        };
    }

    public async Task<EntryDto> GetByIdAsync(Guid id, byte[] masterKey)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var entry = await db.Entries
            .Include(e => e.EntryTags).ThenInclude(et => et.Tag)
            .Include(e => e.Attachments)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id)
            ?? throw new KeyNotFoundException($"Entry {id} not found.");

        return ToDto(entry, masterKey);
    }

    public async Task<EntryDto> CreateAsync(CreateEntryRequest req, byte[] masterKey)
    {
        var (titlePayload, titleNonce)     = EncryptField(req.Title,   masterKey);
        var (contentPayload, contentNonce) = EncryptField(req.Content, masterKey);

        var entry = new Entry
        {
            TitleEncrypted   = titlePayload,
            TitleNonce       = titleNonce,
            ContentEncrypted = contentPayload,
            ContentNonce     = contentNonce,
            Mood             = req.Mood,
            IsFavorite       = false,
        };

        await using var db = await dbFactory.CreateDbContextAsync();

        if (req.TagIds.Count > 0)
        {
            var tagIds = req.TagIds.ToHashSet();
            var tags   = await db.Tags
                .Where(t => tagIds.Contains(t.Id))
                .ToListAsync();

            entry.EntryTags = tags.Select(t => new EntryTag
            {
                EntryId = entry.Id,
                TagId   = t.Id
            }).ToList();
        }

        db.Entries.Add(entry);
        await db.SaveChangesAsync();

        return await GetByIdAsync(entry.Id, masterKey);
    }

    public async Task<EntryDto> UpdateAsync(Guid id, UpdateEntryRequest req, byte[] masterKey)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var entry = await db.Entries
            .Include(e => e.EntryTags)
            .FirstOrDefaultAsync(e => e.Id == id)
            ?? throw new KeyNotFoundException($"Entry {id} not found.");

        // Re-encrypt with fresh nonces
        (entry.TitleEncrypted,   entry.TitleNonce)   = EncryptField(req.Title,   masterKey);
        (entry.ContentEncrypted, entry.ContentNonce) = EncryptField(req.Content, masterKey);

        entry.Mood       = req.Mood;
        entry.IsFavorite = req.IsFavorite;
        entry.UpdatedAt  = DateTime.UtcNow;

        // Replace tags entirely
        db.EntryTags.RemoveRange(entry.EntryTags);

        if (req.TagIds.Count > 0)
        {
            var tagIds = req.TagIds.ToHashSet();
            var tags   = await db.Tags
                .Where(t => tagIds.Contains(t.Id))
                .ToListAsync();

            entry.EntryTags = tags.Select(t => new EntryTag
            {
                EntryId = id,
                TagId   = t.Id
            }).ToList();
        }

        await db.SaveChangesAsync();
        return await GetByIdAsync(id, masterKey);
    }

    public async Task DeleteAsync(Guid id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var entry = await db.Entries
            .Include(e => e.Attachments)
            .FirstOrDefaultAsync(e => e.Id == id)
            ?? throw new KeyNotFoundException($"Entry {id} not found.");

        db.Entries.Remove(entry);
        await db.SaveChangesAsync();
    }


    public async Task ToggleFavoriteAsync(Guid id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var entry = await db.Entries.FirstOrDefaultAsync(e => e.Id == id)
            ?? throw new KeyNotFoundException($"Entry {id} not found.");

        entry.IsFavorite = !entry.IsFavorite;
        entry.UpdatedAt  = DateTime.UtcNow;

        await db.SaveChangesAsync();
    }


    private EntryDto ToDto(Entry entry, byte[] masterKey) => new(
        entry.Id,
        DecryptField(entry.TitleEncrypted,   entry.TitleNonce,   masterKey),
        DecryptField(entry.ContentEncrypted, entry.ContentNonce, masterKey),
        entry.Mood,
        entry.IsFavorite,
        entry.CreatedAt,
        entry.UpdatedAt,
        entry.EntryTags.Select(et => et.Tag).ToList().AsReadOnly(),
        entry.Attachments.Select(a => new AttachmentMetaDto(
            a.Id,
            a.FileNameEncrypted.Length > 0
                ? DecryptField(a.FileNameEncrypted, a.FileNameNonce, masterKey)
                : string.Empty,
            a.MimeType,
            a.FileSizeBytes)).ToList().AsReadOnly()
    );

    private (byte[] Payload, byte[] Nonce) EncryptField(string plaintext, byte[] masterKey) =>
        encryption.Encrypt(Encoding.UTF8.GetBytes(plaintext), masterKey);

    private string DecryptField(byte[] payload, byte[] nonce, byte[] masterKey) =>
        Encoding.UTF8.GetString(encryption.Decrypt(payload, nonce, masterKey));
}
