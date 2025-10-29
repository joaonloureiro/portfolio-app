namespace JoaoLoureiro.Portfolio.Core.Models;

public class EmailToSend
{
    public required string SenderName { get; set; }
    public required string ReplyToEmail { get; set; }
    public required string MessageBody { get; set; }
}