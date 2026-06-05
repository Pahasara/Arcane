using Arcane.Core.Helpers;
using AwesomeAssertions;

namespace Arcane.Tests;

public sealed class PathHelperTests
{
    [Fact]
    public void AppDataDir_ContainsArcaneName()
    {
        PathHelper.AppDataDir.Should().Contain("Arcane",
            because: "app data must be isolated to the Arcane directory");
    }

    [Fact]
    public void DatabasePath_EndsWithArcaneDb()
    {
        PathHelper.DatabasePath.Should().EndWith("arcane.db");
    }

    [Fact]
    public void AttachmentsDir_IsInsideAppDataDir()
    {
        PathHelper.AttachmentsDir.Should().StartWith(PathHelper.AppDataDir);
    }

    [Fact]
    public void AppDataDir_IsCreatedOnAccess()
    {
        // Accessing the property should create the directory
        var dir = PathHelper.AppDataDir;
        Directory.Exists(dir).Should().BeTrue(
            because: "PathHelper must create the directory on first access");
    }
}
