using JoaoLoureiro.Portfolio.Api.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace JoaoLoureiro.Portfolio.Api.Authentication;

public static class OidcJwtAuthentication
{
    public const string PortfolioAdminPolicyName = "PortfolioAdmin";
    public const string SubjectClaimType = "sub";

    private static readonly TimeSpan AllowedClockSkew = TimeSpan.FromSeconds(30);

    public static IServiceCollection AddOidcJwtAuthentication(
        this IServiceCollection services,
        IHostEnvironment environment)
    {
        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<OidcJwtOptions>>((jwtOptions, configuredOptions) =>
            {
                var settings = configuredOptions.Value;
                jwtOptions.RequireHttpsMetadata = !environment.IsEnvironment("Testing");
                jwtOptions.MapInboundClaims = false;
                jwtOptions.IncludeErrorDetails = false;
                jwtOptions.MetadataAddress = settings.DiscoveryAddress;
                jwtOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    RequireSignedTokens = true,
                    RequireExpirationTime = true,
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true,
                    ValidIssuer = settings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = settings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = AllowedClockSkew,
                    ValidAlgorithms = new[] {SecurityAlgorithms.RsaSha256}
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(PortfolioAdminPolicyName, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.AddRequirements(new PortfolioAdminRequirement());
            });
        });
        services.AddSingleton<IAuthorizationHandler, PortfolioAdminHandler>();
        return services;
    }
}

public sealed class PortfolioAdminRequirement : IAuthorizationRequirement;

internal sealed class PortfolioAdminHandler(IOptions<OidcJwtOptions> options)
    : AuthorizationHandler<PortfolioAdminRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PortfolioAdminRequirement requirement)
    {
        var settings = options.Value;
        if (context.User.FindAll(settings.AdminClaimType)
            .Any(claim => string.Equals(claim.Value, settings.AdminGroup, StringComparison.Ordinal)))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
