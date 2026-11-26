using JoaoLoureiro.Portfolio.Domain.Entities;
using JoaoLoureiro.Portfolio.Core.Posts;

namespace JoaoLoureiro.Portfolio.Core.Projects;

public static class ProjectLimits
{
    public const int AdminDefaultPageSize = 20;
    public const int AdminMaxPageSize = 50;
    public const int PublicDefaultPageSize = 20;
    public const int PublicMaxPageSize = 50;
    public const int MaxRequestBytes = 256 * 1024;
}

public sealed record ProjectPage<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    bool HasNext);

public enum ProjectStoreMutationResult
{
    Succeeded,
    InvalidInput,
    NotFound,
    Conflict,
    IdentifierConflict,
    OrderConflict
}

public sealed class ProjectStorageUnavailableException(string operation, Exception innerException)
    : Exception($"The project store could not {operation}.", innerException)
{
    public string Operation { get; } = operation;
}

public sealed record ProjectOrderChange(int Id, int SortOrder, long ExpectedRevision);

public interface IProjectStore
{
    Task<int> GetNextIdAsync(CancellationToken cancellationToken);

    Task<ProjectPage<Project>> ListAdminAsync(int page, int pageSize, CancellationToken cancellationToken);

    Task<Project?> GetAdminAsync(int id, CancellationToken cancellationToken);

    Task<ProjectPage<Project>> ListPublishedAsync(int page, int pageSize, CancellationToken cancellationToken);

    Task<Project?> GetPublishedAsync(int id, CancellationToken cancellationToken);

    Task<ProjectStoreMutationResult> AddAsync(Project project, CancellationToken cancellationToken);

    Task<ProjectStoreMutationResult> UpdateAsync(Project project, long expectedRevision, CancellationToken cancellationToken);

    Task<ProjectStoreMutationResult> ReorderAsync(IReadOnlyList<ProjectOrderChange> changes, CancellationToken cancellationToken);

    Task<ProjectStoreMutationResult> DeleteAsync(int id, long expectedRevision, CancellationToken cancellationToken);
}

public sealed record CreateProjectCommand(
    int SortOrder,
    string ImageReference,
    string? RepositoryUrl,
    string? LiveUrl,
    IReadOnlyList<string> Technologies,
    IReadOnlyDictionary<ProjectLocale, ProjectLocalizedContent>? Content = null);

public sealed record UpdateProjectCommand(
    long ExpectedRevision,
    int SortOrder,
    string ImageReference,
    string? RepositoryUrl,
    string? LiveUrl,
    IReadOnlyList<string> Technologies,
    IReadOnlyDictionary<ProjectLocale, ProjectLocalizedContent> Content);

public sealed record LifecycleProjectCommand(long ExpectedRevision);

public sealed record DeleteProjectCommand(long ExpectedRevision);

public sealed record ReorderProjectsCommand(IReadOnlyList<ProjectOrderChange> Changes);

public enum ProjectOperationOutcome
{
    Succeeded,
    NotFound,
    InvalidInput,
    IdentifierConflict,
    OrderConflict,
    StaleRevision,
    InvalidTransition,
    PublishedDeleteConflict,
    UnsupportedLocale
}

public sealed record ProjectOperationResult(
    ProjectOperationOutcome Outcome,
    Project? Project = null);

public sealed record PublicProjectSummary(
    int Id,
    int SortOrder,
    ProjectStatus Status,
    string ImageReference,
    string? RepositoryUrl,
    string? LiveUrl,
    IReadOnlyList<string> Technologies,
    ProjectLocalizedContent Content,
    DateTime? PublishedAt,
    DateTime UpdatedAt,
    long Revision);

public sealed record PublicProjectDetail(
    int Id,
    int SortOrder,
    string ImageReference,
    string? RepositoryUrl,
    string? LiveUrl,
    IReadOnlyList<string> Technologies,
    ProjectLocalizedContent Content,
    DateTime? PublishedAt,
    DateTime UpdatedAt,
    long Revision);

