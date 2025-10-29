using JoaoLoureiro.Portfolio.Core.Models;

namespace JoaoLoureiro.Portfolio.Core.Interfaces;

public interface IEmailSender
{
    Task SendEmailAsync(EmailToSend email, CancellationToken cancellationToken = default);
}
