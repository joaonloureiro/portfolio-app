using FluentValidation;
using Microsoft.AspNetCore.RateLimiting;
using JoaoLoureiro.Portfolio.Api.Authentication;
using JoaoLoureiro.Portfolio.Api.Configuration;
using JoaoLoureiro.Portfolio.Api.Dtos;
using JoaoLoureiro.Portfolio.Api.Endpoints;
using JoaoLoureiro.Portfolio.Api.Validators;
using JoaoLoureiro.Portfolio.Core.Interfaces;
using JoaoLoureiro.Portfolio.Core.Models;
using JoaoLoureiro.Portfolio.Core.EmailDelivery;
using JoaoLoureiro.Portfolio.Domain.Entities;
using JoaoLoureiro.Portfolio.Infrastructure.HealthCheck;
using JoaoLoureiro.Portfolio.Infrastructure.Persistence;
using JoaoLoureiro.Portfolio.Infrastructure.Services;
using JoaoLoureiro.Portfolio.Infrastructure.Settings;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Net;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

if (builder.Configuration["BACKEND_PORT"] is { Length: > 0 } port && ushort.TryParse(port, out _))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

builder.Host.UseSerilog((context, config) => config.ReadFrom.Configuration(context.Configuration));

builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste a valid JWT access token. Swagger UI adds the Bearer prefix."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var corsOrigins = builder.Configuration["CorsOrigins"];
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (corsOrigins is not null)
        {
            policy.WithOrigins(corsOrigins.Split(','))
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
    });
});

builder.Services.AddSingleton<IValidateOptions<SmtpTransportOptions>, SmtpTransportOptionsValidator>();
builder.Services.AddOptions<SmtpTransportOptions>()
    .Bind(builder.Configuration.GetSection(SmtpTransportOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<IValidateOptions<OidcJwtOptions>, OidcJwtOptionsValidator>();
builder.Services.AddOptions<OidcJwtOptions>()
    .Bind(builder.Configuration.GetSection(OidcJwtOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddOidcJwtAuthentication(builder.Environment);

builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddContentPersistence(builder.Configuration);
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        var errorKey = context.HttpContext.Request.Path.StartsWithSegments("/admin/api/email-delivery")
            ? "email_delivery_test_throttled"
            : "contact_throttled";
        context.HttpContext.Response.ContentType = "application/problem+json";
        await context.HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Request limit reached.",
            Extensions = new Dictionary<string, object?> { ["errorKey"] = errorKey }
        }, cancellationToken);
    };
    options.AddPolicy("contact", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        GetRateLimitPartitionKey(httpContext),
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
    options.AddPolicy("admin-email-test", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        $"admin:{httpContext.User.FindFirst("sub")?.Value ?? GetRateLimitPartitionKey(httpContext)}",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 3,
            Window = TimeSpan.FromMinutes(5),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});

builder.Services.AddHealthChecks()
    .AddCheck<SmtpHealthCheck>("SMTP");

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownProxies.Add(IPAddress.Parse(builder.Configuration["ProxyIP"] ?? "10.0.10.10"));
});

var app = builder.Build();

if (args.Contains("--migrate", StringComparer.Ordinal))
{
    using var migrationScope = app.Services.CreateScope();
    migrationScope.ServiceProvider.GetRequiredService<PortfolioDbContext>().Database.Migrate();
    return;
}

// Local development should be usable from a clean checkout. Production keeps
// the documented explicit migration step so schema changes remain an
// operator-controlled deployment action.
if (app.Environment.IsDevelopment())
{
    using var migrationScope = app.Services.CreateScope();
    migrationScope.ServiceProvider.GetRequiredService<PortfolioDbContext>().Database.Migrate();
}

app.UseForwardedHeaders();

app.UseSerilogRequestLogging();
app.UseExceptionHandler();
app.UseStatusCodePages(async statusCodeContext =>
{
    var httpContext = statusCodeContext.HttpContext;
    if (httpContext.Response.StatusCode == StatusCodes.Status413PayloadTooLarge &&
        (httpContext.Request.Path.StartsWithSegments("/api/email/send") ||
         httpContext.Request.Path.StartsWithSegments("/admin/api/posts") ||
         httpContext.Request.Path.StartsWithSegments("/admin/api/projects") ||
         httpContext.Request.Path.StartsWithSegments("/admin/api/locales")))
    {
        var isContactPayload = httpContext.Request.Path.StartsWithSegments("/api/email/send");
        httpContext.Response.StatusCode = isContactPayload
            ? StatusCodes.Status413PayloadTooLarge
            : StatusCodes.Status400BadRequest;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = httpContext.Response.StatusCode,
            Title = isContactPayload ? "Contact message is too large." : "Invalid content input.",
            Extensions = new Dictionary<string, object?>
            {
                ["errorKey"] = httpContext.Request.Path.StartsWithSegments("/api/email/send")
                    ? "contact_payload_too_large"
                    : httpContext.Request.Path.StartsWithSegments("/admin/api/projects")
                    ? ProjectErrorKeys.InvalidInput
                    : httpContext.Request.Path.StartsWithSegments("/admin/api/locales")
                        ? "locale_invalid_shape"
                        : PostErrorKeys.InvalidInput
            }
        }, httpContext.RequestAborted);
    }
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// TLS terminates at the trusted edge proxy. The frontend BFF reaches this
// container over the private Compose network, so redirecting that HTTP hop
// would prevent the BFF from reaching the API.
app.UseRouting();
app.UseCors();
app.Use(async (httpContext, next) =>
{
    if (httpContext.Request.Path.StartsWithSegments("/admin/api"))
    {
        httpContext.Response.Headers.CacheControl = "no-store";
        httpContext.Response.Headers.Pragma = "no-cache";
    }

    await next();
});
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();


