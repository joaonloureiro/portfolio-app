namespace JoaoLoureiro.Portfolio.Infrastructure.Persistence;

public sealed class ContentStoreOptions
{
    public const string SectionName = "ContentStore";

    public string Path { get; init; } = string.Empty;
}
