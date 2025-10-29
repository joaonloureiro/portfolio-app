
using JoaoLoureiro.Portfolio.Infrastructure.Settings;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace JoaoLoureiro.Portfolio.Infrastructure.HealthCheck;

public class SmtpHealthCheck(IOptions<SmtpSettings> smtpSettings) : IHealthCheck
{
    private readonly SmtpSettings _smtpSettings = smtpSettings.Value;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        using var client = new SmtpClient();
        try
        {
            await client.ConnectAsync(_smtpSettings.Host, _smtpSettings.Port, SecureSocketOptions.StartTlsWhenAvailable, cancellationToken);
            await client.AuthenticateAsync(_smtpSettings.User, _smtpSettings.Pass, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
            return HealthCheckResult.Healthy("SMTP server is responding correctly.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Failed to connect or authenticate with SMTP server.", exception: ex);
        }
    }
}