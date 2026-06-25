using System.Security.Cryptography;
using System.Text;
using System.Timers;
using Arcane.Core.Data;
using Arcane.Core.Encryption;
using Arcane.Core.Helpers;
using Arcane.Core.Models.Entities;
using Arcane.Core.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Arcane.Core.Services.Implementations;

public sealed class VaultService(
    IDbContextFactory<ArcaneDbContext> dbFactory,
    IEncryptionService encryption,
    IKeyDerivationService keyDerivation)
    : IVaultService
{
    private const string VerificationPlaintext = "ARCANE_VAULT_OK";

    private byte[]? _masterKey;
    private System.Timers.Timer? _autoLockTimer;
    private int _autoLockMinutes = 10;

    public event EventHandler? VaultLocked;

    public bool IsUnlocked => _masterKey is not null;

    public bool VaultExists()
    {
        using var db = dbFactory.CreateDbContext();
        return db.VaultProfiles.Any();
    }

    public async Task SetupVaultAsync(string masterPassword)
    {
        PathHelper.SecureDirectoryPermissions();

        var salt = new byte[16];
        RandomNumberGenerator.Fill(salt);

        var key = await Task.Run(() =>
            keyDerivation.DeriveKey(masterPassword, salt));

        var verificationBytes = Encoding.UTF8.GetBytes(VerificationPlaintext);
        var (payload, nonce) = encryption.Encrypt(verificationBytes, key);

        var profile = new VaultProfile
        {
            Salt                    = salt,
            VerificationCiphertext  = payload,
            VerificationNonce       = nonce,
        };

        using var db = dbFactory.CreateDbContext();
        db.VaultProfiles.Add(profile);
        await db.SaveChangesAsync();

        _masterKey        = key;
        _autoLockMinutes  = profile.AutoLockMinutes;
        StartAutoLockTimer();
    }

    public async Task<bool> UnlockAsync(string masterPassword)
    {
        using var db = dbFactory.CreateDbContext();
        var profile = await db.VaultProfiles.FirstOrDefaultAsync();
        if (profile is null) return false;

        try
        {
            var candidateKey = await Task.Run(() =>
                keyDerivation.DeriveKey(
                    masterPassword,
                    profile.Salt,
                    profile.Argon2Iterations,
                    profile.Argon2MemoryKiB,
                    profile.Argon2Parallelism));

            var decrypted = encryption.Decrypt(
                profile.VerificationCiphertext,
                profile.VerificationNonce,
                candidateKey);

            var decryptedText = Encoding.UTF8.GetString(decrypted);

            if (decryptedText != VerificationPlaintext)
            {
                // Decryption "succeeded" but string doesn't match — extremely rare edge case
                CryptographicOperations.ZeroMemory(candidateKey);
                return false;
            }

            // Password correct — store key, start timer, update last-unlocked timestamp
            _masterKey       = candidateKey;
            _autoLockMinutes = profile.AutoLockMinutes;

            profile.LastUnlockedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            StartAutoLockTimer();
            return true;
        }
        catch (AuthenticationTagMismatchException)
        {
            // AES-GCM tag verification failed — wrong password. This is the normal wrong-password path.
            return false;
        }
    }

    public void Lock()
    {
        StopAutoLockTimer();

        if (_masterKey is not null)
        {
            CryptographicOperations.ZeroMemory(_masterKey);
            _masterKey = null;
        }

        VaultLocked?.Invoke(this, EventArgs.Empty);
    }

    public byte[] GetKey() =>
        _masterKey ?? throw new InvalidOperationException(
            "Vault is locked. Call UnlockAsync before accessing the key.");

    public void ResetInactivityTimer()
    {
        if (!IsUnlocked || _autoLockTimer is null) return;
        _autoLockTimer.Stop();
        _autoLockTimer.Start();
    }

    // --- Private helpers ---

    private void StartAutoLockTimer()
    {
        StopAutoLockTimer();

        _autoLockTimer = new System.Timers.Timer(_autoLockMinutes * 60_000)
        {
            AutoReset = false // Fire once, not repeatedly
        };
        _autoLockTimer.Elapsed += OnAutoLockElapsed;
        _autoLockTimer.Start();
    }

    private void StopAutoLockTimer()
    {
        if (_autoLockTimer is null) return;
        _autoLockTimer.Elapsed -= OnAutoLockElapsed;
        _autoLockTimer.Stop();
        _autoLockTimer.Dispose();
        _autoLockTimer = null;
    }

    private void OnAutoLockElapsed(object? sender, ElapsedEventArgs e) => Lock();
}
