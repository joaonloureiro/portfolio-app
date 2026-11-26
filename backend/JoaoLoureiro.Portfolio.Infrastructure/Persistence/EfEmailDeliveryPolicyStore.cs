using JoaoLoureiro.Portfolio.Core.EmailDelivery;
using JoaoLoureiro.Portfolio.Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace JoaoLoureiro.Portfolio.Infrastructure.Persistence;

public sealed class EfEmailDeliveryPolicyStore(
    PortfolioDbContext dbContext,
    IOptions<EmailDeliveryPolicySeedOptions> seedOptions)
    : IEmailDeliveryPolicyStore
{
    private const int PolicyId = 1;

    public async Task<EmailDeliveryPolicy> GetOrSeedAsync(CancellationToken cancellationToken)
    {
        try
        {
            var record = await dbContext.EmailDeliveryPolicies.SingleOrDefaultAsync(
                policy => policy.Id == PolicyId,
                cancellationToken);
            if (record is null)
            {
                record = new EmailDeliveryPolicyRecord
                {
                    Id = PolicyId,
                    Enabled = seedOptions.Value.InitialEnabled,
                    RecipientAddress = seedOptions.Value.InitialRecipientAddress,
                    SenderDisplayName = seedOptions.Value.InitialSenderDisplayName,
                    SubjectPrefix = seedOptions.Value.InitialSubjectPrefix,
                    Revision = 1
                };
                dbContext.EmailDeliveryPolicies.Add(record);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return ToPolicy(record);
        }
        catch (DbUpdateException exception)
        {
            dbContext.ChangeTracker.Clear();
            var existing = await dbContext.EmailDeliveryPolicies.SingleOrDefaultAsync(
                policy => policy.Id == PolicyId,
                cancellationToken);
            return existing is not null
                ? ToPolicy(existing)
                : throw new EmailDeliveryPolicyStorageUnavailableException("seed the policy", exception);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new EmailDeliveryPolicyStorageUnavailableException("read the policy", exception);
        }
    }

    public async Task<EmailDeliveryPolicyStoreMutationResult> UpdateAsync(
        EmailDeliveryPolicy policy,
        long expectedRevision,
        CancellationToken cancellationToken)
    {
        try
        {
            var record = await dbContext.EmailDeliveryPolicies.SingleOrDefaultAsync(
                current => current.Id == PolicyId,
                cancellationToken);
            if (record is null)
            {
                _ = await GetOrSeedAsync(cancellationToken);
                record = await dbContext.EmailDeliveryPolicies.SingleAsync(
                    current => current.Id == PolicyId,
                    cancellationToken);
            }

            if (record.Revision != expectedRevision)
            {
                return EmailDeliveryPolicyStoreMutationResult.Conflict;
            }

            record.Enabled = policy.Enabled;
            record.RecipientAddress = policy.RecipientAddress;
            record.SenderDisplayName = policy.SenderDisplayName;
            record.SubjectPrefix = policy.SubjectPrefix;
            record.Revision = expectedRevision + 1;
            await dbContext.SaveChangesAsync(cancellationToken);
            return EmailDeliveryPolicyStoreMutationResult.Succeeded;
        }
        catch (DbUpdateConcurrencyException)
        {
            dbContext.ChangeTracker.Clear();
            return EmailDeliveryPolicyStoreMutationResult.Conflict;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new EmailDeliveryPolicyStorageUnavailableException("update the policy", exception);
        }
    }

    private static EmailDeliveryPolicy ToPolicy(EmailDeliveryPolicyRecord record)
    {
        return EmailDeliveryPolicy.Create(
            record.Enabled,
            record.RecipientAddress,
            record.SenderDisplayName,
            record.SubjectPrefix,
            record.Revision);
    }
}
