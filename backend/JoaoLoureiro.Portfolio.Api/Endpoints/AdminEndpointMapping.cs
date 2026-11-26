using System.Security.Claims;
using System.Globalization;
using FluentValidation;
using JoaoLoureiro.Portfolio.Api.Authentication;
using JoaoLoureiro.Portfolio.Api.Dtos;
using JoaoLoureiro.Portfolio.Core.Posts;
using JoaoLoureiro.Portfolio.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace JoaoLoureiro.Portfolio.Api.Endpoints;

public static class AdminEndpointMapping
{
    public static RouteGroupBuilder MapAdminEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var adminApi = endpoints.MapGroup("/admin/api")
            .RequireAuthorization(OidcJwtAuthentication.PortfolioAdminPolicyName)
            .AddEndpointFilter(DisableCaching);

        adminApi.MapGet("/me", GetCurrentAdministrator)
            .WithName("GetAdminIdentity")
            .WithDescription("Returns the validated administrator identity.")
            .Produces<AdminIdentityResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        var postsApi = adminApi.MapGroup("/posts")
            .WithMetadata(new RequestSizeLimitAttribute(PostLimits.MaxRequestBytes));

        adminApi.MapAdminProjectEndpoints();
        adminApi.MapAdminLocaleEndpoints();
        adminApi.MapAdminEmailDeliveryEndpoints();

