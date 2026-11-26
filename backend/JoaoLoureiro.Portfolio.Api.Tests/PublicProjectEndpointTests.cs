using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using JoaoLoureiro.Portfolio.Api.Dtos;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace JoaoLoureiro.Portfolio.Api.Tests;

public sealed class PublicProjectEndpointTests(TestWebApplicationFactory factory)
    : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false
    });

    [Fact]
    public async Task AnonymousListIsOrderedLocalizedCachedAndDoesNotExposeAdminMetadata()
    {
        using var response = await client.GetAsync("/api/projects?locale=en");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("public, max-age=60, s-maxage=300, stale-while-revalidate=86400", response.Headers.CacheControl?.ToString());
        Assert.True(response.Headers.ETag is not null);
        var result = await response.Content.ReadFromJsonAsync<PublicProjectListResponse>();
        Assert.Equal(new[] { 1, 2, 3 }, result!.Items.Select(project => project.Id));
        Assert.Equal("Personal Portfolio", result.Items[0].Title);
        Assert.Equal(new[] { "Next.js", ".NET 8", "TypeScript", "Tailwind CSS" }, result.Items[0].Technologies);

        using var jsonResponse = await client.GetAsync("/api/projects/1?locale=en");
        using var document = JsonDocument.Parse(await jsonResponse.Content.ReadAsStreamAsync());
        Assert.False(document.RootElement.TryGetProperty("status", out _));
        Assert.False(document.RootElement.TryGetProperty("revision", out _));
    }

    [Fact]
    public async Task DetailReturnsRequestedLocaleAndSupportsNotModified()
    {
        using var firstResponse = await client.GetAsync("/api/projects/1?locale=pt");

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        var project = await firstResponse.Content.ReadFromJsonAsync<PublicProjectResponse>();
        Assert.Equal("Portfólio Pessoal", project!.Title);
        Assert.Equal("Principais Características:", project.FeatureHeading);
        Assert.Equal(4, project.Features.Count);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/projects/1?locale=pt");
        request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(firstResponse.Headers.ETag!.Tag));
        using var notModified = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotModified, notModified.StatusCode);
    }

    [Fact]
    public async Task InvalidLocaleAndUnpublishedProjectsAreNotPublic()
    {
        using var invalidLocale = await client.GetAsync("/api/projects?locale=fr");
        Assert.Equal(HttpStatusCode.BadRequest, invalidLocale.StatusCode);
        Assert.Equal("project_unsupported_locale", await ReadErrorKey(invalidLocale));

        using var createRequest = new HttpRequestMessage(HttpMethod.Post, "/admin/api/projects")
        {
            Content = JsonContent.Create(new
            {
                sortOrder = 110,
                imageReference = "/draft-project.png",
                technologies = new[] { ".NET 8" }
            })
        };
        createRequest.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            factory.CreateToken(TestWebApplicationFactory.AdministratorSubject));
        using var createResponse = await client.SendAsync(createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var draft = await createResponse.Content.ReadFromJsonAsync<AdminProjectResponse>();

        using var publicDetail = await client.GetAsync($"/api/projects/{draft!.Id}?locale=en");
        Assert.Equal(HttpStatusCode.NotFound, publicDetail.StatusCode);
    }

    private static async Task<string?> ReadErrorKey(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        return document.RootElement.TryGetProperty("errorKey", out var value) ? value.GetString() : null;
    }
}
