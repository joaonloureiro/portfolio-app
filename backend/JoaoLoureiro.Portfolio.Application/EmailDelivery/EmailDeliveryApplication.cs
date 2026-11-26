using JoaoLoureiro.Portfolio.Core.Interfaces;
using JoaoLoureiro.Portfolio.Core.Models;

namespace JoaoLoureiro.Portfolio.Core.EmailDelivery;

public sealed class EmailDeliveryApplicationService(
    IEmailDeliveryPolicyStore policyStore,
    IEmailSender emailSender)
{
    private const string TestSubject = "Portfolio email delivery test";
    private const string TestBody = "This is a bounded test message from the portfolio Admin workspace.";

    public Task<EmailDeliveryPolicy> GetPolicyAsync(CancellationToken cancellationToken)
    {
        return policyStore.GetOrSeedAsync(cancellationToken);
    }

    public async Task<EmailDeliveryPolicyUpdateResult> UpdatePolicyAsync(
        long expectedRevision,
        EmailDeliveryPolicyInput input,
        CancellationToken cancellationToken)
    {
        if (expectedRevision < 1)
        {
            return new(EmailDeliveryPolicyUpdateOutcome.InvalidPolicy);
        }

        EmailDeliveryPolicy requested;
        try
        {
            requested = EmailDeliveryPolicy.Create(
                input.Enabled,
                input.RecipientAddress,
                input.SenderDisplayName,
                input.SubjectPrefix,
                expectedRevision);
        }
        catch (EmailDeliveryPolicyValidationException)
        {
            return new(EmailDeliveryPolicyUpdateOutcome.InvalidPolicy);
        }

        var mutation = await policyStore.UpdateAsync(requested, expectedRevision, cancellationToken);
        return mutation == EmailDeliveryPolicyStoreMutationResult.Conflict
            ? new(EmailDeliveryPolicyUpdateOutcome.StaleRevision)
            : new(EmailDeliveryPolicyUpdateOutcome.Succeeded, requested with {Revision = expectedRevision + 1});
    }

    public async Task<EmailDeliverySendResult> SendContactAsync(
        string senderName,
        string replyToEmail,
        string messageBody,
        CancellationToken cancellationToken)
    {
        var policy = await policyStore.GetOrSeedAsync(cancellationToken);
        if (!policy.Enabled)
        {
            return new(EmailDeliverySendOutcome.Disabled);
        }

        var email = new EmailToSend
        {
            RecipientAddress = policy.RecipientAddress,
            SenderDisplayName = policy.SenderDisplayName,
            Subject = $"{policy.SubjectPrefix}: {senderName}",
            SenderName = senderName,
            ReplyToEmail = replyToEmail,
            MessageBody = messageBody
        };

        return await SendAsync(email, cancellationToken);
    }

    public async Task<EmailDeliverySendResult> SendTestAsync(CancellationToken cancellationToken)
    {
        var policy = await policyStore.GetOrSeedAsync(cancellationToken);
        if (!policy.Enabled)
        {
            return new(EmailDeliverySendOutcome.Disabled);
        }

        var email = new EmailToSend
        {
            RecipientAddress = policy.RecipientAddress,
            SenderDisplayName = policy.SenderDisplayName,
            Subject = TestSubject,
            SenderName = "Portfolio Admin",
            ReplyToEmail = policy.RecipientAddress,
            MessageBody = TestBody
        };

        return await SendAsync(email, cancellationToken);
    }

    private async Task<EmailDeliverySendResult> SendAsync(
        EmailToSend email,
        CancellationToken cancellationToken)
    {
        try
        {
            await emailSender.SendEmailAsync(email, cancellationToken);
            return new(EmailDeliverySendOutcome.Succeeded);
        }
        catch (EmailDeliveryException exception) when (exception.Kind == EmailDeliveryFailureKind.Timeout)
        {
            return new(EmailDeliverySendOutcome.TimedOut);
        }
        catch (EmailDeliveryException)
        {
            return new(EmailDeliverySendOutcome.Unavailable);
        }
    }
}

public enum EmailDeliveryPolicyUpdateOutcome
{
    Succeeded,
    InvalidPolicy,
    StaleRevision
}

public sealed record EmailDeliveryPolicyUpdateResult(
    EmailDeliveryPolicyUpdateOutcome Outcome,
    EmailDeliveryPolicy? Policy = null);
