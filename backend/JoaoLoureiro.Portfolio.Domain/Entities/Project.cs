namespace JoaoLoureiro.Portfolio.Domain.Entities;

public enum ProjectLocale
{
    En,
    Pt
}

public enum ProjectStatus
{
    Draft,
    Published,
    Unpublished
}

public static class ProjectErrorKeys
{
    public const string InvalidInput = "project_invalid_input";
    public const string InvalidTransition = "project_invalid_transition";
    public const string NotFound = "project_not_found";
    public const string IdentifierConflict = "project_identifier_conflict";
    public const string StaleRevision = "project_stale_revision";
    public const string PublishedDeleteConflict = "project_published_delete_conflict";
    public const string StorageUnavailable = "project_storage_unavailable";
    public const string OrderConflict = "project_order_conflict";
}

public sealed class ProjectDomainException(string errorKey, string message) : Exception(message)
{
    public string ErrorKey { get; } = errorKey;
}

public static class ProjectLocaleExtensions
{
    public static string ToCode(this ProjectLocale locale) => locale switch
    {
        ProjectLocale.En => "en",
        ProjectLocale.Pt => "pt",
        _ => throw new ArgumentOutOfRangeException(nameof(locale), locale, null)
    };

    public static bool TryParse(string? value, out ProjectLocale locale)
    {
        if (string.Equals(value, "en", StringComparison.OrdinalIgnoreCase))
        {
            locale = ProjectLocale.En;
            return true;
        }

        if (string.Equals(value, "pt", StringComparison.OrdinalIgnoreCase))
        {
            locale = ProjectLocale.Pt;
            return true;
        }

        locale = default;
        return false;
    }
}

public sealed record ProjectLocalizedContent
{
    public const int MaxTitleLength = 120;
    public const int MaxSummaryLength = 320;
    public const int MaxDetailsLength = 10_000;
    public const int MaxFeatureHeadingLength = 120;
    public const int MaxFeatureLength = 500;
    public const int MaxFeatureCount = 20;

    public ProjectLocalizedContent(
        string title,
        string summary,
        string details,
        string featureHeading,
        IReadOnlyList<string> features)
    {
        Title = RequireLength(title, MaxTitleLength, nameof(title));
        Summary = RequireLength(summary, MaxSummaryLength, nameof(summary));
        Details = RequireLength(details, MaxDetailsLength, nameof(details));
        FeatureHeading = RequireLength(featureHeading, MaxFeatureHeadingLength, nameof(featureHeading));

        if (features is null || features.Count is < 1 or > MaxFeatureCount)
        {
            throw new ProjectDomainException(ProjectErrorKeys.InvalidInput, "A project must contain between one and twenty features per locale.");
        }

        Features = features.Select(feature => RequireLength(feature, MaxFeatureLength, nameof(features))).ToArray();
    }

    public string Title { get; }

    public string Summary { get; }

    public string Details { get; }

    public string FeatureHeading { get; }

    public IReadOnlyList<string> Features { get; }

    public bool IsComplete =>
        !string.IsNullOrWhiteSpace(Title) &&
        !string.IsNullOrWhiteSpace(Summary) &&
        !string.IsNullOrWhiteSpace(Details) &&
        !string.IsNullOrWhiteSpace(FeatureHeading) &&
        Features.Count > 0 &&
        Features.All(feature => !string.IsNullOrWhiteSpace(feature));

    private static string RequireLength(string? value, int maximumLength, string fieldName)
    {
        if (value is null || value.Length > maximumLength)
        {
            throw new ProjectDomainException(ProjectErrorKeys.InvalidInput, $"Project field '{fieldName}' exceeds its length limit.");
        }

        return value;
    }
}

public sealed class Project
{
    public const int MaxSortOrder = 1_000;
    public const int MaxTechnologyCount = 20;
    public const int MaxTechnologyLength = 60;
    public const int MaxUrlLength = 2_048;
    public const int MaxImageReferenceLength = 512;

