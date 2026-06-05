using System.Security.Cryptography;
using AwesomeAssertions;

namespace Arcane.Tests;

public sealed class MemorySafetyTests
{
    [Fact]
    public void ZeroMemory_ClearsKeyBytesToZero()
    {
        // Arrange — a byte array with known non-zero content
        var key = new byte[] { 0x01, 0x02, 0x03, 0xAB, 0xCD, 0xEF, 0xFF, 0x10 };
        key.Should().NotContain(0, because: "starts with non-zero values");

        // Act — this is what VaultService.Lock() does
        CryptographicOperations.ZeroMemory(key);

        // Assert
        key.Should().AllBeEquivalentTo(0,
            because: "ZeroMemory must overwrite all bytes before GC can reclaim the memory");
    }

    [Fact]
    public void ZeroMemory_OnNullSpan_DoesNotThrow()
    {
        // Span of length 0 should be a no-op, not a crash
        var emptyKey = Array.Empty<byte>();
        var act = () => CryptographicOperations.ZeroMemory(emptyKey);
        act.Should().NotThrow();
    }
}
