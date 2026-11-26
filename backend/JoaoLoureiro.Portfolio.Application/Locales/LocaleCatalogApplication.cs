using System.Collections.ObjectModel;
using System.Text.Json;

namespace JoaoLoureiro.Portfolio.Core.Locales;

public enum LocaleCatalogValueType
{
    String,
    StringList
}

public enum LocaleCatalogLocale
{
    En,
    Pt
}

public static class LocaleCatalogLocaleExtensions
{
    public static string ToCode(this LocaleCatalogLocale locale) => locale switch
    {
        LocaleCatalogLocale.En => "en",
        LocaleCatalogLocale.Pt => "pt",
        _ => throw new ArgumentOutOfRangeException(nameof(locale), locale, null)
    };

    public static bool TryParse(string? value, out LocaleCatalogLocale locale)
    {
        if (string.Equals(value, "en", StringComparison.OrdinalIgnoreCase))
        {
            locale = LocaleCatalogLocale.En;
            return true;
        }

        if (string.Equals(value, "pt", StringComparison.OrdinalIgnoreCase))
        {
            locale = LocaleCatalogLocale.Pt;
            return true;
        }

        locale = default;
        return false;
    }
}

public static class LocaleCatalogErrorKeys
{
    public const string InvalidShape = "locale_invalid_shape";
    public const string UnknownKey = "locale_unknown_key";
    public const string InvalidValue = "locale_invalid_value";
    public const string StaleRevision = "locale_stale_revision";
    public const string UnsupportedLocale = "locale_unsupported";
    public const string StorageUnavailable = "locale_storage_unavailable";
}

public sealed class LocaleCatalogDomainException(string errorKey, string message) : Exception(message)
{
    public string ErrorKey { get; } = errorKey;
}

public static class LocaleCatalogLimits
{
    public const int MaxRequestBytes = 256 * 1024;
    public const int MaxKeyLength = 96;
    public const int MaxKeyDepth = 4;
    public const int MaxScalarLength = 2_000;
    public const int MaxListItemCount = 20;
    public const int MaxListItemLength = 120;
}

public sealed record LocaleCatalogKeyDefinition(string Key, LocaleCatalogValueType Type);

public sealed record LocaleCatalogValue
{
    private LocaleCatalogValue(string? scalar, IReadOnlyList<string>? list)
    {
        Scalar = scalar;
        List = list;
    }

    public string? Scalar { get; }

    public IReadOnlyList<string>? List { get; }

    public LocaleCatalogValueType Type => List is null
        ? LocaleCatalogValueType.String
        : LocaleCatalogValueType.StringList;

    public static LocaleCatalogValue FromString(string value)
    {
        if (value is null || value.Length > LocaleCatalogLimits.MaxScalarLength || ContainsMarkup(value))
        {
            throw new LocaleCatalogDomainException(LocaleCatalogErrorKeys.InvalidValue, "A locale string exceeds its length limit.");
        }

        return new LocaleCatalogValue(value, null);
    }

    public static LocaleCatalogValue FromList(IReadOnlyList<string> values)
    {
        if (values is null || values.Count is < 1 or > LocaleCatalogLimits.MaxListItemCount ||
            values.Any(value => value is null || value.Length > LocaleCatalogLimits.MaxListItemLength || ContainsMarkup(value)))
        {
            throw new LocaleCatalogDomainException(LocaleCatalogErrorKeys.InvalidValue, "A locale string list is outside its limits.");
        }

        return new LocaleCatalogValue(null, values.ToArray());
    }

    private static bool ContainsMarkup(string value) => value.Contains('<') || value.Contains('>');