    private static readonly ProjectLocale[] SupportedLocales = [ProjectLocale.En, ProjectLocale.Pt];
    private static readonly HashSet<string> ApprovedHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "github.com",
        "joaoloureiro.dev.br",
        "happy.joaoloureiro.dev.br"
    };

    private readonly List<string> technologies;
    private readonly Dictionary<ProjectLocale, ProjectLocalizedContent> localizedContent;

    private Project(
        int id,
        int sortOrder,
        string imageReference,
        string? repositoryUrl,
        string? liveUrl,
        IReadOnlyList<string> technologies,
        IReadOnlyDictionary<ProjectLocale, ProjectLocalizedContent> localizedContent,
        ProjectStatus status,
        DateTime? publishedAt,
        DateTime updatedAt,
        long revision)
    {
        if (id < 1 || revision < 1)
        {
            throw new ProjectDomainException(ProjectErrorKeys.InvalidInput, "A project identifier and revision are required.");
        }

        ValidateSortOrder(sortOrder);
        ValidateImageReference(imageReference);
        ValidateOptionalUrl(repositoryUrl, nameof(repositoryUrl));
        ValidateOptionalUrl(liveUrl, nameof(liveUrl));
        this.technologies = CopyTechnologies(technologies);
        ValidateLocalizedContent(localizedContent);
        EnsureUtc(updatedAt);
        if (publishedAt.HasValue) EnsureUtc(publishedAt.Value);

        if (status == ProjectStatus.Published && !HasCompleteLocalizedContent(localizedContent))
        {
            throw new ProjectDomainException(ProjectErrorKeys.InvalidInput, "A published project must have complete content in both locales.");
        }

        Id = id;
        SortOrder = sortOrder;
        ImageReference = imageReference;
        RepositoryUrl = repositoryUrl;
        LiveUrl = liveUrl;
        this.localizedContent = new Dictionary<ProjectLocale, ProjectLocalizedContent>(localizedContent);
        Status = status;
        PublishedAt = publishedAt;
        UpdatedAt = updatedAt;
        Revision = revision;
    }

    public int Id { get; }

    public int SortOrder { get; private set; }

    public ProjectStatus Status { get; private set; }

    public string ImageReference { get; private set; }

    public string? RepositoryUrl { get; private set; }

    public string? LiveUrl { get; private set; }

    public IReadOnlyList<string> Technologies => technologies;

    public DateTime? PublishedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public long Revision { get; private set; }

    public IReadOnlyDictionary<ProjectLocale, ProjectLocalizedContent> LocalizedContent => localizedContent;

    public bool CanDelete => Status == ProjectStatus.Unpublished;

    public static Project CreateDraft(
        int id,
        int sortOrder,
        string imageReference,
        string? repositoryUrl,
        string? liveUrl,
        IReadOnlyList<string> technologies,
        DateTime updatedAt,
        IReadOnlyDictionary<ProjectLocale, ProjectLocalizedContent>? localizedContent = null)
    {
        return new Project(
            id,
            sortOrder,
            imageReference,
            repositoryUrl,
            liveUrl,
            technologies,
            localizedContent ?? CreateEmptyLocalizedContent(),
            ProjectStatus.Draft,
            null,
            updatedAt,
            1);
    }

    public static Project Rehydrate(
        int id,
        int sortOrder,
        string imageReference,
        string? repositoryUrl,
        string? liveUrl,
        IReadOnlyList<string> technologies,
        IReadOnlyDictionary<ProjectLocale, ProjectLocalizedContent> localizedContent,
        ProjectStatus status,
        DateTime? publishedAt,
        DateTime updatedAt,
        long revision)
    {
        return new Project(id, sortOrder, imageReference, repositoryUrl, liveUrl, technologies, localizedContent, status, publishedAt, updatedAt, revision);
    }

    public void Update(
        int sortOrder,
        string imageReference,
        string? repositoryUrl,
        string? liveUrl,
        IReadOnlyList<string> technologies,
        IReadOnlyDictionary<ProjectLocale, ProjectLocalizedContent> content,
        DateTime updatedAt)
    {
        ValidateSortOrder(sortOrder);
        ValidateImageReference(imageReference);
        ValidateOptionalUrl(repositoryUrl, nameof(repositoryUrl));
        ValidateOptionalUrl(liveUrl, nameof(liveUrl));
        var copiedTechnologies = CopyTechnologies(technologies);
        ValidateLocalizedContent(content);
        if (Status == ProjectStatus.Published && !HasCompleteLocalizedContent(content))
        {
            throw new ProjectDomainException(ProjectErrorKeys.InvalidInput, "A published project must retain complete content in both locales.");
        }

        EnsureUtc(updatedAt);
        SortOrder = sortOrder;
        ImageReference = imageReference;
        RepositoryUrl = repositoryUrl;
        LiveUrl = liveUrl;
        this.technologies.Clear();
        this.technologies.AddRange(copiedTechnologies);
        localizedContent.Clear();
        foreach (var entry in content) localizedContent.Add(entry.Key, entry.Value);
        Touch(updatedAt);
    }

    public void SetSortOrder(int sortOrder, DateTime updatedAt)
    {
        ValidateSortOrder(sortOrder);
        EnsureUtc(updatedAt);
        SortOrder = sortOrder;
        Touch(updatedAt);
    }

    public void Publish(DateTime publishedAt)
    {
        if (Status == ProjectStatus.Published)
        {
            throw new ProjectDomainException(ProjectErrorKeys.InvalidTransition, "The project is already published.");
        }

        if (!HasCompleteLocalizedContent(localizedContent))
        {
            throw new ProjectDomainException(ProjectErrorKeys.InvalidInput, "Both locales must be complete before publication.");
        }

        EnsureUtc(publishedAt);
        Status = ProjectStatus.Published;
        PublishedAt ??= publishedAt;
        Touch(publishedAt);
    }

    public void Unpublish(DateTime updatedAt)
    {
        if (Status != ProjectStatus.Published)
        {
            throw new ProjectDomainException(ProjectErrorKeys.InvalidTransition, "Only a published project can be unpublished.");
        }

        EnsureUtc(updatedAt);
        Status = ProjectStatus.Unpublished;
        Touch(updatedAt);
    }

    private void Touch(DateTime updatedAt)
    {
        UpdatedAt = updatedAt;
        Revision++;
    }

    private static IReadOnlyDictionary<ProjectLocale, ProjectLocalizedContent> CreateEmptyLocalizedContent()
    {
        return SupportedLocales.ToDictionary(
            locale => locale,
            _ => new ProjectLocalizedContent(string.Empty, string.Empty, string.Empty, string.Empty, [string.Empty]));
    }

    private static void ValidateLocalizedContent(IReadOnlyDictionary<ProjectLocale, ProjectLocalizedContent> content)
    {
        if (content is null ||
            content.Count != SupportedLocales.Length ||
            SupportedLocales.Any(locale => !content.ContainsKey(locale)) ||
            content.Values.Any(value => value is null))
        {
            throw new ProjectDomainException(ProjectErrorKeys.InvalidInput, "Project content must include English and Portuguese locales.");
        }
    }

    private static bool HasCompleteLocalizedContent(IReadOnlyDictionary<ProjectLocale, ProjectLocalizedContent> content)
    {
        return SupportedLocales.All(locale => content.TryGetValue(locale, out var value) && value.IsComplete);
    }

    private static List<string> CopyTechnologies(IReadOnlyList<string> values)
    {
        if (values is null || values.Count is < 1 or > MaxTechnologyCount)
        {
            throw new ProjectDomainException(ProjectErrorKeys.InvalidInput, "A project must contain between one and twenty technologies.");
        }

        var copy = values.Select(value => value ?? string.Empty).ToList();
        if (copy.Any(value => value.Length is < 1 or > MaxTechnologyLength || string.IsNullOrWhiteSpace(value)))
        {
            throw new ProjectDomainException(ProjectErrorKeys.InvalidInput, "A project technology is invalid.");
        }

        return copy;
    }

    private static void ValidateSortOrder(int sortOrder)
    {
        if (sortOrder is < 1 or > MaxSortOrder)
        {
            throw new ProjectDomainException(ProjectErrorKeys.InvalidInput, "The project sort order is invalid.");
        }
    }

    private static void ValidateImageReference(string? imageReference)
    {
        if (string.IsNullOrWhiteSpace(imageReference) || imageReference.Length > MaxImageReferenceLength)
        {
            throw new ProjectDomainException(ProjectErrorKeys.InvalidInput, "The project image reference is invalid.");
        }

        if (imageReference.StartsWith("/", StringComparison.Ordinal) && !imageReference.StartsWith("//", StringComparison.Ordinal))
        {
            if (imageReference.Contains('\0') || imageReference.Split('/').Any(segment => segment == ".."))
            {
                throw new ProjectDomainException(ProjectErrorKeys.InvalidInput, "The project image path is invalid.");
            }

            return;
        }

        ValidateHttpsApprovedUrl(imageReference, nameof(imageReference));
    }

    private static void ValidateOptionalUrl(string? value, string fieldName)
    {
        if (value is null) return;
        ValidateHttpsApprovedUrl(value, fieldName);
    }

    private static void ValidateHttpsApprovedUrl(string value, string fieldName)
    {
        if (value.Length > MaxUrlLength ||
            !Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps ||
            uri.UserInfo.Length > 0 ||
            !ApprovedHosts.Contains(uri.Host))
        {
            throw new ProjectDomainException(ProjectErrorKeys.InvalidInput, $"The project URL '{fieldName}' is not approved.");
        }
    }

    private static void EnsureUtc(DateTime value)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ProjectDomainException(ProjectErrorKeys.InvalidInput, "Project timestamps must be UTC.");
        }
    }
}
