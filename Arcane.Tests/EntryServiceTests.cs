using Arcane.Core.Data;
using Arcane.Core.Encryption;
using Arcane.Core.Models.DTOs;
using Arcane.Core.Models.Enums;
using Arcane.Core.Services.Implementations;
using Arcane.Core.Services.Interfaces;
using Arcane.Tests.Helpers;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;

namespace Arcane.Tests;

/// <summary>
/// Integration tests for EntryService.
/// Each test gets a fresh temp SQLite DB via IAsyncLifetime.
/// The master key here is a fixed 32-byte test key — NOT derived via Argon2id,
/// since that would add 1-2s per test. Key derivation itself is covered
/// separately in VaultServiceTests.
/// </summary>
public sealed class EntryServiceTests : IAsyncLifetime
{
    private static readonly byte[] TestKey = System.Text.Encoding.UTF8.GetBytes("ArcaneEntryTestKey32BytesExactly");

    private string _dbPath = null!;
    private IDbContextFactory<ArcaneDbContext> _dbFactory = null!;
    private IEntryService _sut = null!;

    public async ValueTask InitializeAsync()
    {
        _dbPath    = Path.Combine(Path.GetTempPath(), $"arcane_entry_{Guid.NewGuid():N}.db");
        _dbFactory = new TestDbContextFactory(_dbPath);

        await using var db = await _dbFactory.CreateDbContextAsync();
        await db.Database.MigrateAsync();

        _sut = new EntryService(_dbFactory, new AesEncryptionService());
    }

    public ValueTask DisposeAsync()
    {
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
        return ValueTask.CompletedTask;
    }

    // CreateAsync
    [Fact]
    public async Task Create_StoresEncryptedFieldsInDb()
    {
        var req = new CreateEntryRequest("My Title", "My Content", MoodLevel.Good, []);
        var dto = await _sut.CreateAsync(req, TestKey);

        await using var db = await _dbFactory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var raw = await db.Entries.FirstAsync(cancellationToken: TestContext.Current.CancellationToken);

        raw.TitleEncrypted.Should().NotBeEmpty();
        System.Text.Encoding.UTF8.GetString(raw.TitleEncrypted)
              .Should().NotContain("My Title",
                  because: "title must be encrypted, not stored as plaintext");

        dto.Title.Should().Be("My Title");
        dto.Content.Should().Be("My Content");
        dto.Mood.Should().Be(MoodLevel.Good);
    }

    [Fact]
    public async Task Create_AssignsNewGuid()
    {
        var dto = await _sut.CreateAsync(
            new CreateEntryRequest("T", "C", null, []), TestKey);
        dto.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Create_SetsCreatedAtUtc()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var dto    = await _sut.CreateAsync(
            new CreateEntryRequest("T", "C", null, []), TestKey);
        var after  = DateTime.UtcNow.AddSeconds(1);

        dto.CreatedAt.Should().BeAfter(before).And.BeBefore(after);
    }

    // GetByIdAsync
    [Fact]
    public async Task GetById_DecryptsCorrectly()
    {
        var created = await _sut.CreateAsync(
            new CreateEntryRequest("Hello", "World", MoodLevel.Great, []), TestKey);

        var fetched = await _sut.GetByIdAsync(created.Id, TestKey);

        fetched.Title.Should().Be("Hello");
        fetched.Content.Should().Be("World");
        fetched.Mood.Should().Be(MoodLevel.Great);
    }

