namespace JoaoLoureiro.Portfolio.Api.Dtos;

public sealed record AdminPostListResponse(
    IReadOnlyList<AdminPostSummaryResponse> Items,
    int Page,
    int PageSize,
    bool HasNext);

public sealed record AdminPostSummaryResponse(
    Guid Id,
    string Slug,
    string Status,
    DateTime? PublishedAt,
    DateTime UpdatedAt,
    long Revision);

public sealed record AdminPostResponse(
    Guid Id,
    string Slug,
    string Status,
    DateTime? PublishedAt,
    DateTime UpdatedAt,
    long Revision,
    IReadOnlyDictionary<string, AdminLocalizedPostResponse> Content,
    string? CoverImageUrl,
    IReadOnlyList<string> Tags);

public sealed record AdminLocalizedPostResponse(
    string Title,
    string Excerpt,
    string Body,
    string? CoverImageAlt);
