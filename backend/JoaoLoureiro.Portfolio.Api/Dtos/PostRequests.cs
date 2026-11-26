namespace JoaoLoureiro.Portfolio.Api.Dtos;

public sealed record PostLocalizedContentRequest(
    string? Title,
    string? Excerpt,
    string? Body,
    string? CoverImageAlt = null);

public sealed record PostContentRequest(
    PostLocalizedContentRequest? En,
    PostLocalizedContentRequest? Pt);

public sealed record CreatePostRequest(
    string? Slug,
    PostContentRequest? Content,
    string? CoverImageUrl = null,
    IReadOnlyList<string>? Tags = null);

public sealed record UpdatePostRequest(
    long ExpectedRevision,
    PostContentRequest? Content,
    string? CoverImageUrl = null,
    IReadOnlyList<string>? Tags = null);

public sealed record PostRevisionRequest(long ExpectedRevision);

public sealed record DeletePostRequest(
    long ExpectedRevision,
    string? Confirmation);
