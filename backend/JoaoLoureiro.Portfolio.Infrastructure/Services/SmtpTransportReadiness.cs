using JoaoLoureiro.Portfolio.Core.Interfaces;
using JoaoLoureiro.Portfolio.Infrastructure.Settings;
using Microsoft.Extensions.Options;

namespace JoaoLoureiro.Portfolio.Infrastructure.Services;

public sealed class SmtpTransportReadiness(IOptions<SmtpTransportOptions> options) : IEmailTransportReadiness
{
    public bool IsConfigured => options.Value is not null;
}
