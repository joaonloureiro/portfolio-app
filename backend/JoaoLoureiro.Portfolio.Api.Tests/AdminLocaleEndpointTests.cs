using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using JoaoLoureiro.Portfolio.Api.Dtos;
using JoaoLoureiro.Portfolio.Core.Locales;
using JoaoLoureiro.Portfolio.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JoaoLoureiro.Portfolio.Api.Tests;

public sealed class AdminLocaleEndpointTests(TestWebApplicationFactory factory)
    : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false
    });

    [Theory]
    [InlineData("GET")]
    [InlineData("PUT")]
    public async Task LocaleAdminOperationsRequireAdministratorJwt(string method)
    {
        using var missing = await SendProbeAsync(method, "missing");
        using var invalid = await SendProbeAsync(method, "invalid");
        using var wrongSubject = await SendProbeAsync(method, "wrong-subject");

        Assert.Equal(HttpStatusCode.Unauthorized, missing.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, invalid.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, wrongSubject.StatusCode);
    }

    [Fact]
    public async Task BuiltInCatalogIsSeededAndPublicReadsRemainAnonymous()
    {
        using var adminResponse = await SendAsync(HttpMethod.Get, "/admin/api/locales");

        Assert.Equal(HttpStatusCode.OK, adminResponse.StatusCode);
        var admin = await adminResponse.Content.ReadFromJsonAsync<AdminLocaleCatalogResponse>();
        Assert.NotNull(admin);
        Assert.Equal(LocaleCatalogContract.Definitions.Count, admin!.Entries.Count);
        Assert.Equal("metadata.home_title", admin.Entries[0].Key);
        Assert.True(admin.Revision >= 1);

        using var publicResponse = await client.GetAsync("/api/locales/en");

        Assert.Equal(HttpStatusCode.OK, publicResponse.StatusCode);
        Assert.Contains("public", publicResponse.Headers.CacheControl?.ToString());
        Assert.NotNull(publicResponse.Headers.ETag);
        using var document = JsonDocument.Parse(await publicResponse.Content.ReadAsStreamAsync());
        Assert.Equal("João Loureiro | Full Stack Developer", document.RootElement.GetProperty("messages").GetProperty("metadata").GetProperty("home_title").GetString());
        Assert.Equal(3, document.RootElement.GetProperty("messages").GetProperty("about").GetProperty("principles").GetArrayLength());
    }

    [Fact]
    public async Task ExistingCatalogExposesOfferDefaultsAndPersistsAdminEdits()
    {
        using var isolatedFactory = new TestWebApplicationFactory();
        using var isolatedClient = isolatedFactory.CreateClient();
        using var initial = await isolatedClient.GetAsync("/api/locales/en");
        initial.EnsureSuccessStatusCode();
        using (var scope = isolatedFactory.Services.CreateScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
            await database.LocaleCatalogEntries
                .Where(entry => entry.Key.StartsWith("engagement."))
                .ExecuteDeleteAsync();
        }

        using var legacy = await isolatedClient.GetAsync("/api/locales/en");
        legacy.EnsureSuccessStatusCode();
        using var legacyJson = JsonDocument.Parse(await legacy.Content.ReadAsStreamAsync());
        var revision = legacyJson.RootElement.GetProperty("revision").GetInt64();
        Assert.Equal("$150", legacyJson.RootElement.GetProperty("messages").GetProperty("engagement").GetProperty("task_price").GetString());

        using var update = new HttpRequestMessage(HttpMethod.Put, "/admin/api/locales");
        update.Headers.Authorization = new("Bearer", isolatedFactory.CreateToken(TestWebApplicationFactory.AdministratorSubject));
        update.Content = JsonContent.Create(new
        {
            expectedRevision = revision,
            changes = new Dictionary<string, object> { ["engagement.task_price"] = new { en = "$175", pt = "$175" } }
        });
        using var updated = await isolatedClient.SendAsync(update);
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);

        using var saved = await isolatedClient.GetAsync("/api/locales/pt");
        saved.EnsureSuccessStatusCode();
        using var savedJson = JsonDocument.Parse(await saved.Content.ReadAsStreamAsync());
        Assert.Equal("$175", savedJson.RootElement.GetProperty("messages").GetProperty("engagement").GetProperty("task_price").GetString());
        Assert.Equal("Contrato mensal", savedJson.RootElement.GetProperty("messages").GetProperty("engagement").GetProperty("monthly").GetString());
    }

    [Fact]
    public async Task BilingualChangesPublishAtomicallyAndPublicReadsExposeBothLocales()
    {
        using var currentResponse = await SendAsync(HttpMethod.Get, "/admin/api/locales");
        var current = await currentResponse.Content.ReadFromJsonAsync<AdminLocaleCatalogResponse>();
        Assert.NotNull(current);

        using var updateResponse = await SendAsync(
            HttpMethod.Put,
            "/admin/api/locales",
            new
            {
                expectedRevision = current!.Revision,
                changes = new Dictionary<string, object>
                {
                    ["hero.title"] = new { en = "Managed English title", pt = "Título gerenciado" },
                    ["atlas.interfaces.points"] = new { en = new[] { "One", "Two" }, pt = new[] { "Um", "Dois" } }
                }
            });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<AdminLocaleCatalogResponse>();
        Assert.Equal(current.Revision + 1, updated!.Revision);

        using var englishResponse = await client.GetAsync("/api/locales/en");
        using var english = JsonDocument.Parse(await englishResponse.Content.ReadAsStreamAsync());
        Assert.Equal("Managed English title", english.RootElement.GetProperty("messages").GetProperty("hero").GetProperty("title").GetString());
        Assert.Equal(2, english.RootElement.GetProperty("messages").GetProperty("atlas").GetProperty("interfaces").GetProperty("points").GetArrayLength());

        using var portugueseResponse = await client.GetAsync("/api/locales/pt");
        using var portuguese = JsonDocument.Parse(await portugueseResponse.Content.ReadAsStreamAsync());
        Assert.Equal("Título gerenciado", portuguese.RootElement.GetProperty("messages").GetProperty("hero").GetProperty("title").GetString());
        Assert.Equal("Um", portuguese.RootElement.GetProperty("messages").GetProperty("atlas").GetProperty("interfaces").GetProperty("points")[0].GetString());
    }

    [Fact]
    public async Task StaleRevisionIsRejectedWithoutLosingTheNewerCatalog()
    {
        using var currentResponse = await SendAsync(HttpMethod.Get, "/admin/api/locales");
        var current = await currentResponse.Content.ReadFromJsonAsync<AdminLocaleCatalogResponse>();
        Assert.NotNull(current);

        using var firstUpdate = await UpdateScalarAsync(current!.Revision, "hero.role", "First update", "Primeira atualização");
        Assert.Equal(HttpStatusCode.OK, firstUpdate.StatusCode);

        using var staleUpdate = await UpdateScalarAsync(current.Revision, "hero.role", "Stale update", "Atualização antiga");
        Assert.Equal(HttpStatusCode.Conflict, staleUpdate.StatusCode);
        Assert.Equal("locale_stale_revision", await ReadErrorKey(staleUpdate));

        using var publicResponse = await client.GetAsync("/api/locales/en");
        using var document = JsonDocument.Parse(await publicResponse.Content.ReadAsStreamAsync());
        Assert.Equal("First update", document.RootElement.GetProperty("messages").GetProperty("hero").GetProperty("role").GetString());
    }

    [Fact]
    public async Task InvalidLocaleShapesKeysTypesAndLimitsAreRejected()
    {
        using var currentResponse = await SendAsync(HttpMethod.Get, "/admin/api/locales");
        var current = await currentResponse.Content.ReadFromJsonAsync<AdminLocaleCatalogResponse>();
        Assert.NotNull(current);

        using var unknownKey = await SendAsync(HttpMethod.Put, "/admin/api/locales", new
        {
            expectedRevision = current!.Revision,
            changes = new Dictionary<string, object> { ["hero.not_editable"] = new { en = "x", pt = "y" } }
        });
        Assert.Equal(HttpStatusCode.BadRequest, unknownKey.StatusCode);
        Assert.Equal("locale_unknown_key", await ReadErrorKey(unknownKey));

        using var missingCounterpart = await SendAsync(HttpMethod.Put, "/admin/api/locales", new
        {
            expectedRevision = current.Revision,
            changes = new Dictionary<string, object> { ["hero.title"] = new { en = "x" } }
        });
        Assert.Equal(HttpStatusCode.BadRequest, missingCounterpart.StatusCode);
        Assert.Equal("locale_invalid_shape", await ReadErrorKey(missingCounterpart));

        using var wrongType = await SendAsync(HttpMethod.Put, "/admin/api/locales", new
        {
            expectedRevision = current.Revision,
            changes = new Dictionary<string, object> { ["hero.title"] = new { en = new[] { "x" }, pt = "y" } }
        });
        Assert.Equal(HttpStatusCode.BadRequest, wrongType.StatusCode);
        Assert.Equal("locale_invalid_shape", await ReadErrorKey(wrongType));

        using var excessiveValue = await SendAsync(HttpMethod.Put, "/admin/api/locales", new
        {
            expectedRevision = current.Revision,
            changes = new Dictionary<string, object> { ["hero.title"] = new { en = new string('x', LocaleCatalogLimits.MaxScalarLength + 1), pt = "y" } }
        });
        Assert.Equal(HttpStatusCode.BadRequest, excessiveValue.StatusCode);
        Assert.Equal("locale_invalid_value", await ReadErrorKey(excessiveValue));

        using var markupValue = await SendAsync(HttpMethod.Put, "/admin/api/locales", new
        {
            expectedRevision = current.Revision,
            changes = new Dictionary<string, object> { ["hero.title"] = new { en = "<script>alert(1)</script>", pt = "Título" } }
        });
        Assert.Equal(HttpStatusCode.BadRequest, markupValue.StatusCode);
        Assert.Equal("locale_invalid_value", await ReadErrorKey(markupValue));
    }

    [Fact]
    public async Task UnsupportedPublicLocaleReturnsNotFound()
    {
        using var response = await client.GetAsync("/api/locales/fr");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("locale_unsupported", await ReadErrorKey(response));
    }

    private async Task<HttpResponseMessage> UpdateScalarAsync(long revision, string key, string english, string portuguese)
    {
        return await SendAsync(HttpMethod.Put, "/admin/api/locales", new
        {
            expectedRevision = revision,
            changes = new Dictionary<string, object> { [key] = new { en = english, pt = portuguese } }
        });
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string uri, object? body = null)
    {
        using var request = new HttpRequestMessage(method, uri);
        request.Headers.TryAddWithoutValidation(
            "Authorization",
            $"Bearer {factory.CreateToken(TestWebApplicationFactory.AdministratorSubject)}");
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> SendProbeAsync(string method, string tokenKind)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), "/admin/api/locales")
        {
            Content = JsonContent.Create(new
            {
                expectedRevision = 1,
                changes = new Dictionary<string, object> { ["hero.title"] = new { en = "x", pt = "y" } }
            })
        };

        if (tokenKind == "invalid")
        {
            request.Headers.TryAddWithoutValidation("Authorization", "Bearer not-a-jwt");
        }
        else if (tokenKind == "wrong-subject")
        {
            request.Headers.TryAddWithoutValidation(
                "Authorization",
                $"Bearer {factory.CreateToken("different-subject")}");
        }

        return await client.SendAsync(request);
    }

    private static async Task<string?> ReadErrorKey(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        return document.RootElement.TryGetProperty("errorKey", out var value)
            ? value.GetString()
            : null;
    }
}
