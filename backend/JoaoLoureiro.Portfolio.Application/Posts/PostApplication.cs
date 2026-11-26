using JoaoLoureiro.Portfolio.Domain.Entities;

namespace JoaoLoureiro.Portfolio.Core.Posts;

public static class PostLimits
{
    public const int AdminDefaultPageSize = 20;
    public const int AdminMaxPageSize = 50;
    public const int PublicDefaultPageSize = 10;
    public const int PublicMaxPageSize = 20;
    public const int MaxRequestBytes = 256 * 1024;
}

public interface IClock
{
    DateTime UtcNow { get; }
}

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}

public sealed record PostPage<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    bool HasNext);

public enum PostStoreMutationResult
{
    Succeeded,
    NotFound,
    Conflict,
    SlugConflict
}

public sealed class PostStorageUnavailableException(string operation, Exception innerException)
    : Exception($"The post store could not {operation}.", innerException)
{
    public string Operation { get; } = operation;
}

public interface IPostStore
{
    Task<PostPage<Post>> ListAdminAsync(int page, int pageSize, CancellationToken cancellationToken);

    Task<Post?> GetAdminAsync(Guid id, CancellationToken cancellationToken);

    Task<Post?> GetPublishedBySlugAsync(string slug, CancellationToken cancellationToken);

    Task<PostPage<Post>> ListPublishedAsync(int page, int pageSize, CancellationToken cancellationToken);

    Task<PostStoreMutationResult> AddAsync(Post post, CancellationToken cancellationToken);

    Task<PostStoreMutationResult> UpdateAsync(Post post, long expectedRevision, CancellationToken cancellationToken);

    Task<PostStoreMutationResult> DeleteAsync(Guid id, long expectedRevision, CancellationToken cancellationToken);
}

public sealed record CreatePostCommand(
    string Slug,
    IReadOnlyDictionary<PostLocale, PostLocalizedContent>? Content = null,
    string? CoverImageUrl = null,
    IReadOnlyCollection<string>? Tags = null);

public sealed record UpdatePostCommand(
    long ExpectedRevision,
    IReadOnlyDictionary<PostLocale, PostLocalizedContent> Content,
    string? CoverImageUrl = null,
    IReadOnlyCollection<string>? Tags = null);

public sealed record LifecyclePostCommand(long ExpectedRevision);

public sealed record DeletePostCommand(long ExpectedRevision);

public sealed record PreviewPostCommand(
    string Slug,
    IReadOnlyDictionary<PostLocale, PostLocalizedContent> Content);

public enum PostOperationOutcome
{
    Succeeded,
    NotFound,
    InvalidInput,
    SlugConflict,
    StaleRevision,
    InvalidTransition,
    PublishedDeleteConflict,
    UnsupportedLocale
}

public sealed record PostOperationResult(
    PostOperationOutcome Outcome,
    Post? Post = null);

public sealed record PostPreview(
    string Slug,
    IReadOnlyDictionary<PostLocale, PostLocalizedContent> Content);

public sealed record PostPreviewResult(
    PostOperationOutcome Outcome,
    PostPreview? Preview = null);

public sealed record PublicPostSummary(
    string Slug,
    DateTime PublishedAt,
    string Title,
    string Excerpt,
    string? CoverImageUrl,
    string? CoverImageAlt,
    IReadOnlyList<string> Tags,
    long Revision);

public sealed record PublicPostDetail(
    string Slug,
    DateTime PublishedAt,
    string Title,
    string Excerpt,
    string Body,
    string? CoverImageUrl,
    string? CoverImageAlt,
    IReadOnlyList<string> Tags,
    long Revision);

