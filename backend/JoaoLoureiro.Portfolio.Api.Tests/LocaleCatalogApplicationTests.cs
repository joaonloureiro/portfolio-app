using JoaoLoureiro.Portfolio.Core.Locales;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JoaoLoureiro.Portfolio.Api.Tests;

public sealed class LocaleCatalogApplicationTests(TestWebApplicationFactory factory)
    : IClassFixture<TestWebApplicationFactory>
{
    [Fact]
    public async Task BuiltInBootstrapIsIdempotentAndDoesNotReplaceAnExistingRevision()
    {
        LocaleCatalogRevision first;
        await using (var firstScope = factory.Services.CreateAsyncScope())
        {
            var service = firstScope.ServiceProvider.GetRequiredService<LocaleCatalogApplicationService>();
            first = (await service.GetAdminAsync(CancellationToken.None)).Catalog!;
        }

        Assert.NotEmpty(first.Entries);

        await using (var updateScope = factory.Services.CreateAsyncScope())
        {
            var service = updateScope.ServiceProvider.GetRequiredService<LocaleCatalogApplicationService>();
            var result = await service.ReplaceAsync(
                new ReplaceLocaleCatalogCommand(
                    first.Revision,
                    new Dictionary<string, LocaleCatalogChange>
                    {
                        ["hero.title"] = new(LocaleCatalogValue.FromString("Persisted"), LocaleCatalogValue.FromString("Persistido"))
                    }),
                CancellationToken.None);
            Assert.Equal(LocaleCatalogOperationOutcome.Succeeded, result.Outcome);
        }

        await using var secondScope = factory.Services.CreateAsyncScope();
        var second = (await secondScope.ServiceProvider.GetRequiredService<LocaleCatalogApplicationService>().GetAdminAsync(CancellationToken.None)).Catalog!;
        Assert.Equal(first.Revision + 1, second.Revision);
        Assert.Equal("Persisted", second.Entries.Single(entry => entry.Key == "hero.title").English.Scalar);
    }
}
