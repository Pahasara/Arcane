namespace Arcane.Core.Models.Entities;

public sealed class VaultProfile : BaseEntity
{
    public required byte[] Salt { get; set; }

    public int Argon2Iterations  { get; set; } = 4;
    public int Argon2MemoryKiB   { get; set; } = 65536; // 64MB
    public int Argon2Parallelism { get; set; } = 2;

    /// <summary>
    /// AES-256-GCM encryption of the known plaintext "ARCANE_VAULT_OK".
    /// Used to verify the master password on unlock without storing the key.
    /// </summary>
    public required byte[] VerificationCiphertext { get; set; }
    public required byte[] VerificationNonce      { get; set; }

    public DateTime? LastUnlockedAt  { get; set; }
    public int    AutoLockMinutes    { get; set; } = 10;
    public string ThemePreference    { get; set; } = "System"; // "Dark" | "Light" | "System"
    public int    EditorFontSize     { get; set; } = 16;
}
