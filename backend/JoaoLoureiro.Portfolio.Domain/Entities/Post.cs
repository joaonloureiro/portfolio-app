using System.Text.RegularExpressions;

namespace JoaoLoureiro.Portfolio.Domain.Entities;

public enum PostLocale
{
    En,
    Pt
}

public enum PostStatus
{
    Draft,
    Published,
    Unpublished
}

public static class PostErrorKeys
{
    public const string InvalidInput = "post_invalid_input";
    public const string InvalidTransition = "post_invalid_transition";
}

public sealed class PostDomainException(string errorKey, string message) : Exception(message)
{
    public string ErrorKey { get; } = errorKey;
}

public static class PostLocaleExtensions
{
    public static string ToCode(this PostLocale locale) => locale switch
    {
        PostLocale.En => "en",
        PostLocale.Pt => "pt",
        _ => throw new ArgumentOutOfRangeException(nameof(locale), locale, null)
    };

    public static bool TryParse(string? value, out PostLocale locale)
    {
        if (string.Equals(value, "en", StringComparison.OrdinalIgnoreCase))
        {
            locale = PostLocale.En;
            return true;
        }

        if (string.Equals(value, "pt", StringComparison.OrdinalIgnoreCase))
        {
            locale = PostLocale.Pt;
            return true;
        }

        locale = default;
        return false;
    }
}

public sealed record PostLocalizedContent
{
    public const int MaxTitleLength = 120;
    public const int MaxExcerptLength = 320;
    public const int MaxBodyLength = 50_000;
    public const int MaxCoverImageAltLength = 200;

    public PostLocalizedContent(string title, string excerpt, string body, string? coverImageAlt = null)
    {
        Title = title ?? throw new ArgumentNullException(nameof(title));
        Excerpt = excerpt ?? throw new ArgumentNullException(nameof(excerpt));
        Body = body ?? throw new ArgumentNullException(nameof(body));
        CoverImageAlt = coverImageAlt;

        if (Title.Length > MaxTitleLength ||
            Excerpt.Length > MaxExcerptLength ||
            Body.Length > MaxBodyLength ||
            (CoverImageAlt?.Length ?? 0) > MaxCoverImageAltLength)
        {
            throw new PostDomainException(PostErrorKeys.InvalidInput, "Localized post content exceeds its length limit.");
        }
    }

    public string Title { get; }

    public string Excerpt { get; }

    public string Body { get; }

    public string? CoverImageAlt { get; }

    public bool IsComplete =>
        !string.IsNullOrWhiteSpace(Title) &&
        !string.IsNullOrWhiteSpace(Excerpt) &&
        !string.IsNullOrWhiteSpace(Body);
}

public sealed class Post
{
    public const int MaxSlugLength = 80;
    public const int MaxCoverImageUrlLength = 2_048;
    public const int MaxTags = 8;
    public const int MaxTagLength = 32;