public sealed class ProjectApplicationService(IProjectStore store, IClock clock)
{
    public async Task<ProjectPage<Project>> ListAdminAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        ValidatePage(page, pageSize, ProjectLimits.AdminMaxPageSize);
        return await store.ListAdminAsync(page, pageSize, cancellationToken);
    }

    public Task<Project?> GetAdminAsync(int id, CancellationToken cancellationToken)
    {
        return id < 1 ? Task.FromResult<Project?>(null) : store.GetAdminAsync(id, cancellationToken);
    }

    public async Task<ProjectOperationResult> CreateDraftAsync(CreateProjectCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var id = await store.GetNextIdAsync(cancellationToken);
            var project = Project.CreateDraft(
                id,
                command.SortOrder,
                command.ImageReference,
                command.RepositoryUrl,
                command.LiveUrl,
                command.Technologies,
                RequireUtc(clock.UtcNow),
                command.Content);

            return MapMutation(await store.AddAsync(project, cancellationToken), project);
        }
        catch (ProjectDomainException exception)
        {
            return MapDomainException(exception);
        }
    }

    public async Task<ProjectOperationResult> UpdateAsync(int id, UpdateProjectCommand command, CancellationToken cancellationToken)
    {
        if (id < 1 || command.ExpectedRevision < 1) return new(ProjectOperationOutcome.InvalidInput);
        var project = await store.GetAdminAsync(id, cancellationToken);
        if (project is null) return new(ProjectOperationOutcome.NotFound);

        try
        {
            project.Update(command.SortOrder, command.ImageReference, command.RepositoryUrl, command.LiveUrl, command.Technologies, command.Content, RequireUtc(clock.UtcNow));
            return MapMutation(await store.UpdateAsync(project, command.ExpectedRevision, cancellationToken), project);
        }
        catch (ProjectDomainException exception)
        {
            return MapDomainException(exception);
        }
    }

    public Task<ProjectOperationResult> PublishAsync(int id, LifecycleProjectCommand command, CancellationToken cancellationToken)
    {
        return MutateLifecycleAsync(id, command.ExpectedRevision, static (project, now) => project.Publish(now), cancellationToken);
    }

    public Task<ProjectOperationResult> UnpublishAsync(int id, LifecycleProjectCommand command, CancellationToken cancellationToken)
    {
        return MutateLifecycleAsync(id, command.ExpectedRevision, static (project, now) => project.Unpublish(now), cancellationToken);
    }

    public async Task<ProjectOperationResult> ReorderAsync(ReorderProjectsCommand command, CancellationToken cancellationToken)
    {
        if (command.Changes is null || command.Changes.Count == 0 ||
            command.Changes.Select(change => change.Id).Distinct().Count() != command.Changes.Count ||
            command.Changes.Any(change => change.Id < 1 || change.SortOrder < 1 || change.ExpectedRevision < 1))
        {
            return new(ProjectOperationOutcome.InvalidInput);
        }

        var expectedOrders = Enumerable.Range(1, command.Changes.Count).ToHashSet();
        if (command.Changes.Select(change => change.SortOrder).ToHashSet().SetEquals(expectedOrders) is false)
        {
            return new(ProjectOperationOutcome.InvalidInput);
        }

        return MapMutation(await store.ReorderAsync(command.Changes, cancellationToken));
    }

    public async Task<ProjectOperationResult> DeleteAsync(int id, DeleteProjectCommand command, CancellationToken cancellationToken)
    {
        if (id < 1 || command.ExpectedRevision < 1) return new(ProjectOperationOutcome.InvalidInput);
        var project = await store.GetAdminAsync(id, cancellationToken);
        if (project is null) return new(ProjectOperationOutcome.NotFound);
        if (!project.CanDelete) return new(ProjectOperationOutcome.PublishedDeleteConflict);
        return MapMutation(await store.DeleteAsync(id, command.ExpectedRevision, cancellationToken), project);
    }

    public async Task<(ProjectOperationOutcome Outcome, ProjectPage<PublicProjectSummary>? Page)> ListPublishedAsync(
        string? locale,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (!ProjectLocaleExtensions.TryParse(locale, out var parsedLocale)) return (ProjectOperationOutcome.UnsupportedLocale, null);
        ValidatePage(page, pageSize, ProjectLimits.PublicMaxPageSize);
        var projects = await store.ListPublishedAsync(page, pageSize, cancellationToken);
        var summaries = projects.Items.Select(project => ToPublicSummary(project, parsedLocale)).ToArray();
        return (ProjectOperationOutcome.Succeeded, new ProjectPage<PublicProjectSummary>(summaries, projects.Page, projects.PageSize, projects.HasNext));
    }

    public async Task<(ProjectOperationOutcome Outcome, PublicProjectDetail? Project)> GetPublishedAsync(
        string? locale,
        int id,
        CancellationToken cancellationToken)
    {
        if (!ProjectLocaleExtensions.TryParse(locale, out var parsedLocale)) return (ProjectOperationOutcome.UnsupportedLocale, null);
        var project = await store.GetPublishedAsync(id, cancellationToken);
        return project is null
            ? (ProjectOperationOutcome.NotFound, null)
            : (ProjectOperationOutcome.Succeeded, ToPublicDetail(project, parsedLocale));
    }

    private async Task<ProjectOperationResult> MutateLifecycleAsync(
        int id,
        long expectedRevision,
        Action<Project, DateTime> mutation,
        CancellationToken cancellationToken)
    {
        if (id < 1 || expectedRevision < 1) return new(ProjectOperationOutcome.InvalidInput);
        var project = await store.GetAdminAsync(id, cancellationToken);
        if (project is null) return new(ProjectOperationOutcome.NotFound);

        try
        {
            mutation(project, RequireUtc(clock.UtcNow));
            return MapMutation(await store.UpdateAsync(project, expectedRevision, cancellationToken), project);
        }
        catch (ProjectDomainException exception)
        {
            return MapDomainException(exception);
        }
    }

    private static ProjectOperationResult MapMutation(ProjectStoreMutationResult result, Project? project = null)
    {
        return result switch
        {
            ProjectStoreMutationResult.Succeeded => new(ProjectOperationOutcome.Succeeded, project),
            ProjectStoreMutationResult.InvalidInput => new(ProjectOperationOutcome.InvalidInput),
            ProjectStoreMutationResult.NotFound => new(ProjectOperationOutcome.NotFound),
            ProjectStoreMutationResult.Conflict => new(ProjectOperationOutcome.StaleRevision),
            ProjectStoreMutationResult.IdentifierConflict => new(ProjectOperationOutcome.IdentifierConflict),
            ProjectStoreMutationResult.OrderConflict => new(ProjectOperationOutcome.OrderConflict),
            _ => new(ProjectOperationOutcome.InvalidInput)
        };
    }

    private static ProjectOperationResult MapDomainException(ProjectDomainException exception)
    {
        return exception.ErrorKey == ProjectErrorKeys.InvalidTransition
            ? new(ProjectOperationOutcome.InvalidTransition)
            : new(ProjectOperationOutcome.InvalidInput);
    }

    private static PublicProjectSummary ToPublicSummary(Project project, ProjectLocale locale)
    {
        return new(project.Id, project.SortOrder, project.Status, project.ImageReference, project.RepositoryUrl, project.LiveUrl,
            project.Technologies, project.LocalizedContent[locale], project.PublishedAt, project.UpdatedAt, project.Revision);
    }

    private static PublicProjectDetail ToPublicDetail(Project project, ProjectLocale locale)
    {
        return new(project.Id, project.SortOrder, project.ImageReference, project.RepositoryUrl, project.LiveUrl,
            project.Technologies, project.LocalizedContent[locale], project.PublishedAt, project.UpdatedAt, project.Revision);
    }

    private static DateTime RequireUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Utc
            ? value
            : throw new ProjectDomainException(ProjectErrorKeys.InvalidInput, "The application clock must return UTC.");
    }

    private static void ValidatePage(int page, int pageSize, int maximumPageSize)
    {
        if (page < 1 || pageSize < 1 || pageSize > maximumPageSize)
        {
            throw new ProjectDomainException(ProjectErrorKeys.InvalidInput, "The requested page is invalid.");
        }
    }
}
