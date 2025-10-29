using JoaoLoureiro.Portfolio.Core.Interfaces;
using JoaoLoureiro.Portfolio.Core.Models;
using JoaoLoureiro.Portfolio.Infrastructure.Settings;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace JoaoLoureiro.Portfolio.Infrastructure.Services;

public class SmtpEmailSender(IOptions<SmtpSettings> smtpSettings, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly SmtpSettings _smtpSettings = smtpSettings.Value;
    private readonly ILogger<SmtpEmailSender> _logger = logger;

    public async Task SendEmailAsync(EmailToSend email, CancellationToken cancellationToken = default)
    {
        var mimeMessage = new MimeMessage();
        var fromAddress = _smtpSettings.FromEmail ?? _smtpSettings.User;

        mimeMessage.From.Add(new MailboxAddress(email.SenderName, fromAddress));
        mimeMessage.To.Add(MailboxAddress.Parse(_smtpSettings.ReceivingEmail));
        mimeMessage.ReplyTo.Add(MailboxAddress.Parse(email.ReplyToEmail));
        mimeMessage.Subject = $"New Portfolio Contact: {email.SenderName}";

        var builder = new BodyBuilder
        {
            TextBody = $"Name: {email.SenderName}\nEmail: {email.ReplyToEmail}\nMessage: {email.MessageBody}",
            HtmlBody = $@"<p><strong>Name:</strong> {email.SenderName}</p>
                        <p><strong>Email:</strong> <a href=""mailto:{email.ReplyToEmail}"">{email.ReplyToEmail}</a></p>
                        <p><strong>Message:</strong></p>
                        <p>{email.MessageBody.Replace("\n", "<br>")}</p>"
        };
        mimeMessage.Body = builder.ToMessageBody();

        using var client = new SmtpClient();

        await client.ConnectAsync(_smtpSettings.Host, _smtpSettings.Port, SecureSocketOptions.StartTlsWhenAvailable, cancellationToken);
        await client.AuthenticateAsync(_smtpSettings.User, _smtpSettings.Pass, cancellationToken);
        await client.SendAsync(mimeMessage, cancellationToken);
        _logger.LogInformation("Email sent successfully to {Recipient}", _smtpSettings.ReceivingEmail);
    }
}
