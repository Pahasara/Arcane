using System.Security.Cryptography;
using Arcane.Core.Encryption;
using AwesomeAssertions;

namespace Arcane.Tests;

public sealed class KeyDerivationTests
{
    private readonly IKeyDerivationService _sut = new KeyDerivationService();

    private static byte[] GenerateSalt() 
    {
        var salt = new byte[16];
        RandomNumberGenerator.Fill(salt);
        return salt;
    }

    [Fact]
    public void DeriveKey_Returns32Bytes()
    {
        var salt = GenerateSalt();
        var key = _sut.DeriveKey("TestPassword123!", salt);
        key.Should().HaveCount(32, because: "AES-256 requires a 32-byte key");
    }

    [Fact]
    public void DeriveKey_SamePasswordAndSalt_ProducesIdenticalKey()
    {
        // This is the unlock flow — same password + stored salt must always give same key
        var salt = GenerateSalt();
        var key1 = _sut.DeriveKey("MyPassword!", salt);
        var key2 = _sut.DeriveKey("MyPassword!", salt);

        key1.Should().Equal(key2,
            because: "Argon2id is deterministic — same inputs must always produce same key");
    }

    [Fact]
    public void DeriveKey_DifferentPassword_ProducesDifferentKey()
    {
        var salt = GenerateSalt();
        var key1 = _sut.DeriveKey("CorrectPassword!", salt);
        var key2 = _sut.DeriveKey("WrongPassword!",  salt);

        key1.Should().NotEqual(key2,
            because: "different passwords must produce completely different keys");
    }

    [Fact]
    public void DeriveKey_SamePassword_DifferentSalt_ProducesDifferentKey()
    {
        var salt1 = GenerateSalt();
        var salt2 = GenerateSalt();

        var key1 = _sut.DeriveKey("SamePassword!", salt1);
        var key2 = _sut.DeriveKey("SamePassword!", salt2);

        key1.Should().NotEqual(key2,
            because: "different salts must produce different keys even with same password");
    }

    [Fact]
    public void DeriveKey_WithShortSalt_ThrowsArgumentException()
    {
        var shortSalt = new byte[8]; // Less than the required 16 bytes
        var act = () => _sut.DeriveKey("Password", shortSalt);
        act.Should().Throw<ArgumentException>(because: "salt must be at least 16 bytes");
    }

    [Fact]
    public void DeriveKey_WithEmptyPassword_ThrowsArgumentException()
    {
        var salt = GenerateSalt();
        var act = () => _sut.DeriveKey("", salt);
        act.Should().Throw<ArgumentException>(because: "empty password is not allowed");
    }
}
