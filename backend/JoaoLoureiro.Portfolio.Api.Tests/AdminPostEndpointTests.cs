using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using JoaoLoureiro.Portfolio.Api.Authentication;
using JoaoLoureiro.Portfolio.Api.Dtos;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace JoaoLoureiro.Portfolio.Api.Tests;

public sealed class AdminPostEndpointTests(TestWebApplicationFactory factory)
    : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false
    });

    [Theory]
    [InlineData("GET", "/admin/api/posts")]
    [InlineData("GET", "/admin/api/posts/00000000-0000-0000-0000-000000000000")]
    [InlineData("POST", "/admin/api/posts")]
    [InlineData("PUT", "/admin/api/posts/00000000-0000-0000-0000-000000000000")]
    [InlineData("POST", "/admin/api/posts/00000000-0000-0000-0000-000000000000/publish")]
    [InlineData("POST", "/admin/api/posts/00000000-0000-0000-0000-000000000000/unpublish")]
    [InlineData("DELETE", "/admin/api/posts/00000000-0000-0000-0000-000000000000")]
    public async Task EveryPostOperationRequiresAdministratorJwt(string method, string uri)
    {
        using var response = await SendProbeAsync(method, uri, "missing");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("GET", "/admin/api/posts")]
    [InlineData("GET", "/admin/api/posts/00000000-0000-0000-0000-000000000000")]
    [InlineData("POST", "/admin/api/posts")]
    [InlineData("PUT", "/admin/api/posts/00000000-0000-0000-0000-000000000000")]
    [InlineData("POST", "/admin/api/posts/00000000-0000-0000-0000-000000000000/publish")]
    [InlineData("POST", "/admin/api/posts/00000000-0000-0000-0000-000000000000/unpublish")]
    [InlineData("DELETE", "/admin/api/posts/00000000-0000-0000-0000-000000000000")]
    public async Task InvalidAndWrongSubjectTokensAreRejectedForEveryPostOperation(string method, string uri)
    {
        using var invalidResponse = await SendProbeAsync(method, uri, "invalid");
        using var wrongSubjectResponse = await SendProbeAsync(method, uri, "wrong-subject");

        Assert.Equal(HttpStatusCode.Unauthorized, invalidResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, wrongSubjectResponse.StatusCode);
    }

    [Fact]
    public async Task AdminCanCreateEditPublishUnpublishAndDeleteAPost()
    {
        var slug = $"lifecycle-{Guid.NewGuid():N}";
        using var createResponse = await SendAsync(
            HttpMethod.Post,
            "/admin/api/posts",
            new
            {
                slug,
                content = CompleteContent()
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.Equal("no-store", createResponse.Headers.CacheControl?.ToString());
        var created = await createResponse.Content.ReadFromJsonAsync<AdminPostResponse>();
        Assert.NotNull(created);
        Assert.Equal("draft", created.Status);
        Assert.Equal(1, created.Revision);

        using var listResponse = await SendAsync(HttpMethod.Get, "/admin/api/posts?page=1&pageSize=20");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var list = await listResponse.Content.ReadFromJsonAsync<AdminPostListResponse>();
        Assert.Contains(list!.Items, item => item.Id == created.Id);

        using var updateResponse = await SendAsync(
            HttpMethod.Put,
            $"/admin/api/posts/{created.Id:D}",
            new
            {
                expectedRevision = created.Revision,
                content = CompleteContent("Updated title")
            });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<AdminPostResponse>();
        Assert.Equal(2, updated!.Revision);

        using var publishResponse = await SendAsync(
            HttpMethod.Post,
            $"/admin/api/posts/{created.Id:D}/publish",
            new { expectedRevision = updated.Revision });
        Assert.Equal(HttpStatusCode.OK, publishResponse.StatusCode);
        var published = await publishResponse.Content.ReadFromJsonAsync<AdminPostResponse>();
        Assert.Equal("published", published!.Status);
        Assert.NotNull(published.PublishedAt);
        Assert.Equal(3, published.Revision);

        using var publishedDeleteResponse = await SendAsync(
            HttpMethod.Delete,
            $"/admin/api/posts/{created.Id:D}",
            new
            {
                expectedRevision = published.Revision,
                confirmation = created.Id.ToString("D")
            });
        Assert.Equal(HttpStatusCode.Conflict, publishedDeleteResponse.StatusCode);
        Assert.Equal("post_published_delete_conflict", await ReadErrorKey(publishedDeleteResponse));

        using var unpublishResponse = await SendAsync(
            HttpMethod.Post,
            $"/admin/api/posts/{created.Id:D}/unpublish",
            new { expectedRevision = published.Revision });
        Assert.Equal(HttpStatusCode.OK, unpublishResponse.StatusCode);
        var unpublished = await unpublishResponse.Content.ReadFromJsonAsync<AdminPostResponse>();
        Assert.Equal("unpublished", unpublished!.Status);
        Assert.Equal(published.PublishedAt, unpublished.PublishedAt);
        Assert.Equal(4, unpublished.Revision);

        using var deleteResponse = await SendAsync(
            HttpMethod.Delete,
            $"/admin/api/posts/{created.Id:D}",
            new
            {
                expectedRevision = unpublished.Revision,
                confirmation = created.Id.ToString("D")
            });
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        using var missingResponse = await SendAsync(
            HttpMethod.Get,
            $"/admin/api/posts/{created.Id:D}");
        Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);
    }

    [Fact]
    public async Task IncompletePublicationAndStaleRevisionAreRejected()
    {
        var slug = $"validation-{Guid.NewGuid():N}";
        using var createResponse = await SendAsync(
            HttpMethod.Post,
            "/admin/api/posts",
            new { slug });
        var created = await createResponse.Content.ReadFromJsonAsync<AdminPostResponse>();
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        using var publishResponse = await SendAsync(
            HttpMethod.Post,
            $"/admin/api/posts/{created!.Id:D}/publish",
            new { expectedRevision = created.Revision });
        Assert.Equal(HttpStatusCode.BadRequest, publishResponse.StatusCode);
        Assert.Equal("post_invalid_input", await ReadErrorKey(publishResponse));

        using var updateResponse = await SendAsync(
            HttpMethod.Put,
            $"/admin/api/posts/{created.Id:D}",
            new
            {
                expectedRevision = created.Revision,
                content = CompleteContent("Current title")
            });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        using var staleResponse = await SendAsync(
            HttpMethod.Put,
            $"/admin/api/posts/{created.Id:D}",
            new
            {
                expectedRevision = created.Revision,
                content = CompleteContent("Stale title")
            });
        Assert.Equal(HttpStatusCode.Conflict, staleResponse.StatusCode);
        Assert.Equal("post_stale_revision", await ReadErrorKey(staleResponse));
    }

    [Fact]
    public async Task StaleLifecycleAndDeleteRevisionsAreRejected()
    {
        using var createResponse = await SendAsync(
            HttpMethod.Post,
            "/admin/api/posts",
            new {slug = $"stale-lifecycle-{Guid.NewGuid():N}", content = CompleteContent()});
        var created = await createResponse.Content.ReadFromJsonAsync<AdminPostResponse>();

        using var publishResponse = await SendAsync(
            HttpMethod.Post,
            $"/admin/api/posts/{created!.Id:D}/publish",
            new {expectedRevision = created.Revision});
        var published = await publishResponse.Content.ReadFromJsonAsync<AdminPostResponse>();

        using var editResponse = await SendAsync(
            HttpMethod.Put,
            $"/admin/api/posts/{created.Id:D}",
            new {expectedRevision = published!.Revision, content = CompleteContent("Updated after publish")});
        Assert.Equal(HttpStatusCode.OK, editResponse.StatusCode);

        using var staleUnpublish = await SendAsync(
            HttpMethod.Post,
            $"/admin/api/posts/{created.Id:D}/unpublish",
            new {expectedRevision = published.Revision});
        Assert.Equal(HttpStatusCode.Conflict, staleUnpublish.StatusCode);
        Assert.Equal("post_stale_revision", await ReadErrorKey(staleUnpublish));

        using var unpublishResponse = await SendAsync(
            HttpMethod.Post,
            $"/admin/api/posts/{created.Id:D}/unpublish",
            new {expectedRevision = published.Revision + 1});
        var unpublished = await unpublishResponse.Content.ReadFromJsonAsync<AdminPostResponse>();

        using var staleDelete = await SendAsync(
            HttpMethod.Delete,
            $"/admin/api/posts/{created.Id:D}",
            new {expectedRevision = unpublished!.Revision - 1, confirmation = created.Id.ToString("D")});
        Assert.Equal(HttpStatusCode.Conflict, staleDelete.StatusCode);
        Assert.Equal("post_stale_revision", await ReadErrorKey(staleDelete));
    }

    [Fact]
    public async Task PostCoverImageAndTagsArePersistedAndExposedPublicly()
    {
        var slug = $"visual-{Guid.NewGuid():N}";
        const string coverImageUrl = "https://images.example.com/cover.jpg";
        using var createResponse = await SendAsync(HttpMethod.Post, "/admin/api/posts", new
        {
            slug,
            coverImageUrl,
            tags = new[] { "next-js", "accessibility" },
            content = new
            {
                en = new { title = "Visual post", excerpt = "English excerpt", body = "English body", coverImageAlt = "A desk" },
                pt = new { title = "Nota visual", excerpt = "Resumo", body = "Texto", coverImageAlt = "Uma mesa" }
            }
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        using var adminDocument = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var adminPost = adminDocument.RootElement;
        Assert.Equal(coverImageUrl, adminPost.GetProperty("coverImageUrl").GetString());
        Assert.Equal("A desk", adminPost.GetProperty("content").GetProperty("en").GetProperty("coverImageAlt").GetString());
        Assert.Equal(new[] { "next-js", "accessibility" }, adminPost.GetProperty("tags").EnumerateArray().Select(tag => tag.GetString()).ToArray());

        var id = adminPost.GetProperty("id").GetGuid();
        var revision = adminPost.GetProperty("revision").GetInt64();
        using var publishResponse = await SendAsync(HttpMethod.Post, $"/admin/api/posts/{id:D}/publish", new { expectedRevision = revision });
        Assert.Equal(HttpStatusCode.OK, publishResponse.StatusCode);

        using var publicListResponse = await SendAsync(HttpMethod.Get, "/api/posts?locale=en&pageSize=20");
        using var listDocument = JsonDocument.Parse(await publicListResponse.Content.ReadAsStringAsync());
        var summary = listDocument.RootElement.GetProperty("items").EnumerateArray().Single(post => post.GetProperty("slug").GetString() == slug);
        Assert.Equal(coverImageUrl, summary.GetProperty("coverImageUrl").GetString());
        Assert.Equal("A desk", summary.GetProperty("coverImageAlt").GetString());
        Assert.Equal(new[] { "next-js", "accessibility" }, summary.GetProperty("tags").EnumerateArray().Select(tag => tag.GetString()).ToArray());

        using var detailResponse = await SendAsync(HttpMethod.Get, $"/api/posts/{slug}?locale=pt");
        using var detailDocument = JsonDocument.Parse(await detailResponse.Content.ReadAsStringAsync());
        Assert.Equal("Uma mesa", detailDocument.RootElement.GetProperty("coverImageAlt").GetString());
    }

    [Fact]
    public async Task PostCoverImageRejectsNonHttpsUrls()
    {
        using var response = await SendAsync(HttpMethod.Post, "/admin/api/posts", new
        {
            slug = $"unsafe-image-{Guid.NewGuid():N}",
            coverImageUrl = "http://images.example.com/cover.jpg",
            content = CompleteContent()
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("post_invalid_input", await ReadErrorKey(response));
    }

    [Fact]
    public async Task DuplicateSlugReturnsSlugConflict()
    {
        var slug = $"unique-{Guid.NewGuid():N}";
        using var first = await SendAsync(HttpMethod.Post, "/admin/api/posts", new { slug });
        using var second = await SendAsync(HttpMethod.Post, "/admin/api/posts", new { slug });

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("post_slug_conflict", await ReadErrorKey(second));
    }

    [Theory]
    [InlineData("bad slug")]
    [InlineData("posts")]
    [InlineData("Admin")]
    public async Task InvalidAndReservedSlugsReturnInvalidInput(string slug)
    {
        using var response = await SendAsync(HttpMethod.Post, "/admin/api/posts", new { slug });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("post_invalid_input", await ReadErrorKey(response));
    }

    [Fact]
    public async Task SlugRemainsImmutableWhenAnUpdatePayloadIncludesAnotherSlug()
    {
        var slug = $"immutable-{Guid.NewGuid():N}";
        using var createResponse = await SendAsync(HttpMethod.Post, "/admin/api/posts", new { slug });
        var created = await createResponse.Content.ReadFromJsonAsync<AdminPostResponse>();

        using var updateResponse = await SendAsync(
            HttpMethod.Put,
            $"/admin/api/posts/{created!.Id:D}",
            new { slug = "attempted-change", expectedRevision = created.Revision, content = CompleteContent() });
        var updated = await updateResponse.Content.ReadFromJsonAsync<AdminPostResponse>();

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.Equal(slug, updated!.Slug);
    }

    [Fact]
    public async Task FieldAndRequestSizeLimitsAreRejectedBeforePersistence()
    {
        using var oversizedField = await SendAsync(
            HttpMethod.Post,
            "/admin/api/posts",
            new { slug = $"oversized-{Guid.NewGuid():N}", content = new { en = new { title = new string('x', 121), excerpt = "excerpt", body = "body" }, pt = new { title = "Título", excerpt = "Resumo", body = "Corpo" } } });
        Assert.Equal(HttpStatusCode.BadRequest, oversizedField.StatusCode);

        using var oversizedRequest = await SendAsync(
            HttpMethod.Post,
            "/admin/api/posts",
            new { slug = $"large-request-{Guid.NewGuid():N}", content = new { en = new { title = "title", excerpt = "excerpt", body = new string('x', 140_000) }, pt = new { title = "Título", excerpt = "Resumo", body = new string('y', 140_000) } } });
        Assert.Equal(HttpStatusCode.BadRequest, oversizedRequest.StatusCode);
        Assert.Equal("post_invalid_input", await ReadErrorKey(oversizedRequest));
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

    private async Task<HttpResponseMessage> SendProbeAsync(string method, string uri, string tokenKind)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), uri)
        {
            Content = JsonContent.Create(new { expectedRevision = 1, confirmation = Guid.Empty.ToString("D") })
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

    private static object CompleteContent(string title = "English title")
    {
        return new
        {
            en = new
            {
                title,
                excerpt = "English excerpt",
                body = "# Exact markdown\n\n[link](https://example.com)"
            },
            pt = new
            {
                title = "Título em português",
                excerpt = "Resumo em português",
                body = "Corpo em português"
            }
        };
    }
}
