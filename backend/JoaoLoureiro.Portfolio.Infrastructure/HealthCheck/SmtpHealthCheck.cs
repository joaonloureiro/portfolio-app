using JoaoLoureiro.Portfolio.Infrastructure.Settings;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace JoaoLoureiro.Portfolio.Infrastructure.HealthCheck;

public sealed class SmtpHealthCheck(IOptions<SmtpTransportOptions> smtpOptions) : IHealthCheck
{
    private static readonly TimeSpan MinimumCheckInterval = TimeSpan.FromSeconds(30);
    private readonly SmtpTransportOptions _smtpOptions = smtpOptions.Value;
    private readonly SemaphoreSlim gate = new(1, 1);
    private DateTimeOffset lastCheck = DateTimeOffset.MinValue;
    private HealthCheckResult lastResult = HealthCheckResult.Healthy();

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (DateTimeOffset.UtcNow - lastCheck < MinimumCheckInterval)
        {
            return lastResult;
        }

        await gate.WaitAsync(cancellationToken);
        try
        {
            if (DateTimeOffset.UtcNow - lastCheck < MinimumCheckInterval)
            {
                return lastResult;
            }

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(Math.Min(_smtpOptions.ConnectTimeoutSeconds, 10)));
            using var client = new SmtpClient {Timeout = _smtpOptions.ConnectTimeoutSeconds * 1000};
            try
            {
                await client.ConnectAsync(
                    _smtpOptions.Host,
                    _smtpOptions.Port,
                    ParseSecurityMode(_smtpOptions.SecurityMode),
                    timeout.Token);
                if (_smtpOptions.AuthenticationRequired)
                {
                    await client.AuthenticateAsync(_smtpOptions.Username, _smtpOptions.Password, timeout.Token);
                }

                await client.DisconnectAsync(true, timeout.Token);
                lastResult = HealthCheckResult.Healthy();
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                lastResult = HealthCheckResult.Unhealthy();
            }
            catch (Exception)
            {
                lastResult = HealthCheckResult.Unhealthy();
            }

            lastCheck = DateTimeOffset.UtcNow;
            return lastResult;
        }
        finally
        {
            gate.Release();
        }
    }

    private static SecureSocketOptions ParseSecurityMode(string value)
    {
        return value.ToUpperInvariant() switch
        {
            "NONE" => SecureSocketOptions.None,
            "STARTTLS" => SecureSocketOptions.StartTls,
            "SSLONCONNECT" => SecureSocketOptions.SslOnConnect,
            _ => SecureSocketOptions.None
        };
    }
}
