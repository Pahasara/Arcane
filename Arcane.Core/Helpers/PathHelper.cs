namespace Arcane.Core.Helpers;

/// <summary>
/// Resolves all cross-platform file paths for the app.
/// Use these everywhere — never hardcode paths.
/// Directories are created on first access.
/// </summary>
public static class PathHelper
{
    /// <summary>
    /// Root app data directory.
    /// Linux:   ~/.config/Arcane  (XDG_CONFIG_HOME / ApplicationData)
    /// Windows: %APPDATA%\Arcane
    /// Android: handled by Avalonia's platform layer
    /// </summary>
    public static string AppDataDir
    {
        get
        {
            if (field is not null) return field;

            field = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Arcane");

            Directory.CreateDirectory(field);
            return field;
        }
    }

    public static string DatabasePath => Path.Combine(AppDataDir, "arcane.db");

    /// <summary>
    /// Directory for externally stored encrypted attachment files (>1MB).
    /// Created on first access.
    /// </summary>
    public static string AttachmentsDir
    {
        get
        {
            var dir = Path.Combine(AppDataDir, "attachments");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    /// <summary>
    /// Set restrictive permissions on the app data directory (Linux only).
    /// Call this once on startup.
    /// </summary>
    public static void SecureDirectoryPermissions()
    {
        if (!OperatingSystem.IsLinux()) return;

        try
        {
            File.SetUnixFileMode(AppDataDir,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
        catch
        {
            // Non-fatal — permissions couldn't be set (e.g. on a mounted filesystem)
        }
    }
}
