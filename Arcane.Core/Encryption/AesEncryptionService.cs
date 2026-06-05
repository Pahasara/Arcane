using System.Security.Cryptography;

namespace Arcane.Core.Encryption;

public sealed class AesEncryptionService : IEncryptionService
{
    // AES-GCM nonce size is always 12 bytes (96 bits) per NIST recommendation
    private static readonly int NonceSize = AesGcm.NonceByteSizes.MaxSize; // 12
    // GCM authentication tag: 16 bytes (128 bits) — maximum strength
    private static readonly int TagSize   = AesGcm.TagByteSizes.MaxSize;   // 16

    /// <inheritdoc />
    public (byte[] Payload, byte[] Nonce) Encrypt(
        ReadOnlySpan<byte> plaintext,
        ReadOnlySpan<byte> masterKey)
    {
        // Fresh nonce for every single encryption call — NEVER reuse a nonce
        var nonce = new byte[NonceSize];
        RandomNumberGenerator.Fill(nonce);

        var ciphertext = new byte[plaintext.Length];
        var tag        = new byte[TagSize];

        using var aes = new AesGcm(masterKey, TagSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        // Concatenate tag + ciphertext into one payload for clean DB storage
        // Layout: [tag: 16 bytes][ciphertext: N bytes]
        var payload = new byte[TagSize + ciphertext.Length];
        tag.CopyTo(payload, 0);
        ciphertext.CopyTo(payload, TagSize);

        return (payload, nonce);
    }

    /// <inheritdoc />
    public byte[] Decrypt(
        ReadOnlySpan<byte> payload,
        ReadOnlySpan<byte> nonce,
        ReadOnlySpan<byte> masterKey)
    {
        if (payload.Length < TagSize)
            throw new ArgumentException(
                $"Payload too short ({payload.Length} bytes). Expected at least {TagSize} bytes.", nameof(payload));

        // Split the payload back into tag + ciphertext
        var tag        = payload[..TagSize];
        var ciphertext = payload[TagSize..];

        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(masterKey, TagSize);
        // Throws AuthenticationTagMismatchException if key is wrong or data tampered
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return plaintext;
    }
}
