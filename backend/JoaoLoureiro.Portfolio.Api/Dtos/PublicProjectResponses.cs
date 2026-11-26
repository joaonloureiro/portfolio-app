namespace JoaoLoureiro.Portfolio.Api.Dtos;

public sealed record PublicProjectListResponse(
    IReadOnlyList<PublicProjectSummaryResponse> Items,
    int Page,
    int PageSize,
    bool HasNext);

public sealed record PublicProjectSummaryResponse(
    int Id,
    int SortOrder,
    string ImageReference,
    string? RepositoryUrl,
    string? LiveUrl,
    IReadOnlyList<string> Technologies,
    string Title,
    string Summary);

public sealed record PublicProjectResponse(
    int Id,
    int SortOrder,
    string ImageReference,
    string? RepositoryUrl,
    string? LiveUrl,
    IReadOnlyList<string> Technologies,
    string Title,
    string Summary,
    string Details,
    string FeatureHeading,
    IReadOnlyList<string> Features);
