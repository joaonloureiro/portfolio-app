namespace JoaoLoureiro.Portfolio.Infrastructure.Persistence;

public sealed class EmailDeliveryPolicyRecord
{
    public int Id { get; set; }
    public bool Enabled { get; set; }
    public string RecipientAddress { get; set; } = string.Empty;
    public string SenderDisplayName { get; set; } = string.Empty;
    public string SubjectPrefix { get; set; } = string.Empty;
    public long Revision { get; set; }
}