app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = WriteSafeHealthResponse
}).AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    ResponseWriter = WriteSafeHealthResponse
}).AllowAnonymous();
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = WriteSafeHealthResponse
}).AllowAnonymous();

app.MapPost("/api/email/send", async (
    [FromBody] ContactMessageRequest? request,
    IValidator<ContactMessageRequest> validator,
    EmailDeliveryApplicationService emailDelivery,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    if (request is null)
    {
        return Results.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Invalid contact message.",
            extensions: new Dictionary<string, object?> { ["errorKey"] = "contact_invalid_input" });
    }

    var validationResult = await validator.ValidateAsync(request, cancellationToken);
    if (!validationResult.IsValid)
    {
        var error = validationResult.Errors.First();
        return Results.BadRequest(new { errorKey = error.ErrorCode });
    }

    try
    {
        var result = await emailDelivery.SendContactAsync(
            request.Name.Trim(),
            request.Email.Trim(),
            request.Message,
            cancellationToken);
        return result.Outcome switch
        {
            EmailDeliverySendOutcome.Succeeded => Results.Ok(new { message = "Message sent successfully!" }),
            EmailDeliverySendOutcome.Disabled => Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Contact delivery is unavailable.",
                extensions: new Dictionary<string, object?> { ["errorKey"] = "contact_delivery_disabled" }),
            EmailDeliverySendOutcome.TimedOut => Results.Problem(
                statusCode: StatusCodes.Status504GatewayTimeout,
                title: "Contact delivery timed out.",
                extensions: new Dictionary<string, object?> { ["errorKey"] = "contact_delivery_timeout" }),
            _ => Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Contact delivery is unavailable.",
                extensions: new Dictionary<string, object?> { ["errorKey"] = "contact_delivery_unavailable" })
        };
    }
    catch (EmailDeliveryPolicyStorageUnavailableException exception)
    {
        logger.LogError("Email delivery policy storage unavailable during {Operation}.", exception.Operation);
        return Results.Problem(
            statusCode: StatusCodes.Status503ServiceUnavailable,
            title: "Contact delivery is unavailable.",
            extensions: new Dictionary<string, object?> { ["errorKey"] = "contact_delivery_unavailable" });
    }
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    {
        throw;
    }
    catch (Exception)
    {
        logger.LogError("An unexpected email delivery error occurred.");
        return Results.Problem(
            statusCode: StatusCodes.Status503ServiceUnavailable,
            title: "Contact delivery is unavailable.",
            extensions: new Dictionary<string, object?> { ["errorKey"] = "contact_delivery_unavailable" }
        );
    }
})
.WithName("SendContactMessage")
.WithDescription("Accepts a contact form submission and sends it as an email.")
.WithMetadata(new RequestSizeLimitAttribute(ContactMessageRequestValidator.MaxRequestBytes))
.RequireRateLimiting("contact")
.Produces(StatusCodes.Status200OK)
.Produces<HttpValidationProblemDetails>(StatusCodes.Status400BadRequest)
.Produces<ProblemDetails>(StatusCodes.Status413PayloadTooLarge)
.Produces<ProblemDetails>(StatusCodes.Status429TooManyRequests)
.Produces<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)
.Produces<ProblemDetails>(StatusCodes.Status504GatewayTimeout)
.AllowAnonymous();

app.MapPublicPostEndpoints();
app.MapPublicProjectEndpoints();
app.MapPublicLocaleEndpoints();
app.MapAdminEndpoints();
app.Run();

static string GetRateLimitPartitionKey(HttpContext context)
{
    return context.Connection.RemoteIpAddress?.ToString() ?? "unknown-client";
}

static async Task WriteSafeHealthResponse(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";
    context.Response.StatusCode = report.Status == HealthStatus.Healthy
        ? StatusCodes.Status200OK
        : StatusCodes.Status503ServiceUnavailable;
    await context.Response.WriteAsJsonAsync(new
    {
        status = report.Status.ToString().ToLowerInvariant()
    }, context.RequestAborted);
}

public partial class Program { }
