using System.Text;
using Konscious.Security.Cryptography;

namespace Arcane.Core.Encryption;

public sealed class KeyDerivationService : IKeyDerivationService
{
    private const int KeyLengthBytes = 32; // 256 bits

    /// <inheritdoc />
    public byte[] DeriveKey(string password, byte[] salt,
                            int iterations  = 4,
                            int memoryKiB   = 65536,
                            int parallelism = 2)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);
        ArgumentNullException.ThrowIfNull(salt);
        if (salt.Length < 16)
            throw new ArgumentException("Salt must be at least 16 bytes.", nameof(salt));

        var passwordBytes = Encoding.UTF8.GetBytes(password);

        using var argon2 = new Argon2id(passwordBytes)
        {
            Salt        = salt,
            Iterations  = iterations,
            MemorySize  = memoryKiB,
            DegreeOfParallelism = parallelism
        };

        return argon2.GetBytes(KeyLengthBytes);
    }
}
