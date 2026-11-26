using System.Globalization;
using FluentValidation;
using JoaoLoureiro.Portfolio.Api.Dtos;
using JoaoLoureiro.Portfolio.Core.Projects;
using JoaoLoureiro.Portfolio.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace JoaoLoureiro.Portfolio.Api.Endpoints;

public static class AdminProjectEndpointMapping
{
    public static RouteGroupBuilder MapAdminProjectEndpoints(this RouteGroupBuilder adminApi)
    {
        var projectsApi = adminApi.MapGroup("/projects")
            .WithMetadata(new RequestSizeLimitAttribute(ProjectLimits.MaxRequestBytes));

        projectsApi.MapGet("", ListProjects)
            .WithName("ListAdminProjects")
            .Produces<AdminProjectListResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status503ServiceUnavailable);

        projectsApi.MapGet("/{id:int}", GetProject)
            .WithName("GetAdminProject")
            .Produces<AdminProjectResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status503ServiceUnavailable);

        projectsApi.MapPost("", CreateProject)
            .WithName("CreateAdminProject")
            .Produces<AdminProjectResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status503ServiceUnavailable);

        projectsApi.MapPut("/{id:int}", UpdateProject)
            .WithName("UpdateAdminProject")
            .Produces<AdminProjectResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status503ServiceUnavailable);

        projectsApi.MapPost("/{id:int}/publish", PublishProject)
            .WithName("PublishAdminProject")
            .Produces<AdminProjectResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status503ServiceUnavailable);

        projectsApi.MapPost("/{id:int}/unpublish", UnpublishProject)
            .WithName("UnpublishAdminProject")
            .Produces<AdminProjectResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status503ServiceUnavailable);

        projectsApi.MapPost("/reorder", ReorderProjects)
            .WithName("ReorderAdminProjects")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status503ServiceUnavailable);

        projectsApi.MapDelete("/{id:int}", DeleteProject)
            .WithName("DeleteAdminProject")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status503ServiceUnavailable);

        return adminApi;
    }

    private static async Task<IResult> ListProjects(
        string? page,
        string? pageSize,
        ProjectApplicationService projectService,
        ILogger<AdminProjectEndpointLogger> logger,
        CancellationToken cancellationToken)
    {
        if (!TryParsePage(page, pageSize, ProjectLimits.AdminDefaultPageSize, ProjectLimits.AdminMaxPageSize, out var parsedPage, out var parsedPageSize))
        {
            return InvalidInput();
        }

        return await RunAsync(logger, async () =>
        {
            var result = await projectService.ListAdminAsync(parsedPage, parsedPageSize, cancellationToken);
            return Results.Ok(new AdminProjectListResponse(
                result.Items.Select(MapSummary).ToArray(),
                result.Page,
                result.PageSize,
                result.HasNext));
        });
    }

    private static async Task<IResult> GetProject(
        int id,
        ProjectApplicationService projectService,
        ILogger<AdminProjectEndpointLogger> logger,
        CancellationToken cancellationToken)
    {
        return await RunAsync(logger, async () =>
        {
            var project = await projectService.GetAdminAsync(id, cancellationToken);
            return project is null ? NotFound() : Results.Ok(Map(project));
        });
    }

    private static async Task<IResult> CreateProject(
        [FromBody] CreateProjectRequest? request,
        IValidator<CreateProjectRequest> validator,
        ProjectApplicationService projectService,
        ILogger<AdminProjectEndpointLogger> logger,
        CancellationToken cancellationToken)
    {
        if (request is null || !await IsValid(validator, request, cancellationToken)) return InvalidInput();

        return await RunAsync(logger, async () =>
        {
            var result = await projectService.CreateDraftAsync(
                new CreateProjectCommand(
                    request.SortOrder,
                    request.ImageReference!,
                    request.RepositoryUrl,
                    request.LiveUrl,
                    request.Technologies!,
                    MapContent(request.Content)),
                cancellationToken);
            return result.Outcome == ProjectOperationOutcome.Succeeded
                ? Results.Created($"/admin/api/projects/{result.Project!.Id}", Map(result.Project))
                : Map(result);
        });
    }

    private static async Task<IResult> UpdateProject(
        int id,
        [FromBody] UpdateProjectRequest? request,
        IValidator<UpdateProjectRequest> validator,
        ProjectApplicationService projectService,
        ILogger<AdminProjectEndpointLogger> logger,
        CancellationToken cancellationToken)
    {
        if (request is null || !await IsValid(validator, request, cancellationToken)) return InvalidInput();

        return await RunAsync(logger, async () =>
        {
            var result = await projectService.UpdateAsync(
                id,
                new UpdateProjectCommand(
                    request.ExpectedRevision,
                    request.SortOrder,
                    request.ImageReference!,
                    request.RepositoryUrl,
                    request.LiveUrl,
                    request.Technologies!,
                    MapContent(request.Content)!),
                cancellationToken);
            return result.Outcome == ProjectOperationOutcome.Succeeded
                ? Results.Ok(Map(result.Project!))
                : Map(result);
        });
    }

    private static Task<IResult> PublishProject(
        int id,
        [FromBody] ProjectRevisionRequest? request,
        IValidator<ProjectRevisionRequest> validator,
        ProjectApplicationService projectService,
        ILogger<AdminProjectEndpointLogger> logger,
        CancellationToken cancellationToken)
    {
        return MutateLifecycle(id, request, validator, projectService.PublishAsync, logger, cancellationToken);
    }

    private static Task<IResult> UnpublishProject(
        int id,
        [FromBody] ProjectRevisionRequest? request,
        IValidator<ProjectRevisionRequest> validator,
        ProjectApplicationService projectService,
        ILogger<AdminProjectEndpointLogger> logger,
        CancellationToken cancellationToken)
    {
        return MutateLifecycle(id, request, validator, projectService.UnpublishAsync, logger, cancellationToken);
    }

    private static async Task<IResult> MutateLifecycle(
        int id,
        ProjectRevisionRequest? request,
        IValidator<ProjectRevisionRequest> validator,
        Func<int, LifecycleProjectCommand, CancellationToken, Task<ProjectOperationResult>> mutation,
        ILogger<AdminProjectEndpointLogger> logger,
        CancellationToken cancellationToken)
    {
        if (request is null || !await IsValid(validator, request, cancellationToken)) return InvalidInput();

        return await RunAsync(logger, async () =>
        {
            var result = await mutation(id, new LifecycleProjectCommand(request.ExpectedRevision), cancellationToken);
            return result.Outcome == ProjectOperationOutcome.Succeeded
                ? Results.Ok(Map(result.Project!))
                : Map(result);
        });
    }

    private static async Task<IResult> ReorderProjects(
        [FromBody] ReorderProjectsRequest? request,
        IValidator<ReorderProjectsRequest> validator,
        ProjectApplicationService projectService,
        ILogger<AdminProjectEndpointLogger> logger,
        CancellationToken cancellationToken)
    {
        if (request is null || !await IsValid(validator, request, cancellationToken))
        {
            return InvalidInput();
        }

        var items = request.Items;
        if (items is null || items.Select(item => item.Id).Distinct().Count() != items.Count)
        {
            return InvalidInput();
        }

        var changes = items
            .Select((item, index) => new ProjectOrderChange(item.Id, index + 1, item.ExpectedRevision))
            .ToArray();

        return await RunAsync(logger, async () =>
        {
            var result = await projectService.ReorderAsync(new ReorderProjectsCommand(changes), cancellationToken);
            return result.Outcome == ProjectOperationOutcome.Succeeded ? Results.NoContent() : Map(result);
        });
    }

    private static async Task<IResult> DeleteProject(
        int id,
        [FromBody] DeleteProjectRequest? request,
        IValidator<DeleteProjectRequest> validator,
        ProjectApplicationService projectService,
        ILogger<AdminProjectEndpointLogger> logger,
        CancellationToken cancellationToken)
    {
        if (request is null || !string.Equals(request.Confirmation, id.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal) ||
            !await IsValid(validator, request, cancellationToken))
        {
            return InvalidInput();
        }

        return await RunAsync(logger, async () =>
        {
            var result = await projectService.DeleteAsync(id, new DeleteProjectCommand(request.ExpectedRevision), cancellationToken);
            return result.Outcome == ProjectOperationOutcome.Succeeded ? Results.NoContent() : Map(result);
        });
    }

    private static IReadOnlyDictionary<ProjectLocale, ProjectLocalizedContent>? MapContent(ProjectContentRequest? request)
    {
        if (request is null) return null;

        return new Dictionary<ProjectLocale, ProjectLocalizedContent>
        {
            [ProjectLocale.En] = MapContent(request.En!),
            [ProjectLocale.Pt] = MapContent(request.Pt!)
        };
    }

    private static ProjectLocalizedContent MapContent(ProjectLocalizedContentRequest request)
    {
        return new ProjectLocalizedContent(
            request.Title ?? string.Empty,
            request.Summary ?? string.Empty,
            request.Details ?? string.Empty,
            request.FeatureHeading ?? string.Empty,
            request.Features ?? []);
    }

    private static AdminProjectSummaryResponse MapSummary(Project project) => new(
        project.Id,
        project.SortOrder,
        project.Status.ToString().ToLowerInvariant(),
        project.ImageReference,
        project.LocalizedContent.ToDictionary(entry => entry.Key.ToCode(), entry => entry.Value.Title),
        project.PublishedAt,
        project.UpdatedAt,
        project.Revision);

    private static AdminProjectResponse Map(Project project) => new(
        project.Id,
        project.SortOrder,
        project.Status.ToString().ToLowerInvariant(),
        project.ImageReference,
        project.RepositoryUrl,
        project.LiveUrl,
        project.PublishedAt,
        project.UpdatedAt,
        project.Revision,
        project.Technologies,
        project.LocalizedContent.ToDictionary(
            entry => entry.Key.ToCode(),
            entry => new AdminProjectLocalizedContentResponse(
                entry.Value.Title,
                entry.Value.Summary,
                entry.Value.Details,
                entry.Value.FeatureHeading,
                entry.Value.Features)));

    private static IResult Map(ProjectOperationResult result) => result.Outcome switch
    {
        ProjectOperationOutcome.NotFound => NotFound(),
        ProjectOperationOutcome.InvalidInput => InvalidInput(),
        ProjectOperationOutcome.IdentifierConflict => Conflict(ProjectErrorKeys.IdentifierConflict),
        ProjectOperationOutcome.OrderConflict => Conflict(ProjectErrorKeys.OrderConflict),
        ProjectOperationOutcome.StaleRevision => Conflict(ProjectErrorKeys.StaleRevision),
        ProjectOperationOutcome.InvalidTransition => Conflict(ProjectErrorKeys.InvalidTransition),
        ProjectOperationOutcome.PublishedDeleteConflict => Conflict(ProjectErrorKeys.PublishedDeleteConflict),
        _ => Results.Problem(
            statusCode: StatusCodes.Status500InternalServerError,
            title: "Server Error",
            extensions: new Dictionary<string, object?> { ["errorKey"] = "server_unexpected_error" })
    };

    private static async Task<IResult> RunAsync(
        ILogger<AdminProjectEndpointLogger> logger,
        Func<Task<IResult>> operation)
    {
        try
        {
            return await operation();
        }
        catch (ProjectStorageUnavailableException exception)
        {
            logger.LogError(exception, "Project storage operation failed: {Operation}.", exception.Operation);
            return Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Project storage unavailable",
                extensions: new Dictionary<string, object?> { ["errorKey"] = ProjectErrorKeys.StorageUnavailable });
        }
        catch (ProjectDomainException)
        {
            return InvalidInput();
        }
    }

    private static async Task<bool> IsValid<T>(IValidator<T> validator, T request, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(request, cancellationToken);
        return result.IsValid;
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

    private static IResult NotFound() => Problem(ProjectErrorKeys.NotFound, StatusCodes.Status404NotFound, "Project not found.");

    private static IResult Conflict(string errorKey) => Problem(errorKey, StatusCodes.Status409Conflict, "Project operation conflict.");

    private static IResult Problem(string errorKey, int statusCode, string title) => Results.Problem(
        statusCode: statusCode,
        title: title,
        extensions: new Dictionary<string, object?> { ["errorKey"] = errorKey });

    private sealed class AdminProjectEndpointLogger;
}
