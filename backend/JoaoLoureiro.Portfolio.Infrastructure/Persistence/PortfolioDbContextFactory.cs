using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace JoaoLoureiro.Portfolio.Infrastructure.Persistence;

public sealed class PortfolioDbContextFactory : IDesignTimeDbContextFactory<PortfolioDbContext>
{
    public PortfolioDbContext CreateDbContext(string[] args)
    {
        var configuredPath = Environment.GetEnvironmentVariable("ContentStore__Path")
            ?? System.IO.Path.Combine("data", "portfolio.db");

        var options = new DbContextOptionsBuilder<PortfolioDbContext>()
            .UseSqlite(
                ContentStorePath.CreateConnectionString(configuredPath),
                sqlite => sqlite.MigrationsAssembly(typeof(PortfolioDbContext).Assembly.FullName))
            .Options;

        return new PortfolioDbContext(options);
    }
}
