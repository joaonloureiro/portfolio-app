using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using JoaoLoureiro.Portfolio.Api.Authentication;
using JoaoLoureiro.Portfolio.Api.Dtos;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace JoaoLoureiro.Portfolio.Api.Tests;

public sealed class PublicPostEndpointTests
{
    [Fact]
    public async Task DraftsAreExcludedAndEmptyPublicListsAreBounded()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var slug = $"draft-{Guid.NewGuid():N}";
        using var createResponse = await SendAdminAsync(factory, client, HttpMethod.Post, "/admin/api/posts", new { slug });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        foreach (var locale in new[] { "en", "pt" })
        {
            using var listResponse = await client.GetAsync($"/api/posts?locale={locale}");
            Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
            var list = await listResponse.Content.ReadFromJsonAsync<PublicPostListResponse>();
            Assert.NotNull(list);
            Assert.Empty(list.Items);
            Assert.Equal(1, list.Page);
            Assert.Equal(10, list.PageSize);
            Assert.False(list.HasNext);
            Assert.StartsWith("public,", listResponse.Headers.CacheControl?.ToString());
            Assert.False(string.IsNullOrWhiteSpace(listResponse.Headers.ETag?.Tag));

            using var detailResponse = await client.GetAsync($"/api/posts/{slug}?locale={locale}");
            Assert.Equal(HttpStatusCode.NotFound, detailResponse.StatusCode);
            Assert.Equal("post_not_found", await ReadErrorKey(detailResponse));
        }
    }

    [Fact]
    public async Task EmptyStoreIsSuccessfulAndContainsNoFabricatedPosts()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions {AllowAutoRedirect = false});

        foreach (var locale in new[] { "en", "pt" })
        {
            using var response = await client.GetAsync($"/api/posts?locale={locale}");
            var list = await response.Content.ReadFromJsonAsync<PublicPostListResponse>();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(list);
            Assert.Empty(list.Items);
            Assert.False(list.HasNext);
        }
    }

    [Fact]
    public async Task PublishedResponsesAreLocalizedAndSupportRevisionEtags()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var slug = $"published-{Guid.NewGuid():N}";
        var created = await CreatePublishedPostAsync(factory, client, slug);

        using var englishListResponse = await client.GetAsync("/api/posts?locale=en");
        Assert.Equal(HttpStatusCode.OK, englishListResponse.StatusCode);
        var englishListJson = await JsonDocument.ParseAsync(await englishListResponse.Content.ReadAsStreamAsync());
        var englishItem = englishListJson.RootElement.GetProperty("items").EnumerateArray()
            .Single(item => item.GetProperty("slug").GetString() == slug);
        Assert.Equal("English title", englishItem.GetProperty("title").GetString());
        Assert.False(englishItem.TryGetProperty("body", out _));
        var listEtag = englishListResponse.Headers.ETag!.Tag;

        using var listNotModifiedRequest = new HttpRequestMessage(HttpMethod.Get, "/api/posts?locale=en");
        listNotModifiedRequest.Headers.TryAddWithoutValidation("If-None-Match", listEtag);
        using var listNotModifiedResponse = await client.SendAsync(listNotModifiedRequest);
        Assert.Equal(HttpStatusCode.NotModified, listNotModifiedResponse.StatusCode);
        Assert.Equal(listEtag, listNotModifiedResponse.Headers.ETag?.Tag);

        using var englishDetailResponse = await client.GetAsync($"/api/posts/{slug}?locale=en");
        Assert.Equal(HttpStatusCode.OK, englishDetailResponse.StatusCode);
        var englishDetail = await englishDetailResponse.Content.ReadFromJsonAsync<PublicPostResponse>();
        Assert.Equal("English title", englishDetail!.Title);
        Assert.Equal("English body", englishDetail.Body);
        Assert.Equal("\"post-" + slug + "-2\"", englishDetailResponse.Headers.ETag?.Tag);

        using var portugueseDetailResponse = await client.GetAsync($"/api/posts/{slug}?locale=pt");
        Assert.Equal(HttpStatusCode.OK, portugueseDetailResponse.StatusCode);
        var portugueseDetail = await portugueseDetailResponse.Content.ReadFromJsonAsync<PublicPostResponse>();
        Assert.Equal("Título em português", portugueseDetail!.Title);
        Assert.Equal("Corpo em português", portugueseDetail.Body);
        Assert.DoesNotContain("English", portugueseDetail.Body, StringComparison.Ordinal);

        using var adminUnpublishResponse = await SendAdminAsync(
            factory,
            client,
            HttpMethod.Post,
            $"/admin/api/posts/{created.Id:D}/unpublish",
            new { expectedRevision = created.Revision });
        Assert.Equal(HttpStatusCode.OK, adminUnpublishResponse.StatusCode);

        using var unpublishedResponse = await client.GetAsync($"/api/posts/{slug}?locale=en");
        Assert.Equal(HttpStatusCode.NotFound, unpublishedResponse.StatusCode);
    }

    [Fact]
    public async Task InvalidLocaleAndUnboundedPagesAreRejectedWithoutAdminAuth()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var localeResponse = await client.GetAsync("/api/posts?locale=fr");
        Assert.Equal(HttpStatusCode.BadRequest, localeResponse.StatusCode);
        Assert.Equal("post_unsupported_locale", await ReadErrorKey(localeResponse));

        using var pageResponse = await client.GetAsync("/api/posts?locale=en&pageSize=21");
        Assert.Equal(HttpStatusCode.BadRequest, pageResponse.StatusCode);
        Assert.Equal("post_invalid_input", await ReadErrorKey(pageResponse));
    }

    private static async Task<AdminPostResponse> CreatePublishedPostAsync(
        TestWebApplicationFactory factory,
        HttpClient client,
        string slug)
    {
        using var createResponse = await SendAdminAsync(
            factory,
            client,
            HttpMethod.Post,
            "/admin/api/posts",
            new
            {
                slug,
                content = CompleteContent()
            });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<AdminPostResponse>();
        Assert.NotNull(created);

        using var publishResponse = await SendAdminAsync(
            factory,
            client,
            HttpMethod.Post,
            $"/admin/api/posts/{created.Id:D}/publish",
            new { expectedRevision = created.Revision });
        Assert.Equal(HttpStatusCode.OK, publishResponse.StatusCode);
        var published = await publishResponse.Content.ReadFromJsonAsync<AdminPostResponse>();
        Assert.NotNull(published);
        return published;
    }

    private static async Task<HttpResponseMessage> SendAdminAsync(
        TestWebApplicationFactory factory,
        HttpClient client,
        HttpMethod method,
        string uri,
        object body)
    {
        using var request = new HttpRequestMessage(method, uri)
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.TryAddWithoutValidation(
            "Authorization",
            $"Bearer {factory.CreateToken(TestWebApplicationFactory.AdministratorSubject)}");
        return await client.SendAsync(request);
    }

    private static async Task<string?> ReadErrorKey(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        return document.RootElement.TryGetProperty("errorKey", out var value)
            ? value.GetString()
            : null;
    }

    private static object CompleteContent()
    {
        return new
        {
            en = new
            {
                title = "English title",
                excerpt = "English excerpt",
                body = "English body"
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
