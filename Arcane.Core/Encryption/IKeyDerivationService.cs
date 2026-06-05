namespace Arcane.Core.Encryption;

public interface IKeyDerivationService
{
    /// <summary>
    /// Derives a 32-byte key from the password and salt using Argon2id.
    /// This is intentionally slow — it takes ~0.5–2 seconds depending on parameters.
    /// That is correct behaviour: it makes brute-force attacks impractical.
    /// </summary>
    /// <param name="password">The master password entered by the user.</param>
    /// <param name="salt">16-byte random salt (generated once at vault setup, stored in VaultProfile).</param>
    /// <param name="iterations">Argon2id time cost (default 4).</param>
    /// <param name="memoryKiB">Memory cost in KiB (default 65536 = 64MB).</param>
    /// <param name="parallelism">Degree of parallelism (default 2).</param>
    /// <returns>32-byte derived key. NEVER write this to disk.</returns>
    byte[] DeriveKey(string password, byte[] salt,
                     int iterations  = 4,
                     int memoryKiB   = 65536,
                     int parallelism = 2);
}
