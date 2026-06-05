namespace Arcane.Core.Encryption;

public interface IEncryptionService
{
    /// <summary>
    /// Encrypts plaintext with AES-256-GCM.
    /// Returns (payload = tag + ciphertext, nonce).
    /// </summary>
    (byte[] Payload, byte[] Nonce) Encrypt(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> masterKey);

    /// <summary>
    /// Decrypts a payload (tag + ciphertext) using the nonce and master key.
    /// Throws <see cref="System.Security.Cryptography.AuthenticationTagMismatchException"/>
    /// if the key is wrong or the data has been tampered with.
    /// </summary>
    byte[] Decrypt(ReadOnlySpan<byte> payload, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> masterKey);
}
