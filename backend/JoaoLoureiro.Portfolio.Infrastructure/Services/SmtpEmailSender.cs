using JoaoLoureiro.Portfolio.Core.Interfaces;
using JoaoLoureiro.Portfolio.Core.Models;
using JoaoLoureiro.Portfolio.Infrastructure.Settings;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Text.Encodings.Web;

namespace JoaoLoureiro.Portfolio.Infrastructure.Services;

public class SmtpEmailSender(IOptions<SmtpTransportOptions> smtpOptions, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly SmtpTransportOptions _smtpOptions = smtpOptions.Value;
    private readonly ILogger<SmtpEmailSender> _logger = logger;

    public async Task SendEmailAsync(EmailToSend email, CancellationToken cancellationToken = default)
    {
        var mimeMessage = new MimeMessage();
        try
        {
            mimeMessage.From.Add(new MailboxAddress(email.SenderDisplayName, _smtpOptions.FromAddress));
            mimeMessage.To.Add(MailboxAddress.Parse(email.RecipientAddress));
            mimeMessage.ReplyTo.Add(MailboxAddress.Parse(email.ReplyToEmail));
            mimeMessage.Subject = email.Subject;

            var encodedName = HtmlEncoder.Default.Encode(email.SenderName);
            var encodedReplyTo = HtmlEncoder.Default.Encode(email.ReplyToEmail);
            var encodedMessage = HtmlEncoder.Default.Encode(email.MessageBody)
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Replace("\n", "<br>", StringComparison.Ordinal);
            var builder = new BodyBuilder
            {
                TextBody = $"Name: {email.SenderName}\r\nEmail: {email.ReplyToEmail}\r\nMessage:\r\n{email.MessageBody}",
                HtmlBody = $"<p><strong>Name:</strong> {encodedName}</p>" +
                           $"<p><strong>Email:</strong> <a href=\"mailto:{encodedReplyTo}\">{encodedReplyTo}</a></p>" +
                           $"<p><strong>Message:</strong></p><p>{encodedMessage}</p>"
            };
            mimeMessage.Body = builder.ToMessageBody();

            using var client = new SmtpClient
            {
                Timeout = checked(_smtpOptions.SendTimeoutSeconds * 1000)
            };
            var securityMode = ParseSecurityMode(_smtpOptions.SecurityMode);
            using var operationTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            operationTimeout.CancelAfter(TimeSpan.FromSeconds(
                _smtpOptions.ConnectTimeoutSeconds + _smtpOptions.SendTimeoutSeconds + _smtpOptions.DisconnectTimeoutSeconds));

            await client.ConnectAsync(_smtpOptions.Host, _smtpOptions.Port, securityMode, operationTimeout.Token);
            if (_smtpOptions.AuthenticationRequired)
            {
                await client.AuthenticateAsync(_smtpOptions.Username, _smtpOptions.Password, operationTimeout.Token);
            }

            await client.SendAsync(mimeMessage, operationTimeout.Token);
            try
            {
                await client.DisconnectAsync(true, operationTimeout.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new EmailDeliveryException(EmailDeliveryFailureKind.Timeout);
            }

            _logger.LogInformation("Email delivery completed.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new EmailDeliveryException(EmailDeliveryFailureKind.Timeout);
        }
        catch (EmailDeliveryException)
        {
            throw;
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new EmailDeliveryException(EmailDeliveryFailureKind.Unavailable);
        }
    }

    private static SecureSocketOptions ParseSecurityMode(string value)
    {
        return value.ToUpperInvariant() switch
        {
            "NONE" => SecureSocketOptions.None,
            "STARTTLS" => SecureSocketOptions.StartTls,
            "SSLONCONNECT" => SecureSocketOptions.SslOnConnect,
            _ => throw new EmailDeliveryException(EmailDeliveryFailureKind.Unavailable)
        };
    }
}
