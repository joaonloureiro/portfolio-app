using JoaoLoureiro.Portfolio.Core.Projects;
using JoaoLoureiro.Portfolio.Domain.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace JoaoLoureiro.Portfolio.Infrastructure.Persistence;

public sealed class EfProjectStore(PortfolioDbContext dbContext) : IProjectStore
{
    public async Task<int> GetNextIdAsync(CancellationToken cancellationToken)
    {
        try
        {
            var highestId = await dbContext.Projects.MaxAsync(project => (int?)project.Id, cancellationToken);
            return (highestId ?? 0) + 1;
        }
        catch (Exception exception) when (IsStorageException(exception))
        {
            throw CreateStorageException("allocate a project identifier", exception);
        }
    }

    public async Task<ProjectPage<Project>> ListAdminAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        try
        {
            var records = await QueryProjects()
                .OrderBy(project => project.SortOrder)
                .ThenBy(project => project.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize + 1)
                .ToListAsync(cancellationToken);
            return CreatePage(records, page, pageSize);
        }
        catch (Exception exception) when (IsStorageException(exception))
        {
            throw CreateStorageException("list projects", exception);
        }
    }

    public async Task<Project?> GetAdminAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            var record = await QueryProjects().SingleOrDefaultAsync(project => project.Id == id, cancellationToken);
            return record is null ? null : ToDomain(record);
        }
        catch (Exception exception) when (IsStorageException(exception))
        {
            throw CreateStorageException("read a project", exception);
        }
    }

    public async Task<ProjectPage<Project>> ListPublishedAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        try
        {
            var records = await QueryProjects()
                .Where(project => project.Status == nameof(ProjectStatus.Published))
                .OrderBy(project => project.SortOrder)
                .ThenBy(project => project.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize + 1)
                .ToListAsync(cancellationToken);
            return CreatePage(records, page, pageSize);
        }
        catch (Exception exception) when (IsStorageException(exception))
        {
            throw CreateStorageException("list published projects", exception);
        }
    }

    public async Task<Project?> GetPublishedAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            var record = await QueryProjects()
                .Where(project => project.Status == nameof(ProjectStatus.Published))
                .SingleOrDefaultAsync(project => project.Id == id, cancellationToken);
            return record is null ? null : ToDomain(record);
        }
        catch (Exception exception) when (IsStorageException(exception))
        {
            throw CreateStorageException("read a published project", exception);
        }
    }

    public async Task<ProjectStoreMutationResult> AddAsync(Project project, CancellationToken cancellationToken)
    {
        try
        {
            dbContext.Projects.Add(ToRecord(project));
            await dbContext.SaveChangesAsync(cancellationToken);
            return ProjectStoreMutationResult.Succeeded;
        }
        catch (DbUpdateException exception) when (IsIdentifierConflict(exception))
        {
            dbContext.ChangeTracker.Clear();
            return ProjectStoreMutationResult.IdentifierConflict;
        }
        catch (DbUpdateException exception) when (IsOrderConflict(exception))
        {
            dbContext.ChangeTracker.Clear();
            return ProjectStoreMutationResult.OrderConflict;
        }
        catch (Exception exception) when (IsStorageException(exception))
        {
            dbContext.ChangeTracker.Clear();
            throw CreateStorageException("create a project", exception);
        }
    }

    public async Task<ProjectStoreMutationResult> UpdateAsync(Project project, long expectedRevision, CancellationToken cancellationToken)
    {
        try
        {
            var record = await QueryProjects().SingleOrDefaultAsync(existing => existing.Id == project.Id, cancellationToken);
            if (record is null) return ProjectStoreMutationResult.NotFound;

            ApplyProject(record, project);
            dbContext.Entry(record).Property(existing => existing.Revision).OriginalValue = expectedRevision;
            await dbContext.SaveChangesAsync(cancellationToken);
            return ProjectStoreMutationResult.Succeeded;
        }
        catch (DbUpdateConcurrencyException)
        {
            dbContext.ChangeTracker.Clear();
            return ProjectStoreMutationResult.Conflict;
        }
        catch (DbUpdateException exception) when (IsIdentifierConflict(exception))
        {
            dbContext.ChangeTracker.Clear();
            return ProjectStoreMutationResult.IdentifierConflict;
        }
        catch (DbUpdateException exception) when (IsOrderConflict(exception))
        {
            dbContext.ChangeTracker.Clear();
            return ProjectStoreMutationResult.OrderConflict;
        }
        catch (Exception exception) when (IsStorageException(exception))
        {
            dbContext.ChangeTracker.Clear();
            throw CreateStorageException("update a project", exception);
        }
    }

    public async Task<ProjectStoreMutationResult> ReorderAsync(IReadOnlyList<ProjectOrderChange> changes, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var records = await dbContext.Projects.ToListAsync(cancellationToken);
            if (records.Count != changes.Count || records.Any(record => changes.All(change => change.Id != record.Id)))
            {
                return ProjectStoreMutationResult.InvalidInput;
            }

            if (changes.Any(change => records.Single(record => record.Id == change.Id).Revision != change.ExpectedRevision))
            {
                return ProjectStoreMutationResult.Conflict;
            }

            foreach (var record in records)
            {
                record.SortOrder = -record.Id;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            var now = DateTime.UtcNow;
            foreach (var change in changes)
            {
                var record = records.Single(project => project.Id == change.Id);
                record.SortOrder = change.SortOrder;
                record.Revision++;
                record.UpdatedAt = now;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return ProjectStoreMutationResult.Succeeded;
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            dbContext.ChangeTracker.Clear();
            return ProjectStoreMutationResult.Conflict;
        }
        catch (Exception exception) when (IsStorageException(exception))
        {
            await transaction.RollbackAsync(CancellationToken.None);
            dbContext.ChangeTracker.Clear();
            throw CreateStorageException("reorder projects", exception);
        }
    }

    public async Task<ProjectStoreMutationResult> DeleteAsync(int id, long expectedRevision, CancellationToken cancellationToken)
    {
        try
        {
            var record = await QueryProjects().SingleOrDefaultAsync(existing => existing.Id == id, cancellationToken);
            if (record is null) return ProjectStoreMutationResult.NotFound;

            dbContext.Projects.Remove(record);
            dbContext.Entry(record).Property(existing => existing.Revision).OriginalValue = expectedRevision;
            await dbContext.SaveChangesAsync(cancellationToken);
            return ProjectStoreMutationResult.Succeeded;
        }
        catch (DbUpdateConcurrencyException)
        {
            dbContext.ChangeTracker.Clear();
            return ProjectStoreMutationResult.Conflict;
        }
        catch (Exception exception) when (IsStorageException(exception))
        {
            dbContext.ChangeTracker.Clear();
            throw CreateStorageException("delete a project", exception);
        }
    }

    private IQueryable<ProjectRecord> QueryProjects()
    {
        return dbContext.Projects
            .Include(project => project.LocalizedContent)
            .ThenInclude(content => content.Features)
            .Include(project => project.Technologies);
    }

    private static ProjectPage<Project> CreatePage(IReadOnlyList<ProjectRecord> records, int page, int pageSize)
    {
        var hasNext = records.Count > pageSize;
        return new ProjectPage<Project>(records.Take(pageSize).Select(ToDomain).ToArray(), page, pageSize, hasNext);
    }

    private static ProjectRecord ToRecord(Project project)
    {
        var record = new ProjectRecord
        {
            Id = project.Id,
            SortOrder = project.SortOrder,
            Status = project.Status.ToString(),
            ImageReference = project.ImageReference,
            RepositoryUrl = project.RepositoryUrl,
            LiveUrl = project.LiveUrl,
            PublishedAt = project.PublishedAt,
            UpdatedAt = project.UpdatedAt,
            Revision = project.Revision
        };
        ApplyLocalizedContent(record, project);
        return record;
    }

    private void ApplyProject(ProjectRecord record, Project project)
    {
        record.SortOrder = project.SortOrder;
        record.Status = project.Status.ToString();
        record.ImageReference = project.ImageReference;
        record.RepositoryUrl = project.RepositoryUrl;
        record.LiveUrl = project.LiveUrl;
        record.PublishedAt = project.PublishedAt;
        record.UpdatedAt = project.UpdatedAt;
        record.Revision = project.Revision;

        var existingContent = record.LocalizedContent.ToArray();
        dbContext.ProjectLocalizedContent.RemoveRange(existingContent);
        record.LocalizedContent.Clear();
        var existingTechnologies = record.Technologies.ToArray();
        dbContext.ProjectTechnologies.RemoveRange(existingTechnologies);
        record.Technologies.Clear();
        ApplyLocalizedContent(record, project);
    }

    private static void ApplyLocalizedContent(ProjectRecord record, Project project)
    {
        foreach (var (locale, content) in project.LocalizedContent)
        {
            var localized = new ProjectLocalizedContentRecord
            {
                ProjectId = project.Id,
                Locale = locale.ToCode(),
                Title = content.Title,
                Summary = content.Summary,
                Details = content.Details,
                FeatureHeading = content.FeatureHeading
            };
            localized.Features.AddRange(content.Features.Select((value, index) => new ProjectFeatureRecord
            {
                ProjectId = project.Id,
                Locale = locale.ToCode(),
                Position = index + 1,
                Value = value
            }));
            record.LocalizedContent.Add(localized);
        }

        record.Technologies.AddRange(project.Technologies.Select((value, index) => new ProjectTechnologyRecord
        {
            ProjectId = project.Id,
            Position = index + 1,
            Value = value
        }));
    }

    private static Project ToDomain(ProjectRecord record)
    {
        if (!Enum.TryParse<ProjectStatus>(record.Status, ignoreCase: false, out var status))
        {
            throw new ProjectStorageUnavailableException("read a project with an unsupported status", new InvalidOperationException());
        }

        var content = record.LocalizedContent.ToDictionary(
            value => ProjectLocaleExtensions.TryParse(value.Locale, out var locale)
                ? locale
                : throw new ProjectStorageUnavailableException("read a project with an unsupported locale", new InvalidOperationException()),
            value => new ProjectLocalizedContent(value.Title, value.Summary, value.Details, value.FeatureHeading,
                value.Features.OrderBy(feature => feature.Position).Select(feature => feature.Value).ToArray()));
        var technologies = record.Technologies.OrderBy(technology => technology.Position).Select(technology => technology.Value).ToArray();

        return Project.Rehydrate(record.Id, record.SortOrder, record.ImageReference, record.RepositoryUrl, record.LiveUrl,
            technologies, content, status, record.PublishedAt, record.UpdatedAt, record.Revision);
    }

    private static bool IsIdentifierConflict(DbUpdateException exception)
    {
        return exception.InnerException is SqliteException sqliteException &&
               sqliteException.SqliteErrorCode == 19 &&
               sqliteException.Message.Contains("Projects.Id", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsOrderConflict(DbUpdateException exception)
    {
        return exception.InnerException is SqliteException sqliteException &&
               sqliteException.SqliteErrorCode == 19 &&
               (sqliteException.Message.Contains("Projects.SortOrder", StringComparison.OrdinalIgnoreCase) ||
                sqliteException.Message.Contains("IX_Projects_SortOrder", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsStorageException(Exception exception)
    {
        return exception is DbUpdateException or DbUpdateConcurrencyException or DbException;
    }

    private static ProjectStorageUnavailableException CreateStorageException(string operation, Exception exception)
    {
        return new ProjectStorageUnavailableException(operation, exception);
    }
}
