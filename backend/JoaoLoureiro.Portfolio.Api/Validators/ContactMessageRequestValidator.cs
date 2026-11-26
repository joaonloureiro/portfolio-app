using FluentValidation;
using JoaoLoureiro.Portfolio.Api.Dtos;

namespace JoaoLoureiro.Portfolio.Api.Validators;

public class ContactMessageRequestValidator : AbstractValidator<ContactMessageRequest>
{
    public const int MaxNameLength = 120;
    public const int MaxEmailLength = 320;
    public const int MaxMessageLength = 4_000;
    public const int MaxRequestBytes = 16 * 1024;

    public ContactMessageRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithErrorCode("status_error_all_fields");
        RuleFor(x => x.Name)
            .MaximumLength(MaxNameLength)
            .WithErrorCode("status_error_all_fields");
        RuleFor(x => x.Name)
            .Must(value => value is not null && !value.Any(char.IsControl))
            .WithErrorCode("status_error_all_fields");
        RuleFor(x => x.Message)
            .NotEmpty()
            .WithErrorCode("status_error_all_fields");
        RuleFor(x => x.Message)
            .MaximumLength(MaxMessageLength)
            .WithErrorCode("status_error_all_fields");
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithErrorCode("status_error_all_fields");
        RuleFor(x => x.Email)
            .MaximumLength(MaxEmailLength)
            .WithErrorCode("status_error_invalid_email");
        RuleFor(x => x.Email)
            .EmailAddress()
            .WithErrorCode("status_error_invalid_email");
        RuleFor(x => x.Email)
            .Must(value => value is not null && !value.Any(char.IsControl))
            .WithErrorCode("status_error_invalid_email");
    }
}
