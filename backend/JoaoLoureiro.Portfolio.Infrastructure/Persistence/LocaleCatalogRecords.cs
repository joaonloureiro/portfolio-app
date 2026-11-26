namespace JoaoLoureiro.Portfolio.Infrastructure.Persistence;

public sealed class LocaleCatalogRecord
{
    public int Id { get; set; } = 1;

    public long Revision { get; set; }

    public DateTime UpdatedAt { get; set; }

    public List<LocaleCatalogEntryRecord> Entries { get; set; } = [];
}

public sealed class LocaleCatalogEntryRecord
{
    public int CatalogId { get; set; }

    public string Key { get; set; } = string.Empty;

    public string Locale { get; set; } = string.Empty;

    public string ValueType { get; set; } = string.Empty;

    public string? ScalarValue { get; set; }

    public string? ListValueJson { get; set; }

    public LocaleCatalogRecord? Catalog { get; set; }
}
