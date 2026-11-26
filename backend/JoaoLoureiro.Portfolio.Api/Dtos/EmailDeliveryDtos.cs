namespace JoaoLoureiro.Portfolio.Api.Dtos;

public sealed record EmailDeliveryPolicyResponse(
    bool Enabled,
    string RecipientAddress,
    string SenderDisplayName,
    string SubjectPrefix,
    long Revision,
    bool TransportConfigured);

public sealed class UpdateEmailDeliveryPolicyRequest
{
    public bool Enabled { get; set; }
    public string? RecipientAddress { get; set; }
    public string? SenderDisplayName { get; set; }
    public string? SubjectPrefix { get; set; }
    public long ExpectedRevision { get; set; }
}

public sealed record EmailDeliveryTestResponse(string Message);
