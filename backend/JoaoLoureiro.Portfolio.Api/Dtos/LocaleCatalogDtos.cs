using System.Text.Json;
using System.Text.Json.Nodes;

namespace JoaoLoureiro.Portfolio.Api.Dtos;

public sealed record LocaleCatalogChangeRequest(
    JsonElement En,
    JsonElement Pt);

public sealed record UpdateLocaleCatalogRequest(
    long ExpectedRevision,
    IReadOnlyDictionary<string, LocaleCatalogChangeRequest>? Changes);

public sealed record AdminLocaleCatalogResponse(
    long Revision,
    IReadOnlyList<AdminLocaleEntryResponse> Entries);

public sealed record AdminLocaleEntryResponse(
    string Key,
    string Type,
    object En,
    object Pt);

public sealed record PublicLocaleCatalogResponse(
    string Locale,
    long Revision,
    JsonObject Messages);
