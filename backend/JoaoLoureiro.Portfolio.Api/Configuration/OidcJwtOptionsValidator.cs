using Microsoft.Extensions.Options;

namespace JoaoLoureiro.Portfolio.Api.Configuration;

public sealed class OidcJwtOptionsValidator(IHostEnvironment environment)
    : IValidateOptions<OidcJwtOptions>
{
    public ValidateOptionsResult Validate(string? name, OidcJwtOptions options)
    {
        var failures = new List<string>();

        AddRequiredFailure(failures, nameof(options.Issuer), options.Issuer);
        AddRequiredFailure(failures, nameof(options.Audience), options.Audience);
        AddRequiredFailure(failures, nameof(options.DiscoveryAddress), options.DiscoveryAddress);
        AddRequiredFailure(failures, nameof(options.AdminClaimType), options.AdminClaimType);
        AddRequiredFailure(failures, nameof(options.AdminGroup), options.AdminGroup);

        var requireHttps = !environment.IsEnvironment("Testing");
        if (!string.IsNullOrWhiteSpace(options.Issuer))
        {
            ValidateAbsoluteUri(failures, nameof(options.Issuer), options.Issuer, requireHttps);
        }

        if (!string.IsNullOrWhiteSpace(options.DiscoveryAddress))
        {
            ValidateAbsoluteUri(failures, nameof(options.DiscoveryAddress), options.DiscoveryAddress, requireHttps);
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void AddRequiredFailure(ICollection<string> failures, string propertyName, string value)
    {
        if (string.IsNullOrWhiteSpace(value)) failures.Add($"{propertyName} is required.");
    }

    private static void ValidateAbsoluteUri(ICollection<string> failures, string propertyName, string value, bool requireHttps)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            (requireHttps && uri.Scheme != Uri.UriSchemeHttps) ||
            (!requireHttps && uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            var expected = requireHttps ? "an absolute HTTPS URL" : "an absolute HTTP or HTTPS URL";
            failures.Add($"{propertyName} must be {expected}.");
        }
    }
}
