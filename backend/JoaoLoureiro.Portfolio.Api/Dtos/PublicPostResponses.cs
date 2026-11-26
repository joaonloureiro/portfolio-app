namespace JoaoLoureiro.Portfolio.Api.Dtos;

public sealed record PublicPostListResponse(
    IReadOnlyList<PublicPostSummaryResponse> Items,
    int Page,
    int PageSize,
    bool HasNext);

public sealed record PublicPostSummaryResponse(
    string Slug,
    DateTime PublishedAt,
    string Title,
    string Excerpt,
    string? CoverImageUrl,
    string? CoverImageAlt,
    IReadOnlyList<string> Tags);

public sealed record PublicPostResponse(
    string Slug,
    DateTime PublishedAt,
    string Title,
    string Excerpt,
    string Body,
    string? CoverImageUrl,
    string? CoverImageAlt,
    IReadOnlyList<string> Tags);