    public static bool TryRead(JsonElement value, LocaleCatalogValueType expectedType, out LocaleCatalogValue? result)
    {
        try
        {
            if (expectedType == LocaleCatalogValueType.String && value.ValueKind == JsonValueKind.String)
            {
                result = FromString(value.GetString()!);
                return true;
            }

            if (expectedType == LocaleCatalogValueType.StringList && value.ValueKind == JsonValueKind.Array)
            {
                var values = new List<string>();
                foreach (var item in value.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.String)
                    {
                        result = null;
                        return false;
                    }

                    values.Add(item.GetString()!);
                }

                result = FromList(values);
                return true;
            }
        }
        catch (LocaleCatalogDomainException)
        {
            result = null;
            return false;
        }

        result = null;
        return false;
    }
}

public sealed record LocaleCatalogEntry(
    string Key,
    LocaleCatalogValueType Type,
    LocaleCatalogValue English,
    LocaleCatalogValue Portuguese);

public sealed record LocaleCatalogRevision(
    long Revision,
    IReadOnlyList<LocaleCatalogEntry> Entries,
    DateTime UpdatedAt);

public sealed record LocaleCatalogChange(
    LocaleCatalogValue English,
    LocaleCatalogValue Portuguese);

public sealed record ReplaceLocaleCatalogCommand(
    long ExpectedRevision,
    IReadOnlyDictionary<string, LocaleCatalogChange> Changes);

public enum LocaleCatalogStoreMutationResult
{
    Succeeded,
    Conflict
}

public sealed class LocaleCatalogStorageUnavailableException(string operation, Exception innerException)
    : Exception($"The locale catalog store could not {operation}.", innerException)
{
    public string Operation { get; } = operation;
}

public interface ILocaleCatalogStore
{
    Task<LocaleCatalogRevision> GetActiveAsync(CancellationToken cancellationToken);

    Task<LocaleCatalogStoreMutationResult> ReplaceAsync(
        long expectedRevision,
        IReadOnlyList<LocaleCatalogEntry> entries,
        CancellationToken cancellationToken);
}

