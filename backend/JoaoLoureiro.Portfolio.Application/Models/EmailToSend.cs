namespace JoaoLoureiro.Portfolio.Core.Models;

public class EmailToSend
{
    public required string RecipientAddress { get; set; }
    public required string SenderDisplayName { get; set; }
    public required string Subject { get; set; }
    public required string SenderName { get; set; }
    public required string ReplyToEmail { get; set; }
    public required string MessageBody { get; set; }
}
