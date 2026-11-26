using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using JoaoLoureiro.Portfolio.Api.Dtos;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace JoaoLoureiro.Portfolio.Api.Tests;

public sealed class AdminProjectEndpointTests(TestWebApplicationFactory factory)
    : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false
    });

    [Theory]
    [InlineData("GET", "/admin/api/projects")]
    [InlineData("GET", "/admin/api/projects/1")]
    [InlineData("POST", "/admin/api/projects")]
    [InlineData("PUT", "/admin/api/projects/1")]
    [InlineData("POST", "/admin/api/projects/1/publish")]
    [InlineData("POST", "/admin/api/projects/1/unpublish")]
    [InlineData("POST", "/admin/api/projects/reorder")]
    [InlineData("DELETE", "/admin/api/projects/1")]
    public async Task EveryProjectOperationRequiresAdministratorJwt(string method, string uri)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), uri)
        {
            Content = JsonContent.Create(new { expectedRevision = 1, confirmation = "1", items = new[] { new { id = 1, expectedRevision = 1 } } })
        };

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("GET", "/admin/api/projects")]
    [InlineData("GET", "/admin/api/projects/1")]
    [InlineData("POST", "/admin/api/projects")]
    [InlineData("PUT", "/admin/api/projects/1")]
    [InlineData("POST", "/admin/api/projects/1/publish")]
    [InlineData("POST", "/admin/api/projects/1/unpublish")]
    [InlineData("POST", "/admin/api/projects/reorder")]
    [InlineData("DELETE", "/admin/api/projects/1")]
    public async Task EveryProjectOperationRejectsInvalidAndWrongSubjectTokens(string method, string uri)
    {
        using var invalidRequest = CreateRequest(method, uri, "invalid");
        using var wrongSubjectRequest = CreateRequest(method, uri, "wrong-subject");

        using var invalidResponse = await client.SendAsync(invalidRequest);
        using var wrongSubjectResponse = await client.SendAsync(wrongSubjectRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, invalidResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, wrongSubjectResponse.StatusCode);
    }

    [Fact]
    public async Task AdministratorCanManageProjectLifecycleAndDeleteOnlyAfterUnpublish()
    {
        using var createResponse = await SendAsync(
            HttpMethod.Post,
            "/admin/api/projects",
            new
            {
                sortOrder = 100,
                imageReference = "/managed-project.png",
                repositoryUrl = "https://github.com/joaonloureiro/portfolio-app",
                liveUrl = "https://joaoloureiro.dev.br",
                technologies = new[] { ".NET 8", "SQLite" },
                content = CompleteContent()
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<AdminProjectResponse>();
        Assert.NotNull(created);
        Assert.Equal("draft", created!.Status);
        Assert.Equal(1, created.Revision);
        Assert.Equal("English title", created.Content["en"].Title);

        using var listResponse = await SendAsync(HttpMethod.Get, "/admin/api/projects?page=1&pageSize=20");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var list = await listResponse.Content.ReadFromJsonAsync<AdminProjectListResponse>();
        Assert.Contains(list!.Items, item => item.Id == created.Id);

        using var updateResponse = await SendAsync(
            HttpMethod.Put,
            $"/admin/api/projects/{created.Id}",
            new
            {
                expectedRevision = created.Revision,
                sortOrder = 99,
                imageReference = "/managed-project-updated.png",
                technologies = new[] { ".NET 8", "SQLite", "EF Core" },
                content = CompleteContent("Updated title")
            });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<AdminProjectResponse>();
        Assert.Equal(2, updated!.Revision);

        using var publishResponse = await SendAsync(
            HttpMethod.Post,
            $"/admin/api/projects/{created.Id}/publish",
            new { expectedRevision = updated.Revision });
        Assert.Equal(HttpStatusCode.OK, publishResponse.StatusCode);
        var published = await publishResponse.Content.ReadFromJsonAsync<AdminProjectResponse>();
        Assert.Equal("published", published!.Status);
        Assert.Equal(3, published.Revision);

        using var publishedDeleteResponse = await SendAsync(
            HttpMethod.Delete,
            $"/admin/api/projects/{created.Id}",
            new { expectedRevision = published.Revision, confirmation = created.Id.ToString() });
        Assert.Equal(HttpStatusCode.Conflict, publishedDeleteResponse.StatusCode);
        Assert.Equal("project_published_delete_conflict", await ReadErrorKey(publishedDeleteResponse));

        using var unpublishResponse = await SendAsync(
            HttpMethod.Post,
            $"/admin/api/projects/{created.Id}/unpublish",
            new { expectedRevision = published.Revision });
        Assert.Equal(HttpStatusCode.OK, unpublishResponse.StatusCode);
        var unpublished = await unpublishResponse.Content.ReadFromJsonAsync<AdminProjectResponse>();
        Assert.Equal("unpublished", unpublished!.Status);
        Assert.Equal(4, unpublished.Revision);

        using var deleteResponse = await SendAsync(
            HttpMethod.Delete,
            $"/admin/api/projects/{created.Id}",
            new { expectedRevision = unpublished.Revision, confirmation = created.Id.ToString() });
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        using var missingResponse = await SendAsync(HttpMethod.Get, $"/admin/api/projects/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);
    }

    [Fact]
    public async Task ProjectEndpointsRejectStaleUpdatesAndIncompletePublication()
    {
        using var createResponse = await SendAsync(
            HttpMethod.Post,
            "/admin/api/projects",
            new
            {
                sortOrder = 101,
                imageReference = "/validation-project.png",
                technologies = new[] { ".NET 8" }
            });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<AdminProjectResponse>();

        using var publishResponse = await SendAsync(
            HttpMethod.Post,
            $"/admin/api/projects/{created!.Id}/publish",
            new { expectedRevision = created.Revision });
        Assert.Equal(HttpStatusCode.BadRequest, publishResponse.StatusCode);
        Assert.Equal("project_invalid_input", await ReadErrorKey(publishResponse));

        using var updateResponse = await SendAsync(
            HttpMethod.Put,
            $"/admin/api/projects/{created.Id}",
            new
            {
                expectedRevision = created.Revision,
                sortOrder = 102,
                imageReference = "/validation-project.png",
                technologies = new[] { ".NET 8" },
                content = CompleteContent("Current title")
            });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        using var staleResponse = await SendAsync(
            HttpMethod.Put,
            $"/admin/api/projects/{created.Id}",
            new
            {
                expectedRevision = created.Revision,
                sortOrder = 103,
                imageReference = "/validation-project.png",
                technologies = new[] { ".NET 8" },
                content = CompleteContent("Stale title")
            });
        Assert.Equal(HttpStatusCode.Conflict, staleResponse.StatusCode);
        Assert.Equal("project_stale_revision", await ReadErrorKey(staleResponse));

        using var cleanupPublish = await SendAsync(
            HttpMethod.Post,
            $"/admin/api/projects/{created.Id}/publish",
            new { expectedRevision = 2 });
        Assert.Equal(HttpStatusCode.OK, cleanupPublish.StatusCode);

        using var cleanupUnpublish = await SendAsync(
            HttpMethod.Post,
            $"/admin/api/projects/{created.Id}/unpublish",
            new { expectedRevision = 3 });
        Assert.Equal(HttpStatusCode.OK, cleanupUnpublish.StatusCode);

        using var cleanup = await SendAsync(
            HttpMethod.Delete,
            $"/admin/api/projects/{created.Id}",
            new { expectedRevision = 4, confirmation = created.Id.ToString() });
        Assert.Equal(HttpStatusCode.NoContent, cleanup.StatusCode);
    }

    [Fact]
    public async Task ReorderRequiresTheFullUniqueIdentifierOrder()
    {
        using var invalidResponse = await SendAsync(
            HttpMethod.Post,
            "/admin/api/projects/reorder",
            new { items = new[] { new { id = 1, expectedRevision = 1 } } });

        Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);
        Assert.Equal("project_invalid_input", await ReadErrorKey(invalidResponse));

        using var staleResponse = await SendAsync(
            HttpMethod.Post,
            "/admin/api/projects/reorder",
            new
            {
                items = new[]
                {
                    new { id = 1, expectedRevision = 2L },
                    new { id = 2, expectedRevision = 1L },
                    new { id = 3, expectedRevision = 1L }
                }
            });

        Assert.Equal(HttpStatusCode.Conflict, staleResponse.StatusCode);
        Assert.Equal("project_stale_revision", await ReadErrorKey(staleResponse));
    }

    [Fact]
    public async Task ProjectBoundaryRejectsInvalidUrlsAndListLimits()
    {
        using var invalidUrl = await SendAsync(
            HttpMethod.Post,
            "/admin/api/projects",
            new
            {
                sortOrder = 120,
                imageReference = "//evil.example/project.png",
                repositoryUrl = "http://github.com/invalid",
                technologies = new[] { ".NET 8" }
            });
        Assert.Equal(HttpStatusCode.BadRequest, invalidUrl.StatusCode);
        Assert.Equal("project_invalid_input", await ReadErrorKey(invalidUrl));

        using var invalidPage = await SendAsync(HttpMethod.Get, "/admin/api/projects?pageSize=51");
        Assert.Equal(HttpStatusCode.BadRequest, invalidPage.StatusCode);
        Assert.Equal("project_invalid_input", await ReadErrorKey(invalidPage));
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string uri, object? body = null)
    {
        using var request = CreateRequest(method.Method, uri, "administrator");
        if (body is not null) request.Content = JsonContent.Create(body);
        return await client.SendAsync(request);
    }

    private HttpRequestMessage CreateRequest(string method, string uri, string tokenKind)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), uri)
        {
            Content = JsonContent.Create(new { expectedRevision = 1, confirmation = "1", items = new[] { new { id = 1, expectedRevision = 1 } } })
        };

        if (tokenKind == "invalid")
        {
            request.Headers.TryAddWithoutValidation("Authorization", "Bearer not-a-jwt");
        }
        else
        {
            var subject = tokenKind == "wrong-subject"
                ? "different-subject"
                : TestWebApplicationFactory.AdministratorSubject;
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {factory.CreateToken(subject)}");
        }

        return request;
    }

    private static async Task<string?> ReadErrorKey(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        return document.RootElement.TryGetProperty("errorKey", out var value) ? value.GetString() : null;
    }

    private static object CompleteContent(string title = "English title") => new
    {
        en = new
        {
            title,
            summary = "English summary",
            details = "English details",
            featureHeading = "English features",
            features = new[] { "English feature" }
        },
        pt = new
        {
            title = "Título em português",
            summary = "Resumo em português",
            details = "Detalhes em português",
            featureHeading = "Recursos",
            features = new[] { "Recurso em português" }
        }
    };
}
