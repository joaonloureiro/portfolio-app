using System.Net;
using System.Net.Sockets;
using System.Text;
using JoaoLoureiro.Portfolio.Core.Models;
using JoaoLoureiro.Portfolio.Infrastructure.Services;
using JoaoLoureiro.Portfolio.Infrastructure.Settings;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MimeKit;
using Xunit;

namespace JoaoLoureiro.Portfolio.Api.Tests;

public sealed class SmtpEmailSenderTests
{
    [Fact]
    public async Task EncodesVisitorContentInHtmlAndPreservesPlainText()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var messageTask = CaptureSmtpMessageAsync(listener);
        var sender = new SmtpEmailSender(
            Options.Create(new SmtpTransportOptions
            {
                Host = "127.0.0.1",
                Port = port,
                SecurityMode = "None",
                AuthenticationRequired = false,
                FromAddress = "no-reply@example.test",
                ConnectTimeoutSeconds = 5,
                SendTimeoutSeconds = 5,
                DisconnectTimeoutSeconds = 5
            }),
            NullLogger<SmtpEmailSender>.Instance);

        await sender.SendEmailAsync(new EmailToSend
        {
            RecipientAddress = "recipient@example.test",
            SenderDisplayName = "Portfolio contact",
            Subject = "Contact",
            SenderName = "<script>alert(1)</script>",
            ReplyToEmail = "visitor@example.test",
            MessageBody = "</p><script>alert(2)</script>\nA second line"
        });

        var rawMessage = await messageTask;
        using var message = MimeMessage.Load(new MemoryStream(Encoding.UTF8.GetBytes(rawMessage)));
        Assert.DoesNotContain("<script>", message.HtmlBody, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("&lt;script&gt;", message.HtmlBody, StringComparison.Ordinal);
        Assert.Contains("</p><script>alert(2)</script>", message.TextBody, StringComparison.Ordinal);
        Assert.Contains("A second line", message.TextBody, StringComparison.Ordinal);
    }

    private static async Task<string> CaptureSmtpMessageAsync(TcpListener listener)
    {
        using var client = await listener.AcceptTcpClientAsync();
        await using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
        await using var writer = new StreamWriter(stream, Encoding.ASCII, leaveOpen: true) {AutoFlush = true, NewLine = "\r\n"};
        await writer.WriteLineAsync("220 smtp.test");

        var message = new StringBuilder();
        while (true)
        {
            var line = await reader.ReadLineAsync();
            if (line is null) break;
            if (line.Equals("DATA", StringComparison.OrdinalIgnoreCase))
            {
                await writer.WriteLineAsync("354 End data with <CR><LF>.<CR><LF>");
                while (true)
                {
                    var dataLine = await reader.ReadLineAsync();
                    if (dataLine is null || dataLine == ".") break;
                    message.Append(dataLine).Append("\r\n");
                }

                await writer.WriteLineAsync("250 2.0.0 accepted");
                continue;
            }

            if (line.StartsWith("EHLO", StringComparison.OrdinalIgnoreCase) || line.StartsWith("HELO", StringComparison.OrdinalIgnoreCase))
            {
                await writer.WriteLineAsync("250-smtp.test");
                await writer.WriteLineAsync("250 OK");
            }
            else if (line.StartsWith("MAIL FROM", StringComparison.OrdinalIgnoreCase) || line.StartsWith("RCPT TO", StringComparison.OrdinalIgnoreCase))
            {
                await writer.WriteLineAsync("250 OK");
            }
            else if (line.StartsWith("QUIT", StringComparison.OrdinalIgnoreCase))
            {
                await writer.WriteLineAsync("221 Bye");
                break;
            }
            else
            {
                await writer.WriteLineAsync("250 OK");
            }
        }

        return message.ToString();
    }
}
