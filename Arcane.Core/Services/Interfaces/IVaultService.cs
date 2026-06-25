namespace Arcane.Core.Services.Interfaces;

public interface IVaultService
{
    /// <summary>Raised on lock (manual or auto-lock timeout). Fires on a ThreadPool thread.</summary>
    event EventHandler? VaultLocked;

    bool IsUnlocked { get; }
    bool VaultExists();
    Task SetupVaultAsync(string masterPassword);
    Task<bool> UnlockAsync(string masterPassword);
    void Lock();

    /// <summary>Returns the in-memory master key. Throws InvalidOperationException if locked.</summary>
    byte[] GetKey();

    /// <summary>Resets the inactivity auto-lock timer. Call on any user interaction.</summary>
    void ResetInactivityTimer();
}
