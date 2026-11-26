using FluentValidation;
using JoaoLoureiro.Portfolio.Api.Dtos;
using JoaoLoureiro.Portfolio.Core.EmailDelivery;

namespace JoaoLoureiro.Portfolio.Api.Validators;

public sealed class EmailDeliveryPolicyRequestValidator : AbstractValidator<UpdateEmailDeliveryPolicyRequest>
{
    public EmailDeliveryPolicyRequestValidator()
    {
        RuleFor(request => request.ExpectedRevision)
            .GreaterThanOrEqualTo(1)
            .WithErrorCode(EmailDeliveryErrorKeys.InvalidPolicy);
        RuleFor(request => request.RecipientAddress)
            .NotEmpty()
            .MaximumLength(EmailDeliveryPolicyLimits.MaxRecipientAddressLength)
            .EmailAddress()
            .WithErrorCode(EmailDeliveryErrorKeys.InvalidPolicy);
        RuleFor(request => request.SenderDisplayName)
            .NotEmpty()
            .MaximumLength(EmailDeliveryPolicyLimits.MaxSenderDisplayNameLength)
            .Must(value => value is not null && !value.Any(char.IsControl))
            .WithErrorCode(EmailDeliveryErrorKeys.InvalidPolicy);
        RuleFor(request => request.SubjectPrefix)
            .NotEmpty()
            .MaximumLength(EmailDeliveryPolicyLimits.MaxSubjectPrefixLength)
            .Must(value => value is not null && !value.Any(char.IsControl))
            .WithErrorCode(EmailDeliveryErrorKeys.InvalidPolicy);
    }
}
