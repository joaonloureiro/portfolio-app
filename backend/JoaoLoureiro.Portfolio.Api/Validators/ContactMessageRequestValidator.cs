using FluentValidation;
using JoaoLoureiro.Portfolio.Api.Dtos;

namespace JoaoLoureiro.Portfolio.Api.Validators;

public class ContactMessageRequestValidator : AbstractValidator<ContactMessageRequest>
{
    public ContactMessageRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithErrorCode("status_error_all_fields");
        RuleFor(x => x.Message).NotEmpty().WithErrorCode("status_error_all_fields");
        RuleFor(x => x.Email)
            .NotEmpty().WithErrorCode("status_error_all_fields")
            .EmailAddress().WithErrorCode("status_error_invalid_email");
    }
}