public static class LocaleCatalogContract
{
    // about.principles is intentionally excluded: it is an array of objects, not a scalar
    // or ordered string list. It remains in the built-in public catalog and has no Admin edit path.
    // Project translation paths are owned by project records, and post bodies are owned by
    // post records; the current message files contain no project_* or post-content leaves.
    public static readonly IReadOnlyList<LocaleCatalogKeyDefinition> Definitions =
        new ReadOnlyCollection<LocaleCatalogKeyDefinition>(
        [
            new("metadata.home_title", LocaleCatalogValueType.String),
            new("metadata.home_description", LocaleCatalogValueType.String),
            new("metadata.projects_title", LocaleCatalogValueType.String),
            new("metadata.projects_description", LocaleCatalogValueType.String),
            new("metadata.about_title", LocaleCatalogValueType.String),
            new("metadata.about_description", LocaleCatalogValueType.String),
            new("engagement.meta_title", LocaleCatalogValueType.String),
            new("engagement.meta_description", LocaleCatalogValueType.String),
            new("engagement.skip", LocaleCatalogValueType.String),
            new("engagement.headline", LocaleCatalogValueType.String),
            new("engagement.headline_accent", LocaleCatalogValueType.String),
            new("engagement.intro", LocaleCatalogValueType.String),
            new("engagement.start", LocaleCatalogValueType.String),
            new("engagement.view_work", LocaleCatalogValueType.String),
            new("engagement.rates", LocaleCatalogValueType.String),
            new("engagement.monthly", LocaleCatalogValueType.String),
            new("engagement.monthly_price", LocaleCatalogValueType.String),
            new("engagement.monthly_unit", LocaleCatalogValueType.String),
            new("engagement.monthly_summary", LocaleCatalogValueType.String),
            new("engagement.task", LocaleCatalogValueType.String),
            new("engagement.task_price", LocaleCatalogValueType.String),
            new("engagement.task_unit", LocaleCatalogValueType.String),
            new("engagement.task_summary", LocaleCatalogValueType.String),
            new("engagement.work", LocaleCatalogValueType.String),
            new("engagement.work_heading", LocaleCatalogValueType.String),
            new("engagement.work_intro", LocaleCatalogValueType.String),
            new("engagement.work_empty", LocaleCatalogValueType.String),
            new("engagement.about_heading", LocaleCatalogValueType.String),
            new("engagement.about_intro", LocaleCatalogValueType.String),
            new("engagement.backend_title", LocaleCatalogValueType.String),
            new("engagement.backend_description", LocaleCatalogValueType.String),
            new("engagement.frontend_title", LocaleCatalogValueType.String),
            new("engagement.frontend_description", LocaleCatalogValueType.String),
            new("engagement.delivery_title", LocaleCatalogValueType.String),
            new("engagement.delivery_description", LocaleCatalogValueType.String),
            new("engagement.contact_heading", LocaleCatalogValueType.String),
            new("engagement.contact_intro", LocaleCatalogValueType.String),
            new("engagement.form_engagement", LocaleCatalogValueType.String),
            new("engagement.form_unsure", LocaleCatalogValueType.String),
            new("navigation.home", LocaleCatalogValueType.String),
            new("navigation.overview", LocaleCatalogValueType.String),
            new("navigation.interfaces", LocaleCatalogValueType.String),
            new("navigation.services", LocaleCatalogValueType.String),
            new("navigation.automation", LocaleCatalogValueType.String),
            new("navigation.infrastructure", LocaleCatalogValueType.String),
            new("navigation.about", LocaleCatalogValueType.String),
            new("navigation.tech", LocaleCatalogValueType.String),
            new("navigation.projects", LocaleCatalogValueType.String),
            new("navigation.posts", LocaleCatalogValueType.String),
            new("navigation.notes", LocaleCatalogValueType.String),
            new("navigation.contact", LocaleCatalogValueType.String),
            new("navigation.atlas_label", LocaleCatalogValueType.String),
            new("navigation.primary_label", LocaleCatalogValueType.String),
            new("navigation.secondary_label", LocaleCatalogValueType.String),
            new("navigation.social_label", LocaleCatalogValueType.String),
            new("navigation.language", LocaleCatalogValueType.String),
            new("navigation.switch_to_light", LocaleCatalogValueType.String),
            new("navigation.switch_to_dark", LocaleCatalogValueType.String),
            new("navigation.open_menu", LocaleCatalogValueType.String),
            new("navigation.close_menu", LocaleCatalogValueType.String),
            new("hero.greeting", LocaleCatalogValueType.String),
            new("hero.title", LocaleCatalogValueType.String),
            new("hero.subtitle", LocaleCatalogValueType.String),
            new("hero.role", LocaleCatalogValueType.String),
            new("hero.rail_intro", LocaleCatalogValueType.String),
            new("hero.manifesto_label", LocaleCatalogValueType.String),
            new("hero.headline", LocaleCatalogValueType.String),
            new("hero.manifesto", LocaleCatalogValueType.String),
            new("hero.cta_button", LocaleCatalogValueType.String),
            new("atlas.interfaces.title", LocaleCatalogValueType.String),
            new("atlas.interfaces.points", LocaleCatalogValueType.StringList),
            new("atlas.services.title", LocaleCatalogValueType.String),
            new("atlas.services.points", LocaleCatalogValueType.StringList),
            new("atlas.automation.title", LocaleCatalogValueType.String),
            new("atlas.automation.points", LocaleCatalogValueType.StringList),
            new("atlas.infrastructure.title", LocaleCatalogValueType.String),
            new("atlas.infrastructure.points", LocaleCatalogValueType.StringList),
            new("notes.title", LocaleCatalogValueType.String),
            new("notes.status", LocaleCatalogValueType.String),
            new("notes.description", LocaleCatalogValueType.String),
            new("notes.published_status", LocaleCatalogValueType.String),
            new("notes.published_description", LocaleCatalogValueType.String),
            new("notes.published_link", LocaleCatalogValueType.String),
            new("notes.purpose_title", LocaleCatalogValueType.String),
            new("notes.purpose", LocaleCatalogValueType.String),
            new("posts.title", LocaleCatalogValueType.String),
            new("posts.description", LocaleCatalogValueType.String),
            new("posts.list_label", LocaleCatalogValueType.String),
            new("posts.loading", LocaleCatalogValueType.String),
            new("posts.empty", LocaleCatalogValueType.String),
            new("posts.unavailable", LocaleCatalogValueType.String),
            new("posts.back_to_posts", LocaleCatalogValueType.String),
            new("posts.related_title", LocaleCatalogValueType.String),
            new("about.title", LocaleCatalogValueType.String),
            new("about.statement", LocaleCatalogValueType.String),
            new("about.paragraph1", LocaleCatalogValueType.String),
            new("about.paragraph2", LocaleCatalogValueType.String),
            new("about.paragraph3", LocaleCatalogValueType.String),
            new("skills.title", LocaleCatalogValueType.String),
            new("skills.backend", LocaleCatalogValueType.String),
            new("skills.frontend", LocaleCatalogValueType.String),
            new("skills.databases", LocaleCatalogValueType.String),
            new("skills.cloud", LocaleCatalogValueType.String),
            new("projects.title", LocaleCatalogValueType.String),
            new("projects.intro", LocaleCatalogValueType.String),
            new("projects.entry_label", LocaleCatalogValueType.String),
            new("projects.tech_used", LocaleCatalogValueType.String),
            new("projects.page_link", LocaleCatalogValueType.String),
            new("projects.live_link", LocaleCatalogValueType.String),
            new("projects.repo_link", LocaleCatalogValueType.String),
            new("projects.back_to_projects", LocaleCatalogValueType.String),
            new("projects.about_project", LocaleCatalogValueType.String),
            new("projects.not_found", LocaleCatalogValueType.String),
            new("projects.unavailable", LocaleCatalogValueType.String),
            new("contact.title", LocaleCatalogValueType.String),
            new("contact.subtitle", LocaleCatalogValueType.String),
            new("contact.direct_label", LocaleCatalogValueType.String),
            new("contact.form_name", LocaleCatalogValueType.String),
            new("contact.form_email", LocaleCatalogValueType.String),
            new("contact.form_message", LocaleCatalogValueType.String),
            new("contact.field_required", LocaleCatalogValueType.String),
            new("contact.privacy_note", LocaleCatalogValueType.String),
            new("contact.submit_button", LocaleCatalogValueType.String),
            new("contact.status_sending", LocaleCatalogValueType.String),
            new("contact.status_success", LocaleCatalogValueType.String),
            new("contact.status_error_failed", LocaleCatalogValueType.String),
            new("contact.status_error_all_fields", LocaleCatalogValueType.String),
            new("contact.status_error_invalid_email", LocaleCatalogValueType.String),
            new("contact.status_error_generic", LocaleCatalogValueType.String),
            new("contact.server_unexpected_error", LocaleCatalogValueType.String),
            new("contact.smtp_auth_failed", LocaleCatalogValueType.String),
            new("contact.smtp_connection_failed", LocaleCatalogValueType.String),
            new("contact.smtp_invalid_recipient", LocaleCatalogValueType.String),
            new("contact.smtp_generic_error", LocaleCatalogValueType.String),
            new("contact.contact_invalid_input", LocaleCatalogValueType.String),
            new("contact.contact_payload_too_large", LocaleCatalogValueType.String),
            new("contact.contact_throttled", LocaleCatalogValueType.String),
            new("contact.contact_delivery_disabled", LocaleCatalogValueType.String),
            new("contact.contact_delivery_timeout", LocaleCatalogValueType.String),
            new("contact.contact_delivery_unavailable", LocaleCatalogValueType.String),
            new("notifications.context", LocaleCatalogValueType.String),
            new("notifications.success", LocaleCatalogValueType.String),
            new("notifications.error", LocaleCatalogValueType.String),
            new("notifications.loading", LocaleCatalogValueType.String),
            new("notifications.update", LocaleCatalogValueType.String),
            new("notifications.dismiss", LocaleCatalogValueType.String),
            new("footer.copyright", LocaleCatalogValueType.String),
            new("footer.socials_title", LocaleCatalogValueType.String),
            new("footer.credit", LocaleCatalogValueType.String)
        ]);

