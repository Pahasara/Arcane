using Arcane.Core.Data;
using Arcane.Core.Encryption;
using Arcane.Core.Models.DTOs;
using Arcane.Core.Services.Implementations;
using Arcane.Core.Services.Interfaces;
using Arcane.Tests.Helpers;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;

namespace Arcane.Tests;

/// <summary>
/// Integration tests for TagService.
/// Each test gets a fresh temp SQLite DB via IAsyncLifetime.
/// EntryService is used alongside TagService in a few tests to verify
/// assignment actually links tags to entries end-to-end.
/// </summary>
public sealed class TagServiceTests : IAsyncLifetime
{
    private static readonly byte[] TestKey =
        System.Text.Encoding.UTF8.GetBytes("ArcaneTagTestKey32BytesExactly!!");

    private string _dbPath = null!;
    private IDbContextFactory<ArcaneDbContext> _dbFactory = null!;
    private ITagService   _sut          = null!;
    private IEntryService _entryService = null!;

    public async ValueTask InitializeAsync()
    {
        _dbPath    = Path.Combine(Path.GetTempPath(), $"arcane_tag_{Guid.NewGuid():N}.db");
        _dbFactory = new TestDbContextFactory(_dbPath);

        await using var db = _dbFactory.CreateDbContext();
        await db.Database.MigrateAsync();

        _sut          = new TagService(_dbFactory);
        _entryService = new EntryService(_dbFactory, new AesEncryptionService());
    }

    public ValueTask DisposeAsync()
    {
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
        return ValueTask.CompletedTask;
    }

    // CreateAsync
    [Fact]
    public async Task Create_AddsTagToList()
    {
        var tag = await _sut.CreateAsync("Personal", "#7C3AED");

        tag.Name.Should().Be("Personal");
        tag.ColorHex.Should().Be("#7C3AED");

        var all = await _sut.GetAllAsync();
        all.Should().ContainSingle(t => t.Id == tag.Id);
    }

    [Fact]
    public async Task Create_TrimsWhitespace()
    {
        var tag = await _sut.CreateAsync("  Thoughts  ", "#2563EB");
        tag.Name.Should().Be("Thoughts");
    }

    [Fact]
    public async Task Create_ThrowsOnEmptyName()
    {
        var act = async () => await _sut.CreateAsync("", "#7C3AED");
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Create_ThrowsOnDuplicateName_CaseInsensitive()
    {
        await _sut.CreateAsync("Work", "#16A34A");

        var act = async () => await _sut.CreateAsync("work", "#EAB308");
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Create_DefaultsToArcanePurple_WhenColorEmpty()
    {
        var tag = await _sut.CreateAsync("NoColor", "");
        tag.ColorHex.Should().Be("#7C3AED");
    }

    // RenameAsync
    [Fact]
    public async Task Rename_UpdatesName()
    {
        var tag = await _sut.CreateAsync("OldName", "#7C3AED");
        await _sut.RenameAsync(tag.Id, "NewName");

        var all = await _sut.GetAllAsync();
        all.Should().ContainSingle(t => t.Id == tag.Id && t.Name == "NewName");
    }

    [Fact]
    public async Task Rename_ThrowsForMissingTag()
    {
        var act = async () => await _sut.RenameAsync(Guid.NewGuid(), "X");
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // DeleteAsync
    [Fact]
    public async Task Delete_RemovesTag()
    {
        var tag = await _sut.CreateAsync("ToDelete", "#EF4444");
        await _sut.DeleteAsync(tag.Id);

        var all = await _sut.GetAllAsync();
        all.Should().NotContain(t => t.Id == tag.Id);
    }

    [Fact]
    public async Task Delete_RemovesEntryTagLinks()
    {
        var tag   = await _sut.CreateAsync("Linked", "#7C3AED");
        var entry = await _entryService.CreateAsync(
            new CreateEntryRequest("T", "C", null, [tag.Id]), TestKey);

        await _sut.DeleteAsync(tag.Id);

        var refreshed = await _entryService.GetByIdAsync(entry.Id, TestKey);
        refreshed.Tags.Should().BeEmpty(
            because: "deleting a tag must remove its EntryTag links");
    }

    // AssignTagAsync / RemoveTagAsync
    [Fact]
    public async Task AssignTag_LinksToEntry()
    {
        var tag   = await _sut.CreateAsync("Peaceful", "#0891B2");
        var entry = await _entryService.CreateAsync(
            new CreateEntryRequest("T", "C", null, []), TestKey);

        await _sut.AssignTagAsync(entry.Id, tag.Id);

        var refreshed = await _entryService.GetByIdAsync(entry.Id, TestKey);
        refreshed.Tags.Should().ContainSingle(t => t.Id == tag.Id);
    }

    [Fact]
    public async Task AssignTag_IsIdempotent()
    {
        var tag   = await _sut.CreateAsync("Repeat", "#7C3AED");
        var entry = await _entryService.CreateAsync(
            new CreateEntryRequest("T", "C", null, []), TestKey);

        await _sut.AssignTagAsync(entry.Id, tag.Id);
        var act = async () => await _sut.AssignTagAsync(entry.Id, tag.Id);

        await act.Should().NotThrowAsync(
            because: "assigning an already-assigned tag should be a safe no-op");

        var refreshed = await _entryService.GetByIdAsync(entry.Id, TestKey);
        refreshed.Tags.Should().HaveCount(1,
            because: "duplicate assignment must not create a second link");
    }

    [Fact]
    public async Task AssignTag_ThrowsForMissingEntry()
    {
        var tag = await _sut.CreateAsync("Orphan", "#7C3AED");
        var act = async () => await _sut.AssignTagAsync(Guid.NewGuid(), tag.Id);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task RemoveTag_UnlinksFromEntry()
    {
        var tag   = await _sut.CreateAsync("Removable", "#7C3AED");
        var entry = await _entryService.CreateAsync(
            new CreateEntryRequest("T", "C", null, [tag.Id]), TestKey);

        await _sut.RemoveTagAsync(entry.Id, tag.Id);

        var refreshed = await _entryService.GetByIdAsync(entry.Id, TestKey);
        refreshed.Tags.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveTag_IsIdempotent()
    {
        var tag   = await _sut.CreateAsync("NeverAssigned", "#7C3AED");
        var entry = await _entryService.CreateAsync(
            new CreateEntryRequest("T", "C", null, []), TestKey);

        var act = async () => await _sut.RemoveTagAsync(entry.Id, tag.Id);
        await act.Should().NotThrowAsync(
            because: "removing a tag that isn't assigned should be a safe no-op");
    }

    // GetAllAsync ordering
    [Fact]
    public async Task GetAll_ReturnsAlphabeticalOrder()
    {
        await _sut.CreateAsync("Zebra",  "#7C3AED");
        await _sut.CreateAsync("Apple",  "#2563EB");
        await _sut.CreateAsync("Mango",  "#16A34A");

        var all = await _sut.GetAllAsync();

        all.Select(t => t.Name).Should().BeInAscendingOrder();
    }
}
