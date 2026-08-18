using Arcane.Core.Data;
using Arcane.Core.Models.Entities;
using Arcane.Core.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Arcane.Core.Services.Implementations;

/// <summary>
/// Tag CRUD and entry-tag assignment.
/// Tag names and colors are stored plaintext — low-sensitivity labels
/// like "Work" or "Family" don't need encryption. The entries they're
/// attached to remain fully encrypted regardless.
/// </summary>
public sealed class TagService(IDbContextFactory<ArcaneDbContext> dbFactory) : ITagService
{
    // GetAllAsync
    public async Task<List<Tag>> GetAllAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        return await db.Tags
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    // CreateAsync
    public async Task<Tag> CreateAsync(string name, string colorHex)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tag name cannot be empty.", nameof(name));

        await using var db = await dbFactory.CreateDbContextAsync();

        // Prevent duplicate tag names (case-insensitive)
        var exists = await db.Tags.AnyAsync(t =>
            t.Name.ToLower() == name.Trim().ToLower());

        if (exists)
            throw new InvalidOperationException($"A tag named \"{name}\" already exists.");

        var tag = new Tag
        {
            Name     = name.Trim(),
            ColorHex = string.IsNullOrWhiteSpace(colorHex) ? "#7C3AED" : colorHex
        };

        db.Tags.Add(tag);
        await db.SaveChangesAsync();

        return tag;
    }

    // RenameAsync
    public async Task RenameAsync(Guid id, string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Tag name cannot be empty.", nameof(newName));

        await using var db = await dbFactory.CreateDbContextAsync();

        var tag = await db.Tags.FirstOrDefaultAsync(t => t.Id == id)
            ?? throw new KeyNotFoundException($"Tag {id} not found.");

        tag.Name      = newName.Trim();
        tag.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
    }

    // DeleteAsync
    public async Task DeleteAsync(Guid id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var tag = await db.Tags
            .Include(t => t.EntryTags)
            .FirstOrDefaultAsync(t => t.Id == id)
            ?? throw new KeyNotFoundException($"Tag {id} not found.");

        // EF cascade delete (configured in ArcaneDbContext) removes EntryTag rows too,
        // but we remove explicitly here for clarity and to avoid relying solely on cascade config.
        db.EntryTags.RemoveRange(tag.EntryTags);
        db.Tags.Remove(tag);

        await db.SaveChangesAsync();
    }

    // AssignTagAsync
    public async Task AssignTagAsync(Guid entryId, Guid tagId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var alreadyAssigned = await db.EntryTags.AnyAsync(et =>
            et.EntryId == entryId && et.TagId == tagId);

        if (alreadyAssigned) return; // idempotent — no-op if already assigned

        var entryExists = await db.Entries.AnyAsync(e => e.Id == entryId);
        var tagExists    = await db.Tags.AnyAsync(t => t.Id == tagId);

        if (!entryExists) throw new KeyNotFoundException($"Entry {entryId} not found.");
        if (!tagExists)   throw new KeyNotFoundException($"Tag {tagId} not found.");

        db.EntryTags.Add(new EntryTag { EntryId = entryId, TagId = tagId });
        await db.SaveChangesAsync();
    }

    // RemoveTagAsync
    public async Task RemoveTagAsync(Guid entryId, Guid tagId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var link = await db.EntryTags.FirstOrDefaultAsync(et =>
            et.EntryId == entryId && et.TagId == tagId);

        if (link is null) return; // idempotent — no-op if not assigned

        db.EntryTags.Remove(link);
        await db.SaveChangesAsync();
    }
}
