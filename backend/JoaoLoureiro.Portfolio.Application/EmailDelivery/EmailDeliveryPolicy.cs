using System.Net.Mail;

namespace JoaoLoureiro.Portfolio.Core.EmailDelivery;

public static class EmailDeliveryPolicyLimits
{
    public const int MaxRecipientAddressLength = 320;
    public const int MaxSenderDisplayNameLength = 120;
    public const int MaxSubjectPrefixLength = 120;
}

public static class EmailDeliveryErrorKeys
{
    public const string InvalidPolicy = "email_delivery_invalid_policy";
    public const string StaleRevision = "email_delivery_stale_revision";
    public const string StorageUnavailable = "email_delivery_storage_unavailable";
}

public sealed class EmailDeliveryPolicyValidationException(string errorKey)
    : Exception("The email delivery policy is invalid.")
{
    public string ErrorKey { get; } = errorKey;
}

public sealed record EmailDeliveryPolicy(
    bool Enabled,
    string RecipientAddress,
    string SenderDisplayName,
    string SubjectPrefix,
    long Revision)
{
    public static EmailDeliveryPolicy Create(
        bool enabled,
        string? recipientAddress,
        string? senderDisplayName,
        string? subjectPrefix,
        long revision)
    {
        if (revision < 1 ||
            !TryNormalizeAddress(recipientAddress, out var normalizedRecipient) ||
            !TryNormalizeText(senderDisplayName, EmailDeliveryPolicyLimits.MaxSenderDisplayNameLength, out var normalizedDisplayName) ||
            !TryNormalizeText(subjectPrefix, EmailDeliveryPolicyLimits.MaxSubjectPrefixLength, out var normalizedSubjectPrefix))
        {
            throw new EmailDeliveryPolicyValidationException(EmailDeliveryErrorKeys.InvalidPolicy);
        }

        return new(enabled, normalizedRecipient, normalizedDisplayName, normalizedSubjectPrefix, revision);
    }

    public static bool TryNormalizeAddress(string? value, out string normalized)
    {
        normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length is 0 or > EmailDeliveryPolicyLimits.MaxRecipientAddressLength ||
            normalized.Any(char.IsControl) || normalized.Contains(',', StringComparison.Ordinal))
        {
            normalized = string.Empty;
            return false;
        }

        try
        {
            var address = new MailAddress(normalized);
            return string.Equals(address.Address, normalized, StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            normalized = string.Empty;
            return false;
        }
    }

    public static bool TryNormalizeText(string? value, int maximumLength, out string normalized)
    {
        normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length is 0 || normalized.Length > maximumLength || normalized.Any(char.IsControl))
        {
            normalized = string.Empty;
            return false;
        }

        return true;
    }
}

public sealed record EmailDeliveryPolicyInput(
    bool Enabled,
    string? RecipientAddress,
    string? SenderDisplayName,
    string? SubjectPrefix);

public enum EmailDeliveryPolicyStoreMutationResult
{
    Succeeded,
    Conflict
}

public sealed class EmailDeliveryPolicyStorageUnavailableException(string operation, Exception innerException)
    : Exception("The email delivery policy store is unavailable.", innerException)
{
    public string Operation { get; } = operation;
}

public interface IEmailDeliveryPolicyStore
{
    Task<EmailDeliveryPolicy> GetOrSeedAsync(CancellationToken cancellationToken);

    Task<EmailDeliveryPolicyStoreMutationResult> UpdateAsync(
        EmailDeliveryPolicy policy,
        long expectedRevision,
        CancellationToken cancellationToken);
}

public enum EmailDeliverySendOutcome
{
    Succeeded,
    Disabled,
    TimedOut,
    Unavailable
}

public sealed record EmailDeliverySendResult(EmailDeliverySendOutcome Outcome);
