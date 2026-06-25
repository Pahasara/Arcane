using Arcane.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace Arcane.Tests.Helpers;

public sealed class TestDbContextFactory(string dbPath) : IDbContextFactory<ArcaneDbContext>
{
    private readonly DbContextOptions<ArcaneDbContext> _options = new DbContextOptionsBuilder<ArcaneDbContext>()
        .UseSqlite($"Data Source={dbPath}")
        .Options;

    public ArcaneDbContext CreateDbContext() => new(_options);
}