        postsApi.MapGet("", ListPosts)
            .WithName("ListAdminPosts")
            .Produces<AdminPostListResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status503ServiceUnavailable);

        postsApi.MapGet("/{id:guid}", GetPost)
            .WithName("GetAdminPost")
            .Produces<AdminPostResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status503ServiceUnavailable);

        postsApi.MapPost("", CreatePost)
            .WithName("CreateAdminPost")
            .Produces<AdminPostResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status503ServiceUnavailable);

        postsApi.MapPut("/{id:guid}", UpdatePost)
            .WithName("UpdateAdminPost")
            .Produces<AdminPostResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status503ServiceUnavailable);

        postsApi.MapPost("/{id:guid}/publish", PublishPost)
            .WithName("PublishAdminPost")
            .Produces<AdminPostResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status503ServiceUnavailable);

        postsApi.MapPost("/{id:guid}/unpublish", UnpublishPost)
            .WithName("UnpublishAdminPost")
            .Produces<AdminPostResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status503ServiceUnavailable);

        postsApi.MapDelete("/{id:guid}", DeletePost)
            .WithName("DeleteAdminPost")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status503ServiceUnavailable);

        return adminApi;
    }

    private static async Task<IResult> ListPosts(
        HttpContext httpContext,
        string? page,
        string? pageSize,
        PostApplicationService postService,
        ILogger<AdminEndpointLogger> logger,
        CancellationToken cancellationToken)
    {
        if (!TryParsePage(page, pageSize, PostLimits.AdminDefaultPageSize, PostLimits.AdminMaxPageSize, out var parsedPage, out var parsedPageSize))
        {
            return InvalidInput();
        }

        return await RunAsync(
            httpContext,
            logger,
            async () =>
            {
                var result = await postService.ListAdminAsync(parsedPage, parsedPageSize, cancellationToken);
                return Results.Ok(new AdminPostListResponse(
                    result.Items.Select(MapSummary).ToArray(),
                    result.Page,
                    result.PageSize,
                    result.HasNext));
            });
    }

    private static async Task<IResult> GetPost(
        Guid id,
        HttpContext httpContext,
        PostApplicationService postService,
        ILogger<AdminEndpointLogger> logger,
        CancellationToken cancellationToken)
    {
        return await RunAsync(
            httpContext,
            logger,
            async () =>
            {
                var post = await postService.GetAdminAsync(id, cancellationToken);
                return post is null
                    ? NotFound()
                    : Results.Ok(Map(post));
            });
    }

    private static async Task<IResult> CreatePost(
        [FromBody] CreatePostRequest? request,
        HttpContext httpContext,
        IValidator<CreatePostRequest> validator,
        PostApplicationService postService,
        ILogger<AdminEndpointLogger> logger,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return InvalidInput();
        }

        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return InvalidInput();
        }

        return await RunAsync(
            httpContext,
            logger,
            async () =>
            {
                var result = await postService.CreateDraftAsync(
                    new CreatePostCommand(request.Slug!, MapContent(request.Content), request.CoverImageUrl, request.Tags),
                    cancellationToken);
                return result.Outcome == PostOperationOutcome.Succeeded
                    ? Results.Created($"/admin/api/posts/{result.Post!.Id:D}", Map(result.Post))
                    : Map(result);
            });
    }

    private static async Task<IResult> UpdatePost(
        Guid id,
        [FromBody] UpdatePostRequest? request,
        HttpContext httpContext,
        IValidator<UpdatePostRequest> validator,
        PostApplicationService postService,
        ILogger<AdminEndpointLogger> logger,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return InvalidInput();
        }

        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return InvalidInput();
        }

        return await RunAsync(
            httpContext,
            logger,
            async () =>
            {
                var result = await postService.UpdateAsync(
                    id,
                    new UpdatePostCommand(request.ExpectedRevision, MapContent(request.Content)!, request.CoverImageUrl, request.Tags),
                    cancellationToken);
                return result.Outcome == PostOperationOutcome.Succeeded
                    ? Results.Ok(Map(result.Post!))
                    : Map(result);
            });
    }

    private static Task<IResult> PublishPost(
        Guid id,
        [FromBody] PostRevisionRequest? request,
        HttpContext httpContext,
        IValidator<PostRevisionRequest> validator,
        PostApplicationService postService,
        ILogger<AdminEndpointLogger> logger,
        CancellationToken cancellationToken)
    {
        return MutateLifecycle(
            id,
            request,
            httpContext,
            validator,
            postService.PublishAsync,
            logger,
            cancellationToken);
    }

    private static Task<IResult> UnpublishPost(
        Guid id,
        [FromBody] PostRevisionRequest? request,
        HttpContext httpContext,
        IValidator<PostRevisionRequest> validator,
        PostApplicationService postService,
        ILogger<AdminEndpointLogger> logger,
        CancellationToken cancellationToken)
    {
        return MutateLifecycle(
            id,
            request,
            httpContext,
            validator,
            postService.UnpublishAsync,
            logger,
            cancellationToken);
    }

    private static async Task<IResult> MutateLifecycle(
        Guid id,
        PostRevisionRequest? request,
        HttpContext httpContext,
        IValidator<PostRevisionRequest> validator,
        Func<Guid, LifecyclePostCommand, CancellationToken, Task<PostOperationResult>> mutation,
        ILogger<AdminEndpointLogger> logger,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return InvalidInput();
        }

        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return InvalidInput();
        }

        return await RunAsync(
            httpContext,
            logger,
            async () =>
            {
                var result = await mutation(
                    id,
                    new LifecyclePostCommand(request.ExpectedRevision),
                    cancellationToken);
                return result.Outcome == PostOperationOutcome.Succeeded
                    ? Results.Ok(Map(result.Post!))
                    : Map(result);
            });
    }

    private static async Task<IResult> DeletePost(
        Guid id,
        [FromBody] DeletePostRequest? request,
        HttpContext httpContext,
        IValidator<DeletePostRequest> validator,
        PostApplicationService postService,
        ILogger<AdminEndpointLogger> logger,
        CancellationToken cancellationToken)
    {
        if (request is null || !string.Equals(request.Confirmation, id.ToString("D"), StringComparison.OrdinalIgnoreCase))
        {
            return InvalidInput();
        }

        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return InvalidInput();
        }

        return await RunAsync(
            httpContext,
            logger,
            async () =>
            {
                var result = await postService.DeleteAsync(
                    id,
                    new DeletePostCommand(request.ExpectedRevision),
                    cancellationToken);
                return result.Outcome == PostOperationOutcome.Succeeded
                    ? Results.NoContent()
                    : Map(result);
            });
    }

    private static IReadOnlyDictionary<PostLocale, PostLocalizedContent>? MapContent(PostContentRequest? request)
    {
        if (request is null)
        {
            return null;
        }

        var content = new Dictionary<PostLocale, PostLocalizedContent>();
        if (request.En is not null)
        {
            content[PostLocale.En] = MapContent(request.En);
        }

        if (request.Pt is not null)
        {
            content[PostLocale.Pt] = MapContent(request.Pt);
        }

        return content;
    }

    private static PostLocalizedContent MapContent(PostLocalizedContentRequest request)
    {
        return new PostLocalizedContent(
            request.Title ?? string.Empty,
            request.Excerpt ?? string.Empty,
            request.Body ?? string.Empty,
            request.CoverImageAlt);
    }

    private static AdminPostSummaryResponse MapSummary(Post post)
    {
        return new AdminPostSummaryResponse(
            post.Id,
            post.Slug,
            post.Status.ToString().ToLowerInvariant(),
            post.PublishedAt,
            post.UpdatedAt,
            post.Revision);
    }

    private static AdminPostResponse Map(Post post)
    {
        return new AdminPostResponse(
            post.Id,
            post.Slug,
            post.Status.ToString().ToLowerInvariant(),
            post.PublishedAt,
            post.UpdatedAt,
            post.Revision,
            post.LocalizedContent.ToDictionary(
                entry => entry.Key.ToCode(),
                entry => new AdminLocalizedPostResponse(
                    entry.Value.Title,
                    entry.Value.Excerpt,
                    entry.Value.Body,
                    entry.Value.CoverImageAlt)),
            post.CoverImageUrl,
            post.Tags);
    }

    private static IResult Map(PostOperationResult result)
    {
        return result.Outcome switch
        {
            PostOperationOutcome.NotFound => NotFound(),
            PostOperationOutcome.InvalidInput => InvalidInput(),
            PostOperationOutcome.SlugConflict => Conflict("post_slug_conflict"),
            PostOperationOutcome.StaleRevision => Conflict("post_stale_revision"),
            PostOperationOutcome.InvalidTransition => Conflict("post_invalid_transition"),
            PostOperationOutcome.PublishedDeleteConflict => Conflict("post_published_delete_conflict"),
            _ => Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Server Error",
                extensions: new Dictionary<string, object?> { ["errorKey"] = "server_unexpected_error" })
        };
    }

    private static async Task<IResult> RunAsync(
        HttpContext httpContext,
        ILogger<AdminEndpointLogger> logger,
        Func<Task<IResult>> operation)
    {
        try
        {
            return await operation();
        }
        catch (PostStorageUnavailableException exception)
        {
            logger.LogError(exception, "Post storage operation failed: {Operation}.", exception.Operation);
            return Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Post storage unavailable",
                extensions: new Dictionary<string, object?> { ["errorKey"] = "post_storage_unavailable" });
        }
    }

    private static bool TryParsePage(
        string? page,
        string? pageSize,
        int defaultPageSize,
        int maximumPageSize,
        out int parsedPage,
        out int parsedPageSize)
    {
        parsedPage = 1;
        parsedPageSize = defaultPageSize;
        return (page is null || int.TryParse(page, NumberStyles.None, CultureInfo.InvariantCulture, out parsedPage)) &&
               (pageSize is null || int.TryParse(pageSize, NumberStyles.None, CultureInfo.InvariantCulture, out parsedPageSize)) &&
               parsedPage > 0 &&
               parsedPageSize > 0 &&
               parsedPageSize <= maximumPageSize;
    }

    private static IResult InvalidInput() => Problem("post_invalid_input", StatusCodes.Status400BadRequest, "Invalid post input.");

    private static IResult NotFound() => Problem("post_not_found", StatusCodes.Status404NotFound, "Post not found.");

    private static IResult Conflict(string errorKey) => Problem(errorKey, StatusCodes.Status409Conflict, "Post operation conflict.");

    private static IResult Problem(string errorKey, int statusCode, string title) => Results.Problem(
        statusCode: statusCode,
        title: title,
        extensions: new Dictionary<string, object?> { ["errorKey"] = errorKey });

    private static ValueTask<object?> DisableCaching(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        context.HttpContext.Response.Headers.CacheControl = "no-store";
        context.HttpContext.Response.Headers.Pragma = "no-cache";
        return next(context);
    }

    private sealed class AdminEndpointLogger;

    private static IResult GetCurrentAdministrator(ClaimsPrincipal user)
    {
        var subject = user.FindFirst(OidcJwtAuthentication.SubjectClaimType)?.Value;
        return subject is { Length: > 0 }
            ? Results.Ok(new AdminIdentityResponse(subject))
            : Results.Problem(
                detail: "Authenticated identity is unavailable.",
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Server Error",
                extensions: new Dictionary<string, object?>
                {
                    ["errorKey"] = "server_identity_unavailable"
                });
    }
}
