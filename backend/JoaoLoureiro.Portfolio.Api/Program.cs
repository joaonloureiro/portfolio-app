using AutoMapper;
using FluentValidation;
using HealthChecks.UI.Client;
using JoaoLoureiro.Portfolio.Api.Dtos;
using JoaoLoureiro.Portfolio.Core.Interfaces;
using JoaoLoureiro.Portfolio.Core.Models;
using JoaoLoureiro.Portfolio.Infrastructure.HealthCheck;
using JoaoLoureiro.Portfolio.Infrastructure.Services;
using JoaoLoureiro.Portfolio.Infrastructure.Settings;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

if (builder.Configuration["BackendPort"] is { Length: > 0 } port && ushort.TryParse(port, out _))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

builder.Host.UseSerilog((context, config) => config.ReadFrom.Configuration(context.Configuration));

builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

builder.Services.AddOptions<SmtpSettings>()
    .Bind(builder.Configuration.GetSection("SmtpSettings"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddAutoMapper(cfg => { }, typeof(Program));
builder.Services.AddHealthChecks()
    .AddCheck<SmtpHealthCheck>("SMTP");

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.KnownProxies.Add(IPAddress.Parse(builder.Configuration["ProxyIP"] ?? "10.0.10.10"));
});

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseSerilogRequestLogging();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();


app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapPost("/api/email/send", async (
    [FromBody] ContactMessageRequest request,
    IValidator<ContactMessageRequest> validator,
    IEmailSender emailSender,
    IMapper mapper,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    var validationResult = await validator.ValidateAsync(request, cancellationToken);
    if (!validationResult.IsValid)
    {
        var error = validationResult.Errors.First();
        return Results.BadRequest(new { errorKey = error.ErrorCode });
    }

    try
    {
        var email = mapper.Map<EmailToSend>(request);

        await emailSender.SendEmailAsync(email, cancellationToken);

        return Results.Ok(new { message = "Message sent successfully!" });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An unexpected error occurred while sending email.");
        return Results.Problem(
            detail: "An unexpected error occurred on the server.",
            statusCode: 500,
            title: "Server Error",
            extensions: new Dictionary<string, object?> { { "errorKey", "server_unexpected_error" } }
        );
    }
})
.WithName("SendContactMessage")
.WithDescription("Accepts a contact form submission and sends it as an email.")
.Produces(StatusCodes.Status200OK)
.Produces<HttpValidationProblemDetails>(StatusCodes.Status400BadRequest)
.Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


app.Run();