    private static readonly Regex SlugPattern = new(
        "^[a-z0-9]+(?:-[a-z0-9]+)*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex TagPattern = new(
        "^[a-z0-9]+(?:-[a-z0-9]+)*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly HashSet<string> ReservedSlugs = new(StringComparer.Ordinal)
    {
        "admin", "api", "about", "contact", "en", "favicon", "health", "notes",
        "posts", "project", "pt", "robots", "sitemap"
    };

    private static readonly PostLocale[] SupportedLocales = [PostLocale.En, PostLocale.Pt];
    private readonly Dictionary<PostLocale, PostLocalizedContent> localizedContent;
    private IReadOnlyList<string> tags = Array.Empty<string>();

    private Post(
        Guid id,
        string slug,
        IReadOnlyDictionary<PostLocale, PostLocalizedContent> localizedContent,
        PostStatus status,
        DateTime? publishedAt,
        DateTime updatedAt,
        long revision,
        string? coverImageUrl,
        IReadOnlyCollection<string>? tags)
    {
        if (id == Guid.Empty || revision < 1)
        {
            throw new PostDomainException(PostErrorKeys.InvalidInput, "A post identifier and revision are required.");
        }

        ValidateSlug(slug);
        ValidateLocalizedContent(localizedContent);
        coverImageUrl = NormalizeCoverImageUrl(coverImageUrl);
        var normalizedTags = NormalizeTags(tags);
        EnsureUtc(updatedAt);
        if (publishedAt.HasValue)
        {
            EnsureUtc(publishedAt.Value);
        }

        if (status == PostStatus.Published &&
            (!publishedAt.HasValue || !HasCompleteLocalizedContent(localizedContent)))
        {
            throw new PostDomainException(PostErrorKeys.InvalidInput, "A published post must have complete content and a publication timestamp.");
        }

        Id = id;
        Slug = slug;
        this.localizedContent = new Dictionary<PostLocale, PostLocalizedContent>(localizedContent);
        Status = status;
        PublishedAt = publishedAt;
        UpdatedAt = updatedAt;
        Revision = revision;
        CoverImageUrl = coverImageUrl;
        this.tags = normalizedTags;
    }

    public Guid Id { get; }

    public string Slug { get; }

    public PostStatus Status { get; private set; }

    public DateTime? PublishedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public long Revision { get; private set; }

    public IReadOnlyDictionary<PostLocale, PostLocalizedContent> LocalizedContent => localizedContent;

    public string? CoverImageUrl { get; private set; }

    public IReadOnlyList<string> Tags => tags;

    public static Post CreateDraft(
        Guid id,
        string slug,
        DateTime updatedAt,
        IReadOnlyDictionary<PostLocale, PostLocalizedContent>? localizedContent = null,
        string? coverImageUrl = null,
        IReadOnlyCollection<string>? tags = null)
    {
        var content = localizedContent is null
            ? CreateEmptyLocalizedContent()
            : localizedContent;

        return new Post(id, slug, content, PostStatus.Draft, null, updatedAt, revision: 1, coverImageUrl, tags);
    }

    public static Post Rehydrate(
        Guid id,
        string slug,
        IReadOnlyDictionary<PostLocale, PostLocalizedContent> localizedContent,
        PostStatus status,
        DateTime? publishedAt,
        DateTime updatedAt,
        long revision,
        string? coverImageUrl = null,
        IReadOnlyCollection<string>? tags = null)
    {
        return new Post(id, slug, localizedContent, status, publishedAt, updatedAt, revision, coverImageUrl, tags);
    }

    public static string NormalizeSlug(string? slug)
    {
        if (slug is null)
        {
            throw new PostDomainException(PostErrorKeys.InvalidInput, "A slug is required.");
        }

        ValidateSlug(slug);
        return slug;
    }

    public static void ValidateSlug(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug) ||
            slug.Length > MaxSlugLength ||
            !string.Equals(slug, slug.Trim(), StringComparison.Ordinal) ||
            !string.Equals(slug, slug.ToLowerInvariant(), StringComparison.Ordinal) ||
            !SlugPattern.IsMatch(slug) ||
            ReservedSlugs.Contains(slug))
        {
            throw new PostDomainException(PostErrorKeys.InvalidInput, "The slug is invalid or reserved.");
        }
    }

    public void UpdateContent(
        IReadOnlyDictionary<PostLocale, PostLocalizedContent> content,
        DateTime updatedAt,
        string? coverImageUrl = null,
        IReadOnlyCollection<string>? tags = null)
    {
        ValidateLocalizedContent(content);
        var normalizedCoverImageUrl = NormalizeCoverImageUrl(coverImageUrl);
        var normalizedTags = NormalizeTags(tags);
        if (Status == PostStatus.Published && !HasCompleteLocalizedContent(content))
        {
            throw new PostDomainException(PostErrorKeys.InvalidInput, "A published post must retain complete content in both locales.");
        }

        EnsureUtc(updatedAt);
        localizedContent.Clear();
        foreach (var entry in content)
        {
            localizedContent.Add(entry.Key, entry.Value);
        }

        CoverImageUrl = normalizedCoverImageUrl;
        this.tags = normalizedTags;
        Touch(updatedAt);
    }

    public void Publish(DateTime publishedAt)
    {
        if (Status == PostStatus.Published)
        {
            throw new PostDomainException(PostErrorKeys.InvalidTransition, "The post is already published.");
        }

        if (!HasCompleteLocalizedContent(localizedContent))
        {
            throw new PostDomainException(PostErrorKeys.InvalidInput, "Both locales must be complete before publication.");
        }

        EnsureUtc(publishedAt);
        Status = PostStatus.Published;
        PublishedAt ??= publishedAt;
        Touch(publishedAt);
    }

    public void Unpublish(DateTime updatedAt)
    {
        if (Status != PostStatus.Published)
        {
            throw new PostDomainException(PostErrorKeys.InvalidTransition, "Only a published post can be unpublished.");
        }

        EnsureUtc(updatedAt);
        Status = PostStatus.Unpublished;
        Touch(updatedAt);
    }

    public bool CanDelete => Status is PostStatus.Draft or PostStatus.Unpublished;

    private void Touch(DateTime updatedAt)
    {
        UpdatedAt = updatedAt;
        Revision++;
    }

    private static IReadOnlyDictionary<PostLocale, PostLocalizedContent> CreateEmptyLocalizedContent()
    {
        return SupportedLocales.ToDictionary(
            locale => locale,
            _ => new PostLocalizedContent(string.Empty, string.Empty, string.Empty));
    }

    private static void ValidateLocalizedContent(IReadOnlyDictionary<PostLocale, PostLocalizedContent> content)
    {
        if (content is null ||
            content.Count != SupportedLocales.Length ||
            SupportedLocales.Any(locale => !content.ContainsKey(locale)) ||
            content.Values.Any(value => value is null))
        {
            throw new PostDomainException(PostErrorKeys.InvalidInput, "Post content must include English and Portuguese locales.");
        }
    }

    private static bool HasCompleteLocalizedContent(IReadOnlyDictionary<PostLocale, PostLocalizedContent> content)
    {
        return SupportedLocales.All(locale => content.TryGetValue(locale, out var value) && value.IsComplete);
    }

    private static string? NormalizeCoverImageUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var candidate = value.Trim();
        if (candidate.Length > MaxCoverImageUrlLength ||
            !Uri.TryCreate(candidate, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps ||
            string.IsNullOrWhiteSpace(uri.Host) ||
            !string.IsNullOrEmpty(uri.UserInfo))
        {
            throw new PostDomainException(PostErrorKeys.InvalidInput, "The cover image URL must be a valid HTTPS URL.");
        }

        return uri.AbsoluteUri;
    }

    private static IReadOnlyList<string> NormalizeTags(IReadOnlyCollection<string>? values)
    {
        if (values is null) return Array.Empty<string>();
        if (values.Count > MaxTags)
        {
            throw new PostDomainException(PostErrorKeys.InvalidInput, "A post cannot have more than eight tags.");
        }

        var normalized = new List<string>(values.Count);
        foreach (var value in values)
        {
            var tag = value?.Trim().ToLowerInvariant().Replace(' ', '-');
            if (string.IsNullOrEmpty(tag) || tag.Length > MaxTagLength || !TagPattern.IsMatch(tag) ||
                normalized.Contains(tag, StringComparer.Ordinal))
            {
                throw new PostDomainException(PostErrorKeys.InvalidInput, "Post tags must be unique lowercase words separated by hyphens.");
            }

            normalized.Add(tag);
        }

        return normalized;
    }

    private static DateTime RequireUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Utc
            ? value
            : throw new PostDomainException(PostErrorKeys.InvalidInput, "The application clock must return UTC.");
    }

    private static void EnsureUtc(DateTime value)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new PostDomainException(PostErrorKeys.InvalidInput, "Post timestamps must be UTC.");
        }
    }
}