    private static readonly IReadOnlyDictionary<string, LocaleCatalogKeyDefinition> DefinitionMap =
        Definitions.ToDictionary(definition => definition.Key, StringComparer.Ordinal);

    public static bool TryGetDefinition(string key, out LocaleCatalogKeyDefinition? definition)
    {
        if (DefinitionMap.TryGetValue(key, out var found))
        {
            definition = found;
            return true;
        }

        definition = null;
        return false;
    }

    public static bool IsValidKeyShape(string? key)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length > LocaleCatalogLimits.MaxKeyLength)
        {
            return false;
        }

        var parts = key.Split('.');
        return parts.Length <= LocaleCatalogLimits.MaxKeyDepth &&
            parts.All(part => part.Length > 0 && part.All(character => char.IsLetterOrDigit(character) || character == '_'));
    }

    public static IReadOnlyList<LocaleCatalogEntry> ApplyChanges(
        IReadOnlyList<LocaleCatalogEntry> current,
        IReadOnlyDictionary<string, LocaleCatalogChange> changes)
    {
        var currentByKey = current.ToDictionary(entry => entry.Key, StringComparer.Ordinal);
        foreach (var (key, change) in changes)
        {
            if (!TryGetDefinition(key, out var definition))
            {
                throw new LocaleCatalogDomainException(LocaleCatalogErrorKeys.UnknownKey, "The locale key is not editable.");
            }

            ValidateKey(key);
            ValidateValueType(definition!, change.English);
            ValidateValueType(definition!, change.Portuguese);
            currentByKey[key] = new LocaleCatalogEntry(key, definition!.Type, change.English, change.Portuguese);
        }

        var entries = Definitions.Select(definition =>
        {
            if (!currentByKey.TryGetValue(definition.Key, out var entry))
            {
                throw new LocaleCatalogDomainException(LocaleCatalogErrorKeys.InvalidShape, "The active locale catalog is incomplete.");
            }

            ValidateValueType(definition, entry.English);
            ValidateValueType(definition, entry.Portuguese);
            return new LocaleCatalogEntry(definition.Key, definition.Type, entry.English, entry.Portuguese);
        }).ToArray();

        return entries;
    }

    public static void ValidateChanges(ReplaceLocaleCatalogCommand command)
    {
        if (command.ExpectedRevision < 1 || command.Changes is null || command.Changes.Count is < 1 or > 128)
        {
            throw new LocaleCatalogDomainException(LocaleCatalogErrorKeys.InvalidShape, "The locale update shape is invalid.");
        }

        if (command.Changes.Keys.Distinct(StringComparer.Ordinal).Count() != command.Changes.Count)
        {
            throw new LocaleCatalogDomainException(LocaleCatalogErrorKeys.InvalidShape, "The locale update contains duplicate keys.");
        }

        foreach (var (key, change) in command.Changes)
        {
            ValidateKey(key);
            if (!TryGetDefinition(key, out var definition))
            {
                throw new LocaleCatalogDomainException(LocaleCatalogErrorKeys.UnknownKey, "The locale key is not editable.");
            }

            if (change is null)
            {
                throw new LocaleCatalogDomainException(LocaleCatalogErrorKeys.InvalidShape, "Both locale values are required.");
            }

            ValidateValueType(definition!, change.English);
            ValidateValueType(definition!, change.Portuguese);
        }
    }

    private static void ValidateKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length > LocaleCatalogLimits.MaxKeyLength ||
            key.Split('.').Length > LocaleCatalogLimits.MaxKeyDepth ||
            key.Split('.').Any(part => part.Length == 0 || part.Any(character => !char.IsLetterOrDigit(character) && character != '_')))
        {
            throw new LocaleCatalogDomainException(LocaleCatalogErrorKeys.InvalidShape, "The locale key path is invalid.");
        }
    }

    private static void ValidateValueType(LocaleCatalogKeyDefinition definition, LocaleCatalogValue value)
    {
        if (value is null || value.Type != definition.Type)
        {
            throw new LocaleCatalogDomainException(LocaleCatalogErrorKeys.InvalidShape, "The locale value type does not match the catalog contract.");
        }

        if (definition.Type == LocaleCatalogValueType.String &&
            (value.Scalar is null || value.Scalar.Length > LocaleCatalogLimits.MaxScalarLength))
        {
            throw new LocaleCatalogDomainException(LocaleCatalogErrorKeys.InvalidValue, "A locale string exceeds its length limit.");
        }

        if (definition.Type == LocaleCatalogValueType.StringList &&
            (value.List is null || value.List.Count is < 1 or > LocaleCatalogLimits.MaxListItemCount ||
             value.List.Any(item => item is null || item.Length > LocaleCatalogLimits.MaxListItemLength)))
        {
            throw new LocaleCatalogDomainException(LocaleCatalogErrorKeys.InvalidValue, "A locale string list is outside its limits.");
        }
    }
}

