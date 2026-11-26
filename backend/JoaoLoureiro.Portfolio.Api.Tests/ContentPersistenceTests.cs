using JoaoLoureiro.Portfolio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JoaoLoureiro.Portfolio.Api.Tests;

public sealed class ContentPersistenceTests(TestWebApplicationFactory factory)
    : IClassFixture<TestWebApplicationFactory>
{
    [Fact]
    public async Task TestingEnvironment_UsesAnIsolatedSqliteStore()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();

        Assert.True(await context.Database.CanConnectAsync());
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        Assert.Empty(await context.Posts.ToListAsync());
    }
}
