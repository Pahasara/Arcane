using Arcane.Core.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Arcane.Core.Data;

/// <summary>
/// Design-time factory used by the EF Core CLI (dotnet ef migrations add ...).
/// This is only ever used by tooling — not at runtime.
/// </summary>
public sealed class ArcaneDbContextFactory : IDesignTimeDbContextFactory<ArcaneDbContext>
{
    public ArcaneDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ArcaneDbContext>()
            .UseSqlite($"Data Source={PathHelper.DatabasePath}")
            .Options;

        return new ArcaneDbContext(options);
    }
}
