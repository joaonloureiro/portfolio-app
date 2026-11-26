using FluentValidation;
using JoaoLoureiro.Portfolio.Api.Authentication;
using JoaoLoureiro.Portfolio.Api.Dtos;
using JoaoLoureiro.Portfolio.Core.EmailDelivery;
using JoaoLoureiro.Portfolio.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace JoaoLoureiro.Portfolio.Api.Endpoints;

public static class AdminEmailDeliveryEndpointMapping
{
    public static void MapAdminEmailDeliveryEndpoints(this RouteGroupBuilder adminApi)
    {
        var emailDeliveryApi = adminApi.MapGroup("/email-delivery");

        emailDeliveryApi.MapGet("", GetPolicy)
            .WithName("GetAdminEmailDeliveryPolicy")
            .Produces<EmailDeliveryPolicyResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status503ServiceUnavailable);

        emailDeliveryApi.MapPut("", UpdatePolicy)
            .WithMetadata(new RequestSizeLimitAttribute(16 * 1024))
            .WithName("UpdateAdminEmailDeliveryPolicy")
            .Produces<EmailDeliveryPolicyResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status503ServiceUnavailable);

        emailDeliveryApi.MapPost("/test", SendTest)
            .WithName("SendAdminEmailDeliveryTest")
            .RequireRateLimiting("admin-email-test")
            .Produces<EmailDeliveryTestResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status429TooManyRequests)
            .Produces<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)
            .Produces<ProblemDetails>(StatusCodes.Status504GatewayTimeout);
    }

    private static async Task<IResult> GetPolicy(
        EmailDeliveryApplicationService service,
        IEmailTransportReadiness readiness,
        ILogger<AdminEmailDeliveryLogger> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var policy = await service.GetPolicyAsync(cancellationToken);
            return Results.Ok(ToResponse(policy, readiness));
        }
        catch (EmailDeliveryPolicyStorageUnavailableException exception)
        {
            logger.LogError("Email delivery policy storage unavailable during {Operation}.", exception.Operation);
            return Problem("email_delivery_storage_unavailable", StatusCodes.Status503ServiceUnavailable, "Email delivery settings are unavailable.");
        }
    }

    private static async Task<IResult> UpdatePolicy(
        UpdateEmailDeliveryPolicyRequest? request,
        IValidator<UpdateEmailDeliveryPolicyRequest> validator,
        EmailDeliveryApplicationService service,
        IEmailTransportReadiness readiness,
        ILogger<AdminEmailDeliveryLogger> logger,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Problem(EmailDeliveryErrorKeys.InvalidPolicy, StatusCodes.Status400BadRequest, "Invalid email delivery policy.");
        }

        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Problem(EmailDeliveryErrorKeys.InvalidPolicy, StatusCodes.Status400BadRequest, "Invalid email delivery policy.");
        }

        try
        {
            var result = await service.UpdatePolicyAsync(
                request.ExpectedRevision,
                new EmailDeliveryPolicyInput(
                    request.Enabled,
                    request.RecipientAddress,
                    request.SenderDisplayName,
                    request.SubjectPrefix),
                cancellationToken);

            return result.Outcome switch
            {
                EmailDeliveryPolicyUpdateOutcome.Succeeded => Updated(result.Policy!, readiness, logger),
                EmailDeliveryPolicyUpdateOutcome.StaleRevision => Problem(EmailDeliveryErrorKeys.StaleRevision, StatusCodes.Status409Conflict, "Email delivery settings changed elsewhere."),
                _ => Problem(EmailDeliveryErrorKeys.InvalidPolicy, StatusCodes.Status400BadRequest, "Invalid email delivery policy.")
            };
        }
        catch (EmailDeliveryPolicyStorageUnavailableException exception)
        {
            logger.LogError("Email delivery policy storage unavailable during {Operation}.", exception.Operation);
            return Problem("email_delivery_storage_unavailable", StatusCodes.Status503ServiceUnavailable, "Email delivery settings are unavailable.");
        }
    }

    private static async Task<IResult> SendTest(
        EmailDeliveryApplicationService service,
        ILogger<AdminEmailDeliveryLogger> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await service.SendTestAsync(cancellationToken);
            logger.LogInformation("Email delivery test requested with outcome {Outcome}.", result.Outcome);
            return result.Outcome switch
            {
                EmailDeliverySendOutcome.Succeeded => Results.Ok(new EmailDeliveryTestResponse("Test delivery sent to the configured recipient.")),
                EmailDeliverySendOutcome.Disabled => Problem("email_delivery_disabled", StatusCodes.Status400BadRequest, "Email delivery is disabled."),
                EmailDeliverySendOutcome.TimedOut => Problem("email_delivery_timeout", StatusCodes.Status504GatewayTimeout, "Email delivery timed out."),
                _ => Problem("email_delivery_unavailable", StatusCodes.Status503ServiceUnavailable, "Email delivery is unavailable.")
            };
        }
        catch (EmailDeliveryPolicyStorageUnavailableException exception)
        {
            logger.LogError("Email delivery policy storage unavailable during {Operation}.", exception.Operation);
            return Problem("email_delivery_storage_unavailable", StatusCodes.Status503ServiceUnavailable, "Email delivery settings are unavailable.");
        }
    }

    private static IResult Updated(
        EmailDeliveryPolicy policy,
        IEmailTransportReadiness readiness,
        ILogger logger)
    {
        logger.LogInformation("Email delivery policy updated to revision {Revision}.", policy.Revision);
        return Results.Ok(ToResponse(policy, readiness));
    }

    private static EmailDeliveryPolicyResponse ToResponse(
        EmailDeliveryPolicy policy,
        IEmailTransportReadiness readiness)
    {
        return new(
            policy.Enabled,
            policy.RecipientAddress,
            policy.SenderDisplayName,
            policy.SubjectPrefix,
            policy.Revision,
            readiness.IsConfigured);
    }

    private static IResult Problem(string errorKey, int statusCode, string title)
    {
        return Results.Problem(
            statusCode: statusCode,
            title: title,
            extensions: new Dictionary<string, object?> { ["errorKey"] = errorKey });
    }

    private sealed class AdminEmailDeliveryLogger;
}
