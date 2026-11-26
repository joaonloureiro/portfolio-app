using System.Text.Json;
using JoaoLoureiro.Portfolio.Api.Dtos;
using JoaoLoureiro.Portfolio.Core.Locales;
using Microsoft.AspNetCore.Mvc;

namespace JoaoLoureiro.Portfolio.Api.Endpoints;

public static class LocaleEndpointMapping
{
    public static RouteGroupBuilder MapAdminLocaleEndpoints(this RouteGroupBuilder adminApi)
    {
        var localesApi = adminApi.MapGroup("/locales")
            .WithMetadata(new RequestSizeLimitAttribute(LocaleCatalogLimits.MaxRequestBytes));

        localesApi.MapGet("", GetAdminCatalog)
            .WithName("GetAdminLocaleCatalog")
            .WithDescription("Returns the editable bilingual public locale catalog.")
            .Produces<AdminLocaleCatalogResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status503ServiceUnavailable);

        localesApi.MapPut("", UpdateAdminCatalog)
            .WithName("UpdateAdminLocaleCatalog")
            .WithDescription("Publishes a conditional bilingual locale catalog update.")
            .Produces<AdminLocaleCatalogResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status503ServiceUnavailable);

        return adminApi;
    }

    public static IEndpointRouteBuilder MapPublicLocaleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/locales/{locale}", GetPublicCatalog)
            .WithName("GetPublicLocaleCatalog")
            .WithDescription("Returns the active public messages for a supported locale.")
            .Produces<PublicLocaleCatalogResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)
            .AllowAnonymous();

        return endpoints;
    }

    private static async Task<IResult> GetAdminCatalog(
        LocaleCatalogApplicationService localeService,
        ILogger<LocaleEndpointLogger> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await localeService.GetAdminAsync(cancellationToken);
            return Results.Ok(MapAdmin(result.Catalog!));
        }
        catch (LocaleCatalogStorageUnavailableException exception)
        {
            return StorageUnavailable(logger, exception, "read");
        }
    }

    private static async Task<IResult> UpdateAdminCatalog(
        [FromBody] UpdateLocaleCatalogRequest? request,
        LocaleCatalogApplicationService localeService,
        ILogger<LocaleEndpointLogger> logger,
        CancellationToken cancellationToken)
    {
        if (request is null || request.Changes is null)
        {
            return InvalidShape();
        }

        var changes = new Dictionary<string, LocaleCatalogChange>(StringComparer.Ordinal);
        foreach (var (key, value) in request.Changes)
        {
            if (!LocaleCatalogContract.IsValidKeyShape(key))
            {
                return InvalidShape();
            }

            if (!LocaleCatalogContract.TryGetDefinition(key, out var definition))
            {
                return UnknownKey();
            }

            if (value is null)
            {
                return InvalidShape();
            }

            var englishResult = ReadValue(value.En, definition!.Type);
            var portugueseResult = ReadValue(value.Pt, definition.Type);
            if (englishResult.Value is null || portugueseResult.Value is null)
            {
                return englishResult.ErrorKey == LocaleCatalogErrorKeys.InvalidValue ||
                    portugueseResult.ErrorKey == LocaleCatalogErrorKeys.InvalidValue
                    ? InvalidValue()
                    : InvalidShape();
            }

            changes[key] = new LocaleCatalogChange(englishResult.Value, portugueseResult.Value);
        }

        try
        {
            var result = await localeService.ReplaceAsync(
                new ReplaceLocaleCatalogCommand(request.ExpectedRevision, changes),
                cancellationToken);

            return result.Outcome switch
            {
                LocaleCatalogOperationOutcome.Succeeded => Results.Ok(MapAdmin(result.Catalog!)),
                LocaleCatalogOperationOutcome.UnknownKey => UnknownKey(),
                LocaleCatalogOperationOutcome.InvalidValue => InvalidValue(),
                LocaleCatalogOperationOutcome.StaleRevision => Conflict(),
                _ => InvalidShape()
            };
        }
        catch (LocaleCatalogStorageUnavailableException exception)
        {
            return StorageUnavailable(logger, exception, "update");
        }
    }

    private static async Task<IResult> GetPublicCatalog(
        string locale,
        HttpContext httpContext,
        LocaleCatalogApplicationService localeService,
        ILogger<LocaleEndpointLogger> logger,
        CancellationToken cancellationToken)
    {
        if (!LocaleCatalogLocaleExtensions.TryParse(locale, out var parsedLocale))
        {
            return Problem(LocaleCatalogErrorKeys.UnsupportedLocale, StatusCodes.Status404NotFound, "Locale not found.");
        }

        try
        {
            var result = await localeService.GetPublicAsync(locale, cancellationToken);
            var catalog = result.Catalog!;
            httpContext.Response.Headers.ETag = $"\"locale-{catalog.Revision}\"";
            httpContext.Response.Headers.CacheControl = "public, max-age=0, s-maxage=60, stale-while-revalidate=300";
            return Results.Ok(new PublicLocaleCatalogResponse(
                parsedLocale.ToCode(),
                catalog.Revision,
                LocaleCatalogSeedData.CreateMessages(catalog.Entries, parsedLocale)));
        }
        catch (LocaleCatalogStorageUnavailableException exception)
        {
            return StorageUnavailable(logger, exception, "read");
        }
    }

    private static AdminLocaleCatalogResponse MapAdmin(LocaleCatalogRevision catalog)
    {
        return new(
            catalog.Revision,
            catalog.Entries.Select(entry => new AdminLocaleEntryResponse(
                entry.Key,
                entry.Type == LocaleCatalogValueType.String ? "string" : "string-list",
                ToTransportValue(entry.English),
                ToTransportValue(entry.Portuguese))).ToArray());
    }

    private static object ToTransportValue(LocaleCatalogValue value)
    {
        return value.Type == LocaleCatalogValueType.String
            ? value.Scalar!
            : value.List!;
    }

    private static (LocaleCatalogValue? Value, string ErrorKey) ReadValue(
        JsonElement value,
        LocaleCatalogValueType expectedType)
    {
        if (value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return (null, LocaleCatalogErrorKeys.InvalidShape);
        }

        if (expectedType == LocaleCatalogValueType.String)
        {
            if (value.ValueKind != JsonValueKind.String)
            {
                return (null, LocaleCatalogErrorKeys.InvalidShape);
            }

            if (value.GetString() is { Length: > LocaleCatalogLimits.MaxScalarLength })
            {
                return (null, LocaleCatalogErrorKeys.InvalidValue);
            }

            try
            {
                return (LocaleCatalogValue.FromString(value.GetString()!), string.Empty);
            }
            catch (LocaleCatalogDomainException)
            {
                return (null, LocaleCatalogErrorKeys.InvalidValue);
            }
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            return (null, LocaleCatalogErrorKeys.InvalidShape);
        }

        var items = value.EnumerateArray().ToArray();
        if (items.Any(item => item.ValueKind != JsonValueKind.String))
        {
            return (null, LocaleCatalogErrorKeys.InvalidShape);
        }

        var strings = items.Select(item => item.GetString()!).ToArray();
        if (strings.Length is < 1 or > LocaleCatalogLimits.MaxListItemCount ||
            strings.Any(item => item.Length > LocaleCatalogLimits.MaxListItemLength))
        {
            return (null, LocaleCatalogErrorKeys.InvalidValue);
        }

        try
        {
            return (LocaleCatalogValue.FromList(strings), string.Empty);
        }
        catch (LocaleCatalogDomainException)
        {
            return (null, LocaleCatalogErrorKeys.InvalidValue);
        }
    }

    private static IResult StorageUnavailable(
        ILogger<LocaleEndpointLogger> logger,
        LocaleCatalogStorageUnavailableException exception,
        string operation)
    {
        logger.LogError(exception, "Locale catalog storage operation failed: {Operation}.", operation);
        return Problem(LocaleCatalogErrorKeys.StorageUnavailable, StatusCodes.Status503ServiceUnavailable, "Locale catalog unavailable.");
    }

    private static IResult InvalidShape() =>
        Problem(LocaleCatalogErrorKeys.InvalidShape, StatusCodes.Status400BadRequest, "Invalid locale catalog shape.");

    private static IResult UnknownKey() =>
        Problem(LocaleCatalogErrorKeys.UnknownKey, StatusCodes.Status400BadRequest, "Locale key is not editable.");

    private static IResult InvalidValue() =>
        Problem(LocaleCatalogErrorKeys.InvalidValue, StatusCodes.Status400BadRequest, "Invalid locale catalog value.");

    private static IResult Conflict() =>
        Problem(LocaleCatalogErrorKeys.StaleRevision, StatusCodes.Status409Conflict, "Locale catalog revision conflict.");

    private static IResult Problem(string errorKey, int statusCode, string title) => Results.Problem(
        statusCode: statusCode,
        title: title,
        extensions: new Dictionary<string, object?> { ["errorKey"] = errorKey });

    private sealed class LocaleEndpointLogger;
}
