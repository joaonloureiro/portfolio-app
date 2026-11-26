using System.Data.Common;
using System.Text.Json;
using JoaoLoureiro.Portfolio.Core.Locales;
using JoaoLoureiro.Portfolio.Core.Posts;
using Microsoft.EntityFrameworkCore;

namespace JoaoLoureiro.Portfolio.Infrastructure.Persistence;

public sealed class EfLocaleCatalogStore(PortfolioDbContext dbContext, IClock clock) : ILocaleCatalogStore
{
    public async Task<LocaleCatalogRevision> GetActiveAsync(CancellationToken cancellationToken)
    {
        try
        {
            var record = await GetOrSeedAsync(cancellationToken);
            return ToDomain(record);
        }
        catch (LocaleCatalogStorageUnavailableException)
        {
            throw;
        }
        catch (Exception exception) when (IsStorageException(exception))
        {
            dbContext.ChangeTracker.Clear();
            throw CreateStorageException("read the locale catalog", exception);
        }
    }

    public async Task<LocaleCatalogStoreMutationResult> ReplaceAsync(
        long expectedRevision,
        IReadOnlyList<LocaleCatalogEntry> entries,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var record = await GetOrSeedAsync(cancellationToken);
            if (record.Revision != expectedRevision)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                dbContext.ChangeTracker.Clear();
                return LocaleCatalogStoreMutationResult.Conflict;
            }

            var desired = entries
                .SelectMany(entry => new[]
                {
                    ToRecord(record.Id, entry.Key, LocaleCatalogLocale.En, entry.Type == LocaleCatalogValueType.String ? entry.English : entry.English),
                    ToRecord(record.Id, entry.Key, LocaleCatalogLocale.Pt, entry.Type == LocaleCatalogValueType.String ? entry.Portuguese : entry.Portuguese)
                })
                .ToDictionary(entry => (entry.Key, entry.Locale), entry => entry, StringTupleComparer.Instance);

            foreach (var existing in record.Entries.ToArray())
            {
                if (!desired.TryGetValue((existing.Key, existing.Locale), out var replacement))
                {
                    dbContext.LocaleCatalogEntries.Remove(existing);
                    continue;
                }

                existing.ValueType = replacement.ValueType;
                existing.ScalarValue = replacement.ScalarValue;
                existing.ListValueJson = replacement.ListValueJson;
                desired.Remove((existing.Key, existing.Locale));
            }

            foreach (var entry in desired.Values)
            {
                record.Entries.Add(entry);
            }

