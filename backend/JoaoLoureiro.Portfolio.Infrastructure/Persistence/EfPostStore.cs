using JoaoLoureiro.Portfolio.Core.Posts;
using JoaoLoureiro.Portfolio.Domain.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.Text.Json;

namespace JoaoLoureiro.Portfolio.Infrastructure.Persistence;

public sealed class EfPostStore(PortfolioDbContext dbContext) : IPostStore
{
    public async Task<PostPage<Post>> ListAdminAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        try
        {
            var records = await dbContext.Posts
                .AsNoTracking()
                .Include(post => post.LocalizedContent)
                .OrderByDescending(post => post.UpdatedAt)
                .ThenBy(post => post.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize + 1)
                .ToListAsync(cancellationToken);

            return CreatePage(records, page, pageSize);
        }
        catch (Exception exception) when (IsStorageException(exception))
        {
            throw CreateStorageException("list posts", exception);
        }
    }

    public async Task<Post?> GetAdminAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var record = await dbContext.Posts
                .AsNoTracking()
                .Include(post => post.LocalizedContent)
                .SingleOrDefaultAsync(post => post.Id == id, cancellationToken);

            return record is null ? null : ToDomain(record);
        }
        catch (Exception exception) when (IsStorageException(exception))
        {
            throw CreateStorageException("read a post", exception);
        }
    }

    public async Task<Post?> GetPublishedBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        try
        {
            var record = await dbContext.Posts
                .AsNoTracking()
                .Include(post => post.LocalizedContent)
                .SingleOrDefaultAsync(
                    post => post.Slug == slug && post.Status == nameof(PostStatus.Published),
                    cancellationToken);

            return record is null ? null : ToDomain(record);
        }
        catch (Exception exception) when (IsStorageException(exception))
        {
            throw CreateStorageException("read a published post", exception);
        }
    }

    public async Task<PostPage<Post>> ListPublishedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        try
        {
            var records = await dbContext.Posts
                .AsNoTracking()
                .Include(post => post.LocalizedContent)
                .Where(post => post.Status == nameof(PostStatus.Published))
                .OrderByDescending(post => post.PublishedAt)
                .ThenBy(post => post.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize + 1)
                .ToListAsync(cancellationToken);

            return CreatePage(records, page, pageSize);
        }
        catch (Exception exception) when (IsStorageException(exception))
        {
            throw CreateStorageException("list published posts", exception);
        }
    }

    public async Task<PostStoreMutationResult> AddAsync(
        Post post,
        CancellationToken cancellationToken)
    {
        try
        {
            dbContext.Posts.Add(ToRecord(post));
            await dbContext.SaveChangesAsync(cancellationToken);
            return PostStoreMutationResult.Succeeded;
        }
        catch (DbUpdateException exception) when (IsUniqueSlugViolation(exception))
        {
            dbContext.ChangeTracker.Clear();
            return PostStoreMutationResult.SlugConflict;
        }
        catch (Exception exception) when (IsStorageException(exception))
        {
            dbContext.ChangeTracker.Clear();
            throw CreateStorageException("create a post", exception);
        }
    }

    public async Task<PostStoreMutationResult> UpdateAsync(
        Post post,
        long expectedRevision,
        CancellationToken cancellationToken)
    {
        try
        {
            var record = await dbContext.Posts
                .Include(existing => existing.LocalizedContent)
                .SingleOrDefaultAsync(existing => existing.Id == post.Id, cancellationToken);

            if (record is null)
            {
                return PostStoreMutationResult.NotFound;
            }

            ApplyPost(record, post);
            dbContext.Entry(record).Property(existing => existing.Revision).OriginalValue = expectedRevision;
            await dbContext.SaveChangesAsync(cancellationToken);
            return PostStoreMutationResult.Succeeded;
        }
        catch (DbUpdateConcurrencyException)
        {
            dbContext.ChangeTracker.Clear();
            return PostStoreMutationResult.Conflict;
        }
        catch (DbUpdateException exception) when (IsUniqueSlugViolation(exception))
        {
            dbContext.ChangeTracker.Clear();
            return PostStoreMutationResult.SlugConflict;
        }
        catch (Exception exception) when (IsStorageException(exception))
        {
            dbContext.ChangeTracker.Clear();
            throw CreateStorageException("update a post", exception);
        }
    }

    public async Task<PostStoreMutationResult> DeleteAsync(
        Guid id,
        long expectedRevision,
        CancellationToken cancellationToken)
    {
        try
        {
            var record = await dbContext.Posts
                .Include(existing => existing.LocalizedContent)
                .SingleOrDefaultAsync(existing => existing.Id == id, cancellationToken);

            if (record is null)
            {
                return PostStoreMutationResult.NotFound;
            }

            dbContext.Posts.Remove(record);
            dbContext.Entry(record).Property(existing => existing.Revision).OriginalValue = expectedRevision;
            await dbContext.SaveChangesAsync(cancellationToken);
            return PostStoreMutationResult.Succeeded;
        }
        catch (DbUpdateConcurrencyException)
        {
            dbContext.ChangeTracker.Clear();
            return PostStoreMutationResult.Conflict;
        }
        catch (Exception exception) when (IsStorageException(exception))
        {
            dbContext.ChangeTracker.Clear();
            throw CreateStorageException("delete a post", exception);
        }
    }

    private static PostPage<Post> CreatePage(
        IReadOnlyList<PostRecord> records,
        int page,
        int pageSize)
    {
        var hasNext = records.Count > pageSize;
        var posts = records
            .Take(pageSize)
            .Select(ToDomain)
            .ToArray();

        return new PostPage<Post>(posts, page, pageSize, hasNext);
    }

    private static PostRecord ToRecord(Post post)
    {
        var record = new PostRecord
        {
            Id = post.Id,
            Slug = post.Slug,
            Status = post.Status.ToString(),
            PublishedAt = post.PublishedAt,
            UpdatedAt = post.UpdatedAt,
            Revision = post.Revision,
            CoverImageUrl = post.CoverImageUrl,
            TagsJson = JsonSerializer.Serialize(post.Tags)
        };

        foreach (var (locale, content) in post.LocalizedContent)
        {
            record.LocalizedContent.Add(ToRecord(post.Id, locale, content));
        }

        return record;
    }

    private static void ApplyPost(PostRecord record, Post post)
    {
        record.Status = post.Status.ToString();
        record.PublishedAt = post.PublishedAt;
        record.UpdatedAt = post.UpdatedAt;
        record.Revision = post.Revision;
        record.CoverImageUrl = post.CoverImageUrl;
        record.TagsJson = JsonSerializer.Serialize(post.Tags);

        var desired = post.LocalizedContent
            .ToDictionary(entry => entry.Key.ToCode(), entry => entry.Value, StringComparer.Ordinal);

        foreach (var existing in record.LocalizedContent.ToArray())
        {
            if (!desired.TryGetValue(existing.Locale, out var content))
            {
                record.LocalizedContent.Remove(existing);
                continue;
            }

            existing.Title = content.Title;
            existing.Excerpt = content.Excerpt;
            existing.Body = content.Body;
            existing.CoverImageAlt = content.CoverImageAlt;
            desired.Remove(existing.Locale);
        }

        foreach (var (locale, content) in desired)
        {
            record.LocalizedContent.Add(new PostLocalizedContentRecord
            {
                PostId = record.Id,
                Locale = locale,
                Title = content.Title,
                Excerpt = content.Excerpt,
                Body = content.Body,
                CoverImageAlt = content.CoverImageAlt
            });
        }
    }

    private static PostLocalizedContentRecord ToRecord(
        Guid postId,
        PostLocale locale,
        PostLocalizedContent content)
    {
        return new PostLocalizedContentRecord
        {
            PostId = postId,
            Locale = locale.ToCode(),
            Title = content.Title,
            Excerpt = content.Excerpt,
            Body = content.Body,
            CoverImageAlt = content.CoverImageAlt
        };
    }

    private static Post ToDomain(PostRecord record)
    {
        var localizedContent = record.LocalizedContent.ToDictionary(
            content => PostLocaleExtensions.TryParse(content.Locale, out var locale)
                ? locale
                : throw new PostStorageUnavailableException("read a post with an unsupported locale", new InvalidOperationException()),
            content => new PostLocalizedContent(content.Title, content.Excerpt, content.Body, content.CoverImageAlt));

        if (!Enum.TryParse<PostStatus>(record.Status, ignoreCase: false, out var status))
        {
            throw new PostStorageUnavailableException("read a post with an unsupported status", new InvalidOperationException());
        }

        IReadOnlyList<string> tags;
        try
        {
            tags = JsonSerializer.Deserialize<string[]>(record.TagsJson) ?? [];
        }
        catch (JsonException exception)
        {
            throw new PostStorageUnavailableException("read a post with invalid tags", exception);
        }

        return Post.Rehydrate(
            record.Id,
            record.Slug,
            localizedContent,
            status,
            record.PublishedAt,
            record.UpdatedAt,
            record.Revision,
            record.CoverImageUrl,
            tags);
    }

    private static bool IsUniqueSlugViolation(DbUpdateException exception)
    {
        return exception.InnerException is SqliteException sqliteException &&
               sqliteException.SqliteErrorCode == 19 &&
               (sqliteException.Message.Contains("Posts.Slug", StringComparison.OrdinalIgnoreCase) ||
                sqliteException.Message.Contains("IX_Posts_Slug", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsStorageException(Exception exception)
    {
        return exception is DbUpdateException or DbUpdateConcurrencyException or DbException;
    }

    private static PostStorageUnavailableException CreateStorageException(string operation, Exception exception)
    {
        return new PostStorageUnavailableException(operation, exception);
    }
}