public sealed class PostApplicationService(IPostStore store, IClock clock)
{
    public async Task<PostPage<Post>> ListAdminAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        ValidatePage(page, pageSize, PostLimits.AdminMaxPageSize);
        return await store.ListAdminAsync(page, pageSize, cancellationToken);
    }

    public Task<Post?> GetAdminAsync(Guid id, CancellationToken cancellationToken)
    {
        return store.GetAdminAsync(id, cancellationToken);
    }

    public async Task<PostOperationResult> CreateDraftAsync(
        CreatePostCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var post = Post.CreateDraft(
                Guid.NewGuid(),
                Post.NormalizeSlug(command.Slug),
                RequireUtc(clock.UtcNow),
                command.Content,
                command.CoverImageUrl,
                command.Tags);

            return MapMutation(await store.AddAsync(post, cancellationToken), post);
        }
        catch (PostDomainException exception)
        {
            return MapDomainException(exception);
        }
    }

    public async Task<PostOperationResult> UpdateAsync(
        Guid id,
        UpdatePostCommand command,
        CancellationToken cancellationToken)
    {
        if (command.ExpectedRevision < 1)
        {
            return new(PostOperationOutcome.InvalidInput);
        }

        var post = await store.GetAdminAsync(id, cancellationToken);
        if (post is null)
        {
            return new(PostOperationOutcome.NotFound);
        }

        try
        {
            post.UpdateContent(command.Content, RequireUtc(clock.UtcNow), command.CoverImageUrl, command.Tags);
            return MapMutation(
                await store.UpdateAsync(post, command.ExpectedRevision, cancellationToken),
                post);
        }
        catch (PostDomainException exception)
        {
            return MapDomainException(exception);
        }
    }

    public async Task<PostOperationResult> PublishAsync(
        Guid id,
        LifecyclePostCommand command,
        CancellationToken cancellationToken)
    {
        return await MutateLifecycleAsync(
            id,
            command.ExpectedRevision,
            static (post, now) => post.Publish(now),
            cancellationToken);
    }

    public async Task<PostOperationResult> UnpublishAsync(
        Guid id,
        LifecyclePostCommand command,
        CancellationToken cancellationToken)
    {
        return await MutateLifecycleAsync(
            id,
            command.ExpectedRevision,
            static (post, now) => post.Unpublish(now),
            cancellationToken);
    }

    public async Task<PostOperationResult> DeleteAsync(
        Guid id,
        DeletePostCommand command,
        CancellationToken cancellationToken)
    {
        if (command.ExpectedRevision < 1)
        {
            return new(PostOperationOutcome.InvalidInput);
        }

        var post = await store.GetAdminAsync(id, cancellationToken);
        if (post is null)
        {
            return new(PostOperationOutcome.NotFound);
        }

        if (!post.CanDelete)
        {
            return new(PostOperationOutcome.PublishedDeleteConflict);
        }

        return MapMutation(
            await store.DeleteAsync(id, command.ExpectedRevision, cancellationToken),
            post);
    }

    public Task<PostPreviewResult> PreviewAsync(PreviewPostCommand command)
    {
        try
        {
            var preview = new PostPreview(
                Post.NormalizeSlug(command.Slug),
                ValidateAndCopyContent(command.Content));
            return Task.FromResult(new PostPreviewResult(PostOperationOutcome.Succeeded, preview));
        }
        catch (PostDomainException exception)
        {
            return Task.FromResult(new PostPreviewResult(MapDomainException(exception).Outcome));
        }
    }

    public async Task<(PostOperationOutcome Outcome, PostPage<PublicPostSummary>? Page)> ListPublishedAsync(
        string? locale,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (!PostLocaleExtensions.TryParse(locale, out var parsedLocale))
        {
            return (PostOperationOutcome.UnsupportedLocale, null);
        }

        ValidatePage(page, pageSize, PostLimits.PublicMaxPageSize);
        var posts = await store.ListPublishedAsync(page, pageSize, cancellationToken);
        var summaries = posts.Items
            .Select(post => new PublicPostSummary(
                post.Slug,
                post.PublishedAt!.Value,
                post.LocalizedContent[parsedLocale].Title,
                post.LocalizedContent[parsedLocale].Excerpt,
                post.CoverImageUrl,
                post.LocalizedContent[parsedLocale].CoverImageAlt,
                post.Tags,
                post.Revision))
            .ToArray();

        return (PostOperationOutcome.Succeeded, new PostPage<PublicPostSummary>(
            summaries,
            posts.Page,
            posts.PageSize,
            posts.HasNext));
    }

    public async Task<(PostOperationOutcome Outcome, PublicPostDetail? Post)> GetPublishedAsync(
        string? locale,
        string slug,
        CancellationToken cancellationToken)
    {
        if (!PostLocaleExtensions.TryParse(locale, out var parsedLocale))
        {
            return (PostOperationOutcome.UnsupportedLocale, null);
        }

        string normalizedSlug;
        try
        {
            normalizedSlug = Post.NormalizeSlug(slug);
        }
        catch (PostDomainException)
        {
            return (PostOperationOutcome.NotFound, null);
        }

        var post = await store.GetPublishedBySlugAsync(normalizedSlug, cancellationToken);
        return post is null
            ? (PostOperationOutcome.NotFound, null)
            : (PostOperationOutcome.Succeeded, new PublicPostDetail(
                post.Slug,
                post.PublishedAt!.Value,
                post.LocalizedContent[parsedLocale].Title,
                post.LocalizedContent[parsedLocale].Excerpt,
                post.LocalizedContent[parsedLocale].Body,
                post.CoverImageUrl,
                post.LocalizedContent[parsedLocale].CoverImageAlt,
                post.Tags,
                post.Revision));
    }

    private async Task<PostOperationResult> MutateLifecycleAsync(
        Guid id,
        long expectedRevision,
        Action<Post, DateTime> mutation,
        CancellationToken cancellationToken)
    {
        if (expectedRevision < 1)
        {
            return new(PostOperationOutcome.InvalidInput);
        }

        var post = await store.GetAdminAsync(id, cancellationToken);
        if (post is null)
        {
            return new(PostOperationOutcome.NotFound);
        }

        try
        {
            mutation(post, RequireUtc(clock.UtcNow));
            return MapMutation(
                await store.UpdateAsync(post, expectedRevision, cancellationToken),
                post);
        }
        catch (PostDomainException exception)
        {
            return MapDomainException(exception);
        }
    }

    private static PostOperationResult MapMutation(PostStoreMutationResult result, Post post)
    {
        return result switch
        {
            PostStoreMutationResult.Succeeded => new(PostOperationOutcome.Succeeded, post),
            PostStoreMutationResult.NotFound => new(PostOperationOutcome.NotFound),
            PostStoreMutationResult.Conflict => new(PostOperationOutcome.StaleRevision),
            PostStoreMutationResult.SlugConflict => new(PostOperationOutcome.SlugConflict),
            _ => new(PostOperationOutcome.InvalidInput)
        };
    }

    private static PostOperationResult MapDomainException(PostDomainException exception)
    {
        return exception.ErrorKey == PostErrorKeys.InvalidTransition
            ? new(PostOperationOutcome.InvalidTransition)
            : new(PostOperationOutcome.InvalidInput);
    }

    private static IReadOnlyDictionary<PostLocale, PostLocalizedContent> ValidateAndCopyContent(
        IReadOnlyDictionary<PostLocale, PostLocalizedContent> content)
    {
        _ = Post.CreateDraft(Guid.NewGuid(), "preview", DateTime.UtcNow, content);
        return new Dictionary<PostLocale, PostLocalizedContent>(content);
    }

    private static DateTime RequireUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Utc
            ? value
            : throw new PostDomainException(PostErrorKeys.InvalidInput, "The application clock must return UTC.");
    }

    private static void ValidatePage(int page, int pageSize, int maximumPageSize)
    {
        if (page < 1 || pageSize < 1 || pageSize > maximumPageSize)
        {
            throw new PostDomainException(PostErrorKeys.InvalidInput, "The requested page is invalid.");
        }
    }
}
