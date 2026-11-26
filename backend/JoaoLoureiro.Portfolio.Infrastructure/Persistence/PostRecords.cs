namespace JoaoLoureiro.Portfolio.Infrastructure.Persistence;

public sealed class PostRecord
{
    public Guid Id { get; set; }

    public string Slug { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime? PublishedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public long Revision { get; set; }

    public string? CoverImageUrl { get; set; }

    public string TagsJson { get; set; } = "[]";

    public List<PostLocalizedContentRecord> LocalizedContent { get; set; } = [];
}

public sealed class PostLocalizedContentRecord
{
    public Guid PostId { get; set; }

    public string Locale { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Excerpt { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public string? CoverImageAlt { get; set; }

    public PostRecord? Post { get; set; }
}