public enum LocaleCatalogOperationOutcome
{
    Succeeded,
    InvalidShape,
    UnknownKey,
    InvalidValue,
    StaleRevision,
    UnsupportedLocale
}

public sealed record LocaleCatalogOperationResult(
    LocaleCatalogOperationOutcome Outcome,
    LocaleCatalogRevision? Catalog = null);

public sealed class LocaleCatalogApplicationService(ILocaleCatalogStore store)
{
    public async Task<LocaleCatalogOperationResult> GetAdminAsync(CancellationToken cancellationToken)
    {
        return new(LocaleCatalogOperationOutcome.Succeeded, await store.GetActiveAsync(cancellationToken));
    }

    public async Task<LocaleCatalogOperationResult> GetPublicAsync(
        string? locale,
        CancellationToken cancellationToken)
    {
        if (!LocaleCatalogLocaleExtensions.TryParse(locale, out _))
        {
            return new(LocaleCatalogOperationOutcome.UnsupportedLocale);
        }

        return new(LocaleCatalogOperationOutcome.Succeeded, await store.GetActiveAsync(cancellationToken));
    }

    public async Task<LocaleCatalogOperationResult> ReplaceAsync(
        ReplaceLocaleCatalogCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            LocaleCatalogContract.ValidateChanges(command);
            var current = await store.GetActiveAsync(cancellationToken);
            var updatedEntries = LocaleCatalogContract.ApplyChanges(current.Entries, command.Changes);
            var mutation = await store.ReplaceAsync(command.ExpectedRevision, updatedEntries, cancellationToken);
            return mutation == LocaleCatalogStoreMutationResult.Succeeded
                ? new(LocaleCatalogOperationOutcome.Succeeded, new LocaleCatalogRevision(
                    command.ExpectedRevision + 1,
                    updatedEntries,
                    DateTime.UtcNow))
                : new(LocaleCatalogOperationOutcome.StaleRevision);
        }
        catch (LocaleCatalogDomainException exception)
        {
            return exception.ErrorKey switch
            {
                LocaleCatalogErrorKeys.UnknownKey => new(LocaleCatalogOperationOutcome.UnknownKey),
                LocaleCatalogErrorKeys.InvalidValue => new(LocaleCatalogOperationOutcome.InvalidValue),
                _ => new(LocaleCatalogOperationOutcome.InvalidShape)
            };
        }
    }
}
