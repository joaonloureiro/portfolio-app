using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using JoaoLoureiro.Portfolio.Api.Dtos;
using JoaoLoureiro.Portfolio.Core.Posts;
using Microsoft.AspNetCore.Mvc;

namespace JoaoLoureiro.Portfolio.Api.Endpoints;

public static class PublicPostEndpointMapping
{
    private const int CacheMaxAgeSeconds = 60;

    public static IEndpointRouteBuilder MapPublicPostEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var postsApi = endpoints.MapGroup("/api/posts")
            .AllowAnonymous();

        postsApi.MapGet("", ListPublishedPosts)
            .WithName("ListPublishedPosts")
            .Produces<PublicPostListResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status503ServiceUnavailable);

        postsApi.MapGet("/{slug}", GetPublishedPost)
            .WithName("GetPublishedPost")
            .Produces<PublicPostResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status304NotModified)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status503ServiceUnavailable);

        return endpoints;
    }

    private static async Task<IResult> ListPublishedPosts(
        HttpContext httpContext,
        string? locale,
        string? page,
        string? pageSize,
        PostApplicationService postService,
        ILogger<PublicPostEndpointLogger> logger,
        CancellationToken cancellationToken)
    {
        if (!TryParsePage(page, pageSize, out var parsedPage, out var parsedPageSize))
        {
            return InvalidInput();
        }

        return await RunAsync(
            httpContext,
            logger,
            async () =>
            {
                var result = await postService.ListPublishedAsync(
                    locale,
                    parsedPage,
                    parsedPageSize,
                    cancellationToken);

                if (result.Outcome == PostOperationOutcome.UnsupportedLocale)
                {
                    return UnsupportedLocale();
                }

                var pageResult = result.Page!;
                var etag = CreateListEtag(locale!, pageResult);
                SetCacheHeaders(httpContext, etag);
                if (MatchesEtag(httpContext, etag))
                {
                    return Results.StatusCode(StatusCodes.Status304NotModified);
                }

                return Results.Ok(new PublicPostListResponse(
                    pageResult.Items.Select(post => new PublicPostSummaryResponse(
                        post.Slug,
                        post.PublishedAt,
                        post.Title,
                        post.Excerpt,
                        post.CoverImageUrl,
                        post.CoverImageAlt,
                        post.Tags)).ToArray(),
                    pageResult.Page,
                    pageResult.PageSize,
                    pageResult.HasNext));
            });
    }

    private static async Task<IResult> GetPublishedPost(
        string slug,
        HttpContext httpContext,
        string? locale,
        PostApplicationService postService,
        ILogger<PublicPostEndpointLogger> logger,
        CancellationToken cancellationToken)
    {
        return await RunAsync(
            httpContext,
            logger,
            async () =>
            {
                var result = await postService.GetPublishedAsync(locale, slug, cancellationToken);
                if (result.Outcome == PostOperationOutcome.UnsupportedLocale)
                {
                    return UnsupportedLocale();
                }

                if (result.Outcome == PostOperationOutcome.NotFound || result.Post is null)
                {
                    return NotFound();
                }

                var etag = CreateDetailEtag(result.Post);
                SetCacheHeaders(httpContext, etag);
                if (MatchesEtag(httpContext, etag))
                {
                    return Results.StatusCode(StatusCodes.Status304NotModified);
                }

                return Results.Ok(new PublicPostResponse(
                    result.Post.Slug,
                    result.Post.PublishedAt,
                    result.Post.Title,
                    result.Post.Excerpt,
                    result.Post.Body,
                    result.Post.CoverImageUrl,
                    result.Post.CoverImageAlt,
                    result.Post.Tags));
            });
    }

    private static string CreateListEtag(
        string locale,
        PostPage<PublicPostSummary> page)
    {
        var fingerprint = string.Join(
            '|',
            page.Items.Select(post =>
                $"{post.Slug}:{post.PublishedAt.Ticks}:{post.Revision}"));
        fingerprint = $"{locale}:{page.Page}:{page.PageSize}:{page.HasNext}:{fingerprint}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fingerprint)));
        return $"\"posts-{hash}\"";
    }

    private static string CreateDetailEtag(PublicPostDetail post)
    {
        return $"\"post-{post.Slug}-{post.Revision}\"";
    }

    private static void SetCacheHeaders(HttpContext httpContext, string etag)
    {
        httpContext.Response.Headers.ETag = etag;
        httpContext.Response.Headers.CacheControl = $"public, max-age={CacheMaxAgeSeconds}, must-revalidate";
        httpContext.Response.Headers.Vary = "Accept-Encoding";
    }

    private static bool MatchesEtag(HttpContext httpContext, string etag)
    {
        var ifNoneMatch = httpContext.Request.Headers.IfNoneMatch.ToString();
        return ifNoneMatch
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Any(candidate => candidate is "*" || string.Equals(candidate, etag, StringComparison.Ordinal));
    }

    private static async Task<IResult> RunAsync(
        HttpContext httpContext,
        ILogger<PublicPostEndpointLogger> logger,
        Func<Task<IResult>> operation)
    {
        try
        {
            return await operation();
        }
        catch (PostStorageUnavailableException exception)
        {
            logger.LogError(exception, "Public post storage operation failed: {Operation}.", exception.Operation);
            return Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Post storage unavailable",
                extensions: new Dictionary<string, object?>
                {
                    ["errorKey"] = "post_storage_unavailable"
                });
        }
    }

    private static bool TryParsePage(
        string? page,
        string? pageSize,
        out int parsedPage,
        out int parsedPageSize)
    {
        parsedPage = 1;
        parsedPageSize = PostLimits.PublicDefaultPageSize;
        return (page is null || int.TryParse(page, NumberStyles.None, CultureInfo.InvariantCulture, out parsedPage)) &&
               (pageSize is null || int.TryParse(pageSize, NumberStyles.None, CultureInfo.InvariantCulture, out parsedPageSize)) &&
               parsedPage > 0 &&
               parsedPageSize > 0 &&
               parsedPageSize <= PostLimits.PublicMaxPageSize;
    }

    private static IResult InvalidInput() => Problem(
        "post_invalid_input",
        StatusCodes.Status400BadRequest,
        "Invalid post input.");

    private static IResult UnsupportedLocale() => Problem(
        "post_unsupported_locale",
        StatusCodes.Status400BadRequest,
        "Unsupported post locale.");

    private static IResult NotFound() => Problem(
        "post_not_found",
        StatusCodes.Status404NotFound,
        "Post not found.");

    private static IResult Problem(string errorKey, int statusCode, string title) => Results.Problem(
        statusCode: statusCode,
        title: title,
        extensions: new Dictionary<string, object?>
        {
            ["errorKey"] = errorKey
        });

    private sealed class PublicPostEndpointLogger;
}
