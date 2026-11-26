namespace JoaoLoureiro.Portfolio.Infrastructure.Persistence;

public sealed class ProjectRecord
{
    public int Id { get; set; }

    public int SortOrder { get; set; }

    public string Status { get; set; } = string.Empty;

    public string ImageReference { get; set; } = string.Empty;

    public string? RepositoryUrl { get; set; }

    public string? LiveUrl { get; set; }

    public DateTime? PublishedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public long Revision { get; set; }

    public List<ProjectLocalizedContentRecord> LocalizedContent { get; set; } = [];

    public List<ProjectTechnologyRecord> Technologies { get; set; } = [];
}

public sealed class ProjectLocalizedContentRecord
{
    public int ProjectId { get; set; }

    public string Locale { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string Details { get; set; } = string.Empty;

    public string FeatureHeading { get; set; } = string.Empty;

    public ProjectRecord? Project { get; set; }

    public List<ProjectFeatureRecord> Features { get; set; } = [];
}

public sealed class ProjectFeatureRecord
{
    public int ProjectId { get; set; }

    public string Locale { get; set; } = string.Empty;

    public int Position { get; set; }

    public string Value { get; set; } = string.Empty;

    public ProjectLocalizedContentRecord? Content { get; set; }
}

public sealed class ProjectTechnologyRecord
{
    public int ProjectId { get; set; }

    public int Position { get; set; }

    public string Value { get; set; } = string.Empty;

    public ProjectRecord? Project { get; set; }
}