            record.Revision++;
            record.UpdatedAt = RequireUtc(clock.UtcNow);
            dbContext.Entry(record).Property(value => value.Revision).OriginalValue = expectedRevision;
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return LocaleCatalogStoreMutationResult.Succeeded;
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            dbContext.ChangeTracker.Clear();
            return LocaleCatalogStoreMutationResult.Conflict;
        }
        catch (Exception exception) when (IsStorageException(exception))
        {
            await transaction.RollbackAsync(CancellationToken.None);
            dbContext.ChangeTracker.Clear();
            throw CreateStorageException("replace the locale catalog", exception);
        }
    }

    private async Task<LocaleCatalogRecord> GetOrSeedAsync(CancellationToken cancellationToken)
    {
        var record = await Query().SingleOrDefaultAsync(cancellationToken);
        if (record is not null)
        {
            return record;
        }

        var now = RequireUtc(clock.UtcNow);
        record = new LocaleCatalogRecord
        {
            Id = 1,
            Revision = 1,
            UpdatedAt = now
        };
        record.Entries.AddRange(LocaleCatalogSeedData.CreateEntries()
            .SelectMany(entry => new[]
            {
                ToRecord(record.Id, entry.Key, LocaleCatalogLocale.En, entry.English),
                ToRecord(record.Id, entry.Key, LocaleCatalogLocale.Pt, entry.Portuguese)
            }));

        try
        {
            dbContext.LocaleCatalogs.Add(record);
            await dbContext.SaveChangesAsync(cancellationToken);
            return record;
        }
        catch (DbUpdateException)
        {
            dbContext.ChangeTracker.Clear();
            return await Query().SingleAsync(cancellationToken);
        }
    }

    private IQueryable<LocaleCatalogRecord> Query()
    {
        return dbContext.LocaleCatalogs.Include(catalog => catalog.Entries);
    }

    private static LocaleCatalogRevision ToDomain(LocaleCatalogRecord record)
    {
        var grouped = record.Entries
            .GroupBy(entry => entry.Key, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToDictionary(entry => entry.Locale, StringComparer.Ordinal), StringComparer.Ordinal);

        var entries = new List<LocaleCatalogEntry>(LocaleCatalogContract.Definitions.Count);
        IReadOnlyDictionary<string, LocaleCatalogEntry>? defaults = null;
        foreach (var definition in LocaleCatalogContract.Definitions)
        {
            // Existing catalogs use defaults for keys added after the last save.
            if (!grouped.TryGetValue(definition.Key, out var values))
            {
                defaults ??= LocaleCatalogSeedData.CreateEntries()
                    .ToDictionary(entry => entry.Key, StringComparer.Ordinal);
                entries.Add(defaults[definition.Key]);
                continue;
            }

            if (!values.TryGetValue("en", out var english) ||
                !values.TryGetValue("pt", out var portuguese))
            {
                throw new LocaleCatalogStorageUnavailableException("read an incomplete locale catalog", new InvalidOperationException());
            }

            entries.Add(new LocaleCatalogEntry(
                definition.Key,
                definition.Type,
                ToValue(english, definition),
                ToValue(portuguese, definition)));
        }

        return new LocaleCatalogRevision(record.Revision, entries, RequireUtc(record.UpdatedAt));
    }

    private static LocaleCatalogValue ToValue(LocaleCatalogEntryRecord record, LocaleCatalogKeyDefinition definition)
    {
        if (!Enum.TryParse<LocaleCatalogValueType>(record.ValueType, ignoreCase: false, out var type) || type != definition.Type)
        {
            throw new LocaleCatalogStorageUnavailableException("read a locale value with an invalid type", new InvalidOperationException());
        }

        try
        {
            return type == LocaleCatalogValueType.String
                ? LocaleCatalogValue.FromString(record.ScalarValue ?? throw new InvalidOperationException())
                : LocaleCatalogValue.FromList(JsonSerializer.Deserialize<string[]>(record.ListValueJson ?? "") ?? throw new InvalidOperationException());
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or LocaleCatalogDomainException)
        {
            throw new LocaleCatalogStorageUnavailableException("read an invalid locale value", exception);
        }
    }

    private static LocaleCatalogEntryRecord ToRecord(
        int catalogId,
        string key,
        LocaleCatalogLocale locale,
        LocaleCatalogValue value)
    {
        return new LocaleCatalogEntryRecord
        {
            CatalogId = catalogId,
            Key = key,
            Locale = locale.ToCode(),
            ValueType = value.Type.ToString(),
            ScalarValue = value.Scalar,
            ListValueJson = value.List is null ? null : JsonSerializer.Serialize(value.List)
        };
    }

    private static DateTime RequireUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    private static bool IsStorageException(Exception exception)
    {
        return exception is DbUpdateException or DbUpdateConcurrencyException or DbException;
    }

    private static LocaleCatalogStorageUnavailableException CreateStorageException(string operation, Exception exception)
    {
        return new LocaleCatalogStorageUnavailableException(operation, exception);
    }

    private sealed class StringTupleComparer : IEqualityComparer<(string Key, string Locale)>
    {
        public static StringTupleComparer Instance { get; } = new();

        public bool Equals((string Key, string Locale) x, (string Key, string Locale) y) =>
            StringComparer.Ordinal.Equals(x.Key, y.Key) && StringComparer.Ordinal.Equals(x.Locale, y.Locale);

        public int GetHashCode((string Key, string Locale) obj) =>
            HashCode.Combine(StringComparer.Ordinal.GetHashCode(obj.Key), StringComparer.Ordinal.GetHashCode(obj.Locale));
    }
}
