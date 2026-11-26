namespace JoaoLoureiro.Portfolio.Api.Dtos;

public sealed record ProjectLocalizedContentRequest(
    string? Title,
    string? Summary,
    string? Details,
    string? FeatureHeading,
    IReadOnlyList<string>? Features);

public sealed record ProjectContentRequest(
    ProjectLocalizedContentRequest? En,
    ProjectLocalizedContentRequest? Pt);

public sealed record CreateProjectRequest(
    int SortOrder,
    string? ImageReference,
    string? RepositoryUrl,
    string? LiveUrl,
    IReadOnlyList<string>? Technologies,
    ProjectContentRequest? Content);

public sealed record UpdateProjectRequest(
    long ExpectedRevision,
    int SortOrder,
    string? ImageReference,
    string? RepositoryUrl,
    string? LiveUrl,
    IReadOnlyList<string>? Technologies,
    ProjectContentRequest? Content);

public sealed record ProjectRevisionRequest(long ExpectedRevision);

public sealed record DeleteProjectRequest(long ExpectedRevision, string? Confirmation);

public sealed record ReorderProjectsRequest(IReadOnlyList<ReorderProjectItemRequest>? Items);

public sealed record ReorderProjectItemRequest(int Id, long ExpectedRevision);
