using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using JoaoLoureiro.Portfolio.Api.Dtos;
using JoaoLoureiro.Portfolio.Core.Projects;
using JoaoLoureiro.Portfolio.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace JoaoLoureiro.Portfolio.Api.Endpoints;

public static class PublicProjectEndpointMapping
{
    private const int CacheMaxAgeSeconds = 60;
    private const int SharedCacheMaxAgeSeconds = 300;
    private const int StaleWhileRevalidateSeconds = 86_400;

    public static IEndpointRouteBuilder MapPublicProjectEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var projectsApi = endpoints.MapGroup("/api/projects")
            .AllowAnonymous();

        projectsApi.MapGet("", ListPublishedProjects)
            .WithName("ListPublishedProjects")
            .Produces<PublicProjectListResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status304NotModified)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status503ServiceUnavailable);

        projectsApi.MapGet("/{id:int}", GetPublishedProject)
            .WithName("GetPublishedProject")
            .Produces<PublicProjectResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status304NotModified)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status503ServiceUnavailable);

        return endpoints;
    }

    private static async Task<IResult> ListPublishedProjects(
        HttpContext httpContext,
        string? locale,
        string? page,
        string? pageSize,
        ProjectApplicationService projectService,
        ILogger<PublicProjectEndpointLogger> logger,
        CancellationToken cancellationToken)
    {
        if (!TryParsePage(page, pageSize, ProjectLimits.PublicDefaultPageSize, ProjectLimits.PublicMaxPageSize, out var parsedPage, out var parsedPageSize))
        {
            return InvalidInput();
        }

        return await RunAsync(logger, async () =>
        {
            var result = await projectService.ListPublishedAsync(locale, parsedPage, parsedPageSize, cancellationToken);
            if (result.Outcome == ProjectOperationOutcome.UnsupportedLocale) return UnsupportedLocale();

            var pageResult = result.Page!;
            var etag = CreateListEtag(locale!, pageResult);
            SetCacheHeaders(httpContext, etag);
            if (MatchesEtag(httpContext, etag)) return Results.StatusCode(StatusCodes.Status304NotModified);

            return Results.Ok(new PublicProjectListResponse(
                pageResult.Items.Select(Map).ToArray(),
                pageResult.Page,
                pageResult.PageSize,
                pageResult.HasNext));
        });
    }

    private static async Task<IResult> GetPublishedProject(
        int id,
        HttpContext httpContext,
        string? locale,
        ProjectApplicationService projectService,
        ILogger<PublicProjectEndpointLogger> logger,
        CancellationToken cancellationToken)
    {
        return await RunAsync(logger, async () =>
        {
            var result = await projectService.GetPublishedAsync(locale, id, cancellationToken);
            if (result.Outcome == ProjectOperationOutcome.UnsupportedLocale) return UnsupportedLocale();
            if (result.Outcome == ProjectOperationOutcome.NotFound || result.Project is null) return NotFound();

            var etag = CreateDetailEtag(result.Project);
            SetCacheHeaders(httpContext, etag);
            if (MatchesEtag(httpContext, etag)) return Results.StatusCode(StatusCodes.Status304NotModified);

            return Results.Ok(Map(result.Project));
        });
    }

    private static PublicProjectSummaryResponse Map(PublicProjectSummary project) => new(
        project.Id,
        project.SortOrder,
        project.ImageReference,
        project.RepositoryUrl,
        project.LiveUrl,
        project.Technologies,
        project.Content.Title,
        project.Content.Summary);

    private static PublicProjectResponse Map(PublicProjectDetail project) => new(
        project.Id,
        project.SortOrder,
        project.ImageReference,
        project.RepositoryUrl,
        project.LiveUrl,
        project.Technologies,
        project.Content.Title,
        project.Content.Summary,
        project.Content.Details,
        project.Content.FeatureHeading,
        project.Content.Features);

    private static string CreateListEtag(string locale, ProjectPage<PublicProjectSummary> page)
    {
        var fingerprint = string.Join('|', page.Items.Select(project =>
            $"{project.Id}:{project.SortOrder}:{project.Revision}:{project.UpdatedAt.Ticks}"));
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{locale}:{page.Page}:{page.PageSize}:{page.HasNext}:{fingerprint}")));
        return $"\"projects-{hash}\"";
    }

    private static string CreateDetailEtag(PublicProjectDetail project)
    {
        var fingerprint = $"{project.Id}:{project.Revision}:{project.UpdatedAt.Ticks}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fingerprint)));
        return $"\"project-{project.Id}-{hash}\"";
    }

    private static void SetCacheHeaders(HttpContext httpContext, string etag)
    {
        httpContext.Response.Headers.ETag = etag;
        httpContext.Response.Headers.CacheControl = $"public, max-age={CacheMaxAgeSeconds}, s-maxage={SharedCacheMaxAgeSeconds}, stale-while-revalidate={StaleWhileRevalidateSeconds}";
        httpContext.Response.Headers.Vary = "Accept-Encoding";
    }

    private static bool MatchesEtag(HttpContext httpContext, string etag)
    {
        return httpContext.Request.Headers.IfNoneMatch
            .ToString()
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Any(candidate => candidate is "*" || string.Equals(candidate, etag, StringComparison.Ordinal));
    }

    private static async Task<IResult> RunAsync(ILogger<PublicProjectEndpointLogger> logger, Func<Task<IResult>> operation)
    {
        try
        {
            return await operation();
        }
        catch (ProjectStorageUnavailableException exception)
        {
            logger.LogError(exception, "Public project storage operation failed: {Operation}.", exception.Operation);
            return Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Project storage unavailable",
                extensions: new Dictionary<string, object?> { ["errorKey"] = ProjectErrorKeys.StorageUnavailable });
        }
    }

    private static bool TryParsePage(string? page, string? pageSize, int defaultPageSize, int maximumPageSize, out int parsedPage, out int parsedPageSize)
    {
        parsedPage = 1;
        parsedPageSize = defaultPageSize;
        return (page is null || int.TryParse(page, NumberStyles.None, CultureInfo.InvariantCulture, out parsedPage)) &&
               (pageSize is null || int.TryParse(pageSize, NumberStyles.None, CultureInfo.InvariantCulture, out parsedPageSize)) &&
               parsedPage > 0 && parsedPageSize > 0 && parsedPageSize <= maximumPageSize;
    }

    private static IResult InvalidInput() => Problem(ProjectErrorKeys.InvalidInput, StatusCodes.Status400BadRequest, "Invalid project input.");

    private static IResult UnsupportedLocale() => Problem("project_unsupported_locale", StatusCodes.Status400BadRequest, "Unsupported project locale.");

    private static IResult NotFound() => Problem(ProjectErrorKeys.NotFound, StatusCodes.Status404NotFound, "Project not found.");

    private static IResult Problem(string errorKey, int statusCode, string title) => Results.Problem(
        statusCode: statusCode,
        title: title,
        extensions: new Dictionary<string, object?> { ["errorKey"] = errorKey });

    private sealed class PublicProjectEndpointLogger;
}
