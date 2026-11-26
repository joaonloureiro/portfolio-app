namespace JoaoLoureiro.Portfolio.Api.Dtos;

public sealed record AdminProjectListResponse(
    IReadOnlyList<AdminProjectSummaryResponse> Items,
    int Page,
    int PageSize,
    bool HasNext);

public sealed record AdminProjectSummaryResponse(
    int Id,
    int SortOrder,
    string Status,
    string ImageReference,
    IReadOnlyDictionary<string, string> Titles,
    DateTime? PublishedAt,
    DateTime UpdatedAt,
    long Revision);

public sealed record AdminProjectResponse(
    int Id,
    int SortOrder,
    string Status,
    string ImageReference,
    string? RepositoryUrl,
    string? LiveUrl,
    DateTime? PublishedAt,
    DateTime UpdatedAt,
    long Revision,
    IReadOnlyList<string> Technologies,
    IReadOnlyDictionary<string, AdminProjectLocalizedContentResponse> Content);

public sealed record AdminProjectLocalizedContentResponse(
    string Title,
    string Summary,
    string Details,
    string FeatureHeading,
    IReadOnlyList<string> Features);
