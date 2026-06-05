namespace Arcane.Core.Services.Interfaces;

public interface IVaultService
{
    bool VaultExists();
    Task SetupVaultAsync(string masterPassword);
    Task<bool> UnlockAsync(string masterPassword);
    void Lock();
    bool IsUnlocked { get; }
    byte[] GetKey();
}
