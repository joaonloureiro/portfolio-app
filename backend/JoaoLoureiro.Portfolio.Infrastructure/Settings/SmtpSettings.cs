namespace JoaoLoureiro.Portfolio.Infrastructure.Settings;

public class SmtpSettings
{
    public required string Host { get; set; }
    public int Port { get; set; }
    public required string User { get; set; }
    public required string Pass { get; set; }
    public string? FromEmail { get; set; }
    public required string ReceivingEmail { get; set; }
}
