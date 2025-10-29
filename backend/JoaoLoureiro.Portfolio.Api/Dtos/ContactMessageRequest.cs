namespace JoaoLoureiro.Portfolio.Api.Dtos;

public class ContactMessageRequest
{
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string Message { get; set; }
}
