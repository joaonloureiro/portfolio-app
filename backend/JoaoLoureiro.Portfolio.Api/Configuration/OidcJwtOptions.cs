namespace JoaoLoureiro.Portfolio.Api.Configuration;

public sealed class OidcJwtOptions
{
    public const string SectionName = "Authentication:Oidc";

    public string Issuer { get; init; } = string.Empty;

    public string Audience { get; init; } = string.Empty;

    public string DiscoveryAddress { get; init; } = string.Empty;

    public string AdminClaimType { get; init; } = "groups";

    public string AdminGroup { get; init; } = string.Empty;
}
