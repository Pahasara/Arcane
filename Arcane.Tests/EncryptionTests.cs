using System.Security.Cryptography;
using System.Text;
using Arcane.Core.Encryption;
using AwesomeAssertions;

namespace Arcane.Tests;

public sealed class EncryptionTests
{
    private readonly IEncryptionService _sut = new AesEncryptionService();

    // A fixed 32-byte key for testing
    private static readonly byte[] TestKey = Encoding.UTF8.GetBytes("ArcaneTestKey32BytesLongExactly!"); // 32 chars

    [Fact]
    public void Encrypt_ThenDecrypt_ReturnsOriginalPlaintext()
    {
        // Arrange
        var original = "My secret diary entry 🌙"u8.ToArray();

        // Act
        var (payload, nonce) = _sut.Encrypt(original, TestKey);
        var decrypted = _sut.Decrypt(payload, nonce, TestKey);

        // Assert
        decrypted.Should().Equal(original);
    }

    [Fact]
    public void Encrypt_ThenDecrypt_WorksForEmptyContent()
    {
        var original = Array.Empty<byte>();
        var (payload, nonce) = _sut.Encrypt(original, TestKey);
        var decrypted = _sut.Decrypt(payload, nonce, TestKey);
        decrypted.Should().Equal(original);
    }

    [Fact]
    public void Encrypt_ThenDecrypt_WorksForLargeContent()
    {
        // Simulate a 10KB entry
        var original = Encoding.UTF8.GetBytes(new string('A', 10_000));
        var (payload, nonce) = _sut.Encrypt(original, TestKey);
        var decrypted = _sut.Decrypt(payload, nonce, TestKey);
        decrypted.Should().Equal(original);
    }

    [Fact]
    public void Decrypt_WithWrongKey_ThrowsAuthenticationTagMismatchException()
    {
        // Arrange
        var original = "Secret content"u8.ToArray();
        var (payload, nonce) = _sut.Encrypt(original, TestKey);

        var wrongKey = Encoding.UTF8.GetBytes("WrongKey32BytesLongExactlyHere!!");

        // Act & Assert
        var act = () => _sut.Decrypt(payload, nonce, wrongKey);
        act.Should().Throw<AuthenticationTagMismatchException>(
            because: "wrong key must cause GCM tag verification to fail");
    }

    [Fact]
    public void Decrypt_WithTamperedPayload_ThrowsAuthenticationTagMismatchException()
    {
        // Arrange
        var original = "Tamper test"u8.ToArray();
        var (payload, nonce) = _sut.Encrypt(original, TestKey);

        // Tamper with a byte in the middle of the payload
        var tampered = (byte[])payload.Clone();
        tampered[payload.Length / 2] ^= 0xFF;

        // Act & Assert
        var act = () => _sut.Decrypt(tampered, nonce, TestKey);
        act.Should().Throw<AuthenticationTagMismatchException>(
            because: "modified ciphertext must fail GCM authentication");
    }

    [Fact]
    public void Encrypt_CalledTwice_ProducesDifferentNonces()
    {
        // Nonces must NEVER be reused — each call must produce a fresh one
        var data = "Some content"u8.ToArray();

        var (_, nonce1) = _sut.Encrypt(data, TestKey);
        var (_, nonce2) = _sut.Encrypt(data, TestKey);

        nonce1.Should().NotEqual(nonce2,
            because: "each encryption call must generate a cryptographically fresh nonce");
    }

    [Fact]
    public void Encrypt_CalledTwice_SamePlaintext_ProducesDifferentPayloads()
    {
        // Same plaintext + same key but different nonces must produce different ciphertext
        var data = "Same diary entry"u8.ToArray();

        var (payload1, _) = _sut.Encrypt(data, TestKey);
        var (payload2, _) = _sut.Encrypt(data, TestKey);

        payload1.Should().NotEqual(payload2,
            because: "semantic security: same plaintext must not produce same ciphertext");
    }

    [Fact]
    public void Decrypt_WithTruncatedPayload_ThrowsArgumentException()
    {
        var act = () => _sut.Decrypt(new byte[5], new byte[12], TestKey);
        act.Should().Throw<ArgumentException>(
            because: "payload shorter than the 16-byte tag is invalid input");
    }
}