    [Fact]
    public async Task GetById_ThrowsKeyNotFound_ForMissingId()
    {
        var act = async () => await _sut.GetByIdAsync(Guid.NewGuid(), TestKey);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // GetAllAsync
    [Fact]
    public async Task GetAll_ReturnsAllEntries()
    {
        await _sut.CreateAsync(new CreateEntryRequest("A", "a", null, []), TestKey);
        await _sut.CreateAsync(new CreateEntryRequest("B", "b", null, []), TestKey);
        await _sut.CreateAsync(new CreateEntryRequest("C", "c", null, []), TestKey);

        var all = await _sut.GetAllAsync(TestKey);
        all.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAll_NewestFirstByDefault()
    {
        var first  = await _sut.CreateAsync(new CreateEntryRequest("First", "", null, []), TestKey);
        await Task.Delay(10, TestContext.Current.CancellationToken);
        var second = await _sut.CreateAsync(new CreateEntryRequest("Second", "", null, []), TestKey);

        var all = await _sut.GetAllAsync(TestKey, SortOrder.NewestFirst);

        all[0].Id.Should().Be(second.Id,
            because: "newest entry should appear first");
    }

    [Fact]
    public async Task GetAll_AlphabeticalSort()
    {
        await _sut.CreateAsync(new CreateEntryRequest("Banana", "", null, []), TestKey);
        await _sut.CreateAsync(new CreateEntryRequest("Apple",  "", null, []), TestKey);
        await _sut.CreateAsync(new CreateEntryRequest("Cherry", "", null, []), TestKey);

        var all = await _sut.GetAllAsync(TestKey, SortOrder.Alphabetical);

        all.Select(e => e.Title).Should().BeInAscendingOrder();
    }

    // UpdateAsync
    [Fact]
    public async Task Update_ReEncryptsNewContent()
    {
        var created = await _sut.CreateAsync(
            new CreateEntryRequest("Old", "Old content", null, []), TestKey);

        var req     = new UpdateEntryRequest("New", "New content", MoodLevel.Good, [], false);
        var updated = await _sut.UpdateAsync(created.Id, req, TestKey);

        updated.Title.Should().Be("New");
        updated.Content.Should().Be("New content");
        updated.Mood.Should().Be(MoodLevel.Good);
    }

    [Fact]
    public async Task Update_ChangesUpdatedAt()
    {
        var created    = await _sut.CreateAsync(
            new CreateEntryRequest("T", "C", null, []), TestKey);
        var originalAt = created.UpdatedAt;

        await Task.Delay(20, TestContext.Current.CancellationToken);
        var req     = new UpdateEntryRequest("T", "C2", null, [], false);
        var updated = await _sut.UpdateAsync(created.Id, req, TestKey);

        updated.UpdatedAt.Should().BeAfter(originalAt);
    }

    // ToggleFavoriteAsync
    [Fact]
    public async Task ToggleFavorite_FlipsFlag()
    {
        var entry = await _sut.CreateAsync(
            new CreateEntryRequest("T", "C", null, []), TestKey);

        entry.IsFavorite.Should().BeFalse(because: "new entries are not favorited");

        await _sut.ToggleFavoriteAsync(entry.Id);
        var after = await _sut.GetByIdAsync(entry.Id, TestKey);
        after.IsFavorite.Should().BeTrue();

        await _sut.ToggleFavoriteAsync(entry.Id);
        var afterAgain = await _sut.GetByIdAsync(entry.Id, TestKey);
        afterAgain.IsFavorite.Should().BeFalse();
    }

    // DeleteAsync
    [Fact]
    public async Task Delete_RemovesEntry()
    {
        var entry = await _sut.CreateAsync(
            new CreateEntryRequest("T", "C", null, []), TestKey);

        await _sut.DeleteAsync(entry.Id);

        var all = await _sut.GetAllAsync(TestKey);
        all.Should().BeEmpty();
    }

    [Fact]
    public async Task Delete_GetById_ThrowsAfterDelete()
    {
        var entry = await _sut.CreateAsync(
            new CreateEntryRequest("T", "C", null, []), TestKey);
        await _sut.DeleteAsync(entry.Id);

        var act = async () => await _sut.GetByIdAsync(entry.Id, TestKey);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // Encryption correctness
    [Fact]
    public async Task Create_DifferentNoncesPerEntry_SameContent()
    {
        var a = await _sut.CreateAsync(new CreateEntryRequest("X", "Same", null, []), TestKey);
        var b = await _sut.CreateAsync(new CreateEntryRequest("X", "Same", null, []), TestKey);

        await using var db = await _dbFactory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var rawA = await db.Entries.FirstAsync(e => e.Id == a.Id, cancellationToken: TestContext.Current.CancellationToken);
        var rawB = await db.Entries.FirstAsync(e => e.Id == b.Id, cancellationToken: TestContext.Current.CancellationToken);

        rawA.ContentEncrypted.Should().NotEqual(rawB.ContentEncrypted,
            because: "fresh nonce per encryption must produce unique ciphertext");
    }
}
