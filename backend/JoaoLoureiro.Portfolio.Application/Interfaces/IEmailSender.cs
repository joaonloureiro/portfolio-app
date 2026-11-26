using JoaoLoureiro.Portfolio.Core.Models;

namespace JoaoLoureiro.Portfolio.Core.Interfaces;

public interface IEmailSender
{
    Task SendEmailAsync(EmailToSend email, CancellationToken cancellationToken = default);
}

public enum EmailDeliveryFailureKind
{
    Timeout,
    Unavailable
}

public sealed class EmailDeliveryException(EmailDeliveryFailureKind kind)
    : Exception("The email delivery transport could not complete the operation.")
{
    public EmailDeliveryFailureKind Kind { get; } = kind;
}
