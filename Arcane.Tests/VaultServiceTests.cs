using Arcane.Core.Encryption;
using Arcane.Core.Services.Implementations;
using Arcane.Core.Services.Interfaces;
using Arcane.Tests.Helpers;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;

namespace Arcane.Tests;

public sealed class VaultServiceTests : IAsyncLifetime
{
    private string _dbPath = null!;
    private IDbContextFactory<Arcane.Core.Data.ArcaneDbContext> _dbFactory = null!;
    private IVaultService _sut = null!;

    public async ValueTask InitializeAsync()
    {
        // Fresh temp DB for each test — fully isolated
        _dbPath    = Path.Combine(Path.GetTempPath(), $"arcane_test_{Guid.NewGuid():N}.db");
        _dbFactory = new TestDbContextFactory(_dbPath);

        // Apply migrations to create the schema
        using var db = _dbFactory.CreateDbContext();
        await db.Database.MigrateAsync();

        _sut = new VaultService(_dbFactory, new AesEncryptionService(), new KeyDerivationService());
    }

    public ValueTask DisposeAsync()
    {
        // Clean up temp database file
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
        return ValueTask.CompletedTask;
    }

    // --- VaultExists ---

    [Fact]
    public void VaultExists_ReturnsFalse_WhenDatabaseIsEmpty()
    {
        _sut.VaultExists().Should().BeFalse(
            because: "no VaultProfile has been created yet");
    }

    [Fact]
    public async Task VaultExists_ReturnsTrue_AfterSetup()
    {
        await _sut.SetupVaultAsync("MyPassword123!");
        _sut.VaultExists().Should().BeTrue();
    }

    // --- SetupVaultAsync ---

    [Fact]
    public async Task SetupVault_UnlocksVaultInMemory()
    {
        await _sut.SetupVaultAsync("MyPassword123!");

        _sut.IsUnlocked.Should().BeTrue(
            because: "vault should be unlocked immediately after creation");
    }

    [Fact]
    public async Task SetupVault_GetKey_ReturnsNonEmptyKey()
    {
        await _sut.SetupVaultAsync("MyPassword123!");

        var key = _sut.GetKey();
        key.Should().NotBeNullOrEmpty()
           .And.HaveCount(32, because: "AES-256 requires a 32-byte key");
    }

    [Fact]
    public async Task SetupVault_PersistsProfileWithSalt()
    {
        await _sut.SetupVaultAsync("MyPassword123!");

        await using var db = await _dbFactory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var profile = await db.VaultProfiles.FirstOrDefaultAsync(cancellationToken: TestContext.Current.CancellationToken);

        profile.Should().NotBeNull();
        profile!.Salt.Should().HaveCount(16, because: "salt must be 16 bytes");
        profile.VerificationCiphertext.Should().NotBeNullOrEmpty();
        profile.VerificationNonce.Should().NotBeNullOrEmpty();
    }

    // --- UnlockAsync ---

    [Fact]
    public async Task UnlockAsync_ReturnsTrue_WithCorrectPassword()
    {
        const string password = "CorrectPassword99!";
        await _sut.SetupVaultAsync(password);
        _sut.Lock();

        var result = await _sut.UnlockAsync(password);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task UnlockAsync_ReturnsFalse_WithWrongPassword()
    {
        await _sut.SetupVaultAsync("CorrectPassword99!");
        _sut.Lock();

        var result = await _sut.UnlockAsync("WrongPassword!");
        result.Should().BeFalse();
    }

    [Fact]
    public async Task UnlockAsync_KeyInMemory_AfterSuccessfulUnlock()
    {
        const string password = "CorrectPassword99!";
        await _sut.SetupVaultAsync(password);
        _sut.Lock();

        await _sut.UnlockAsync(password);

        _sut.IsUnlocked.Should().BeTrue();
        _sut.GetKey().Should().HaveCount(32);
    }

    [Fact]
    public async Task UnlockAsync_VaultRemainsLocked_AfterWrongPassword()
    {
        await _sut.SetupVaultAsync("CorrectPassword99!");
        _sut.Lock();

        await _sut.UnlockAsync("WrongPassword!");

        _sut.IsUnlocked.Should().BeFalse();
    }

    [Fact]
    public async Task UnlockAsync_DerivesSameKey_AcrossMultipleUnlocks()
    {
        // Same password + same stored salt must always give the same key
        const string password = "StableKey99!";
        await _sut.SetupVaultAsync(password);

        var key1 = _sut.GetKey().ToArray(); // copy before lock clears it
        _sut.Lock();

        await _sut.UnlockAsync(password);
        var key2 = _sut.GetKey().ToArray();

        key1.Should().Equal(key2,
            because: "Argon2id is deterministic — same password + salt = same key");
    }

    // --- Lock ---

    [Fact]
    public async Task Lock_ClearsKeyFromMemory()
    {
        await _sut.SetupVaultAsync("MyPassword123!");
        _sut.IsUnlocked.Should().BeTrue();

        _sut.Lock();

        _sut.IsUnlocked.Should().BeFalse();
    }

    [Fact]
    public async Task Lock_GetKey_ThrowsWhenLocked()
    {
        await _sut.SetupVaultAsync("MyPassword123!");
        _sut.Lock();

        var act = () => _sut.GetKey();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task Lock_IsIdempotent()
    {
        await _sut.SetupVaultAsync("MyPassword123!");

        // Calling Lock twice should not throw
        var act = () =>
        {
            _sut.Lock();
            _sut.Lock();
        };
        act.Should().NotThrow();
    }

    [Fact]
    public async Task Lock_RaisesVaultLockedEvent()
    {
        await _sut.SetupVaultAsync("MyPassword123!");

        var eventRaised = false;
        _sut.VaultLocked += (_, _) => eventRaised = true;

        _sut.Lock();

        eventRaised.Should().BeTrue(
            because: "VaultLocked event must fire so App can navigate to UnlockView");
    }

    // --- Edge cases ---

    [Fact]
    public void Lock_BeforeSetup_DoesNotThrow()
    {
        // Locking a vault that was never set up should be a safe no-op
        var act = () => _sut.Lock();
        act.Should().NotThrow();
    }

    [Fact]
    public async Task GetKey_ReturnsNewReferenceOnEachUnlock()
    {
        // The key returned should be a fresh array (not the same internal reference)
        // so callers can't accidentally zero it out
        const string password = "MyPassword123!";
        await _sut.SetupVaultAsync(password);
        _sut.Lock();

        await _sut.UnlockAsync(password);
        var key = _sut.GetKey();
        key.Should().HaveCount(32);
    }
}
