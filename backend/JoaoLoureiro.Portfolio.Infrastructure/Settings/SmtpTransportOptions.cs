using System.Net.Mail;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace JoaoLoureiro.Portfolio.Infrastructure.Settings;

public sealed class SmtpTransportOptions
{
    public const string SectionName = "SmtpTransport";

    public string Host { get; init; } = string.Empty;
    public int Port { get; init; } = 587;
    public string SecurityMode { get; init; } = "StartTls";
    public bool AuthenticationRequired { get; init; } = true;
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FromAddress { get; init; } = string.Empty;
    public int ConnectTimeoutSeconds { get; init; } = 5;
    public int SendTimeoutSeconds { get; init; } = 15;
    public int DisconnectTimeoutSeconds { get; init; } = 5;
}

public sealed class SmtpTransportOptionsValidator(IHostEnvironment environment)
    : IValidateOptions<SmtpTransportOptions>
{
    private static readonly string[] AllowedSecurityModes = ["None", "StartTls", "SslOnConnect"];

    public ValidateOptionsResult Validate(string? name, SmtpTransportOptions options)
    {
        var failures = new List<string>();
        if (string.IsNullOrWhiteSpace(options.Host) || options.Host.Length > 255 ||
            options.Host != options.Host.Trim() || options.Host.Any(char.IsControl) || options.Host.Any(char.IsWhiteSpace) ||
            options.Host.Contains('/', StringComparison.Ordinal) || options.Host.Contains('\\', StringComparison.Ordinal))
        {
            failures.Add("SmtpTransport:Host is required and must be a valid host name.");
        }

        if (options.Port is < 1 or > 65_535)
        {
            failures.Add("SmtpTransport:Port must be between 1 and 65535.");
        }

        if (!AllowedSecurityModes.Contains(options.SecurityMode, StringComparer.OrdinalIgnoreCase))
        {
            failures.Add("SmtpTransport:SecurityMode must be None, StartTls, or SslOnConnect.");
        }

        if (environment.IsProduction() && string.Equals(options.SecurityMode, "None", StringComparison.OrdinalIgnoreCase))
        {
            failures.Add("SmtpTransport:SecurityMode cannot be None in Production.");
        }

        if (options.AuthenticationRequired && (string.IsNullOrWhiteSpace(options.Username) || string.IsNullOrWhiteSpace(options.Password)))
        {
            failures.Add("SmtpTransport:Username and Password are required when authentication is enabled.");
        }

        if (!TryValidateAddress(options.FromAddress))
        {
            failures.Add("SmtpTransport:FromAddress must be a valid email address.");
        }

        if (options.ConnectTimeoutSeconds is < 1 or > 60 ||
            options.SendTimeoutSeconds is < 1 or > 120 ||
            options.DisconnectTimeoutSeconds is < 1 or > 60)
        {
            failures.Add("SmtpTransport timeouts are outside their permitted bounds.");
        }

        if (environment.IsProduction() && string.IsNullOrWhiteSpace(options.Password))
        {
            failures.Add("SmtpTransport:Password must be configured in Production.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static bool TryValidateAddress(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Any(char.IsControl)) return false;
        try
        {
            var address = new MailAddress(value.Trim());
            return string.Equals(address.Address, value.Trim(), StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

public sealed class EmailDeliveryPolicySeedOptions
{
    public const string SectionName = "EmailDelivery";

    public bool InitialEnabled { get; init; } = true;
    public string InitialRecipientAddress { get; init; } = "me@joaoloureiro.dev.br";
    public string InitialSenderDisplayName { get; init; } = "Portfolio contact";
    public string InitialSubjectPrefix { get; init; } = "New Portfolio Contact";
}

public sealed class EmailDeliveryPolicySeedOptionsValidator : IValidateOptions<EmailDeliveryPolicySeedOptions>
{
    public ValidateOptionsResult Validate(string? name, EmailDeliveryPolicySeedOptions options)
    {
        try
        {
            _ = JoaoLoureiro.Portfolio.Core.EmailDelivery.EmailDeliveryPolicy.Create(
                options.InitialEnabled,
                options.InitialRecipientAddress,
                options.InitialSenderDisplayName,
                options.InitialSubjectPrefix,
                1);
            return ValidateOptionsResult.Success;
        }
        catch (JoaoLoureiro.Portfolio.Core.EmailDelivery.EmailDeliveryPolicyValidationException)
        {
            return ValidateOptionsResult.Fail("EmailDelivery initial policy values are invalid.");
        }
    }
}
