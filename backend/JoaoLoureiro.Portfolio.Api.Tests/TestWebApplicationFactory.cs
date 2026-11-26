using System.Security.Cryptography;
using System.Text;
using JoaoLoureiro.Portfolio.Api.Authentication;
using JoaoLoureiro.Portfolio.Core.Interfaces;
using JoaoLoureiro.Portfolio.Core.Models;
using JoaoLoureiro.Portfolio.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace JoaoLoureiro.Portfolio.Api.Tests;

public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string Issuer = "https://authentik.test/application/o/";
    public const string Audience = "portfolio-admin-tests";
    public const string AdministratorSubject = "administrator-test-subject";

    private static readonly RSA SigningRsa = CreateSigningRsa();
    private readonly SqliteConnection contentStoreConnection = new("Data Source=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Oidc:Issuer"] = Issuer,
                ["Authentication:Oidc:Audience"] = Audience,
                ["Authentication:Oidc:DiscoveryAddress"] = "https://authentik.test/.well-known/openid-configuration",
                ["Authentication:Oidc:AdminClaimType"] = "groups",
                ["Authentication:Oidc:AdminGroup"] = "portfolio-admin",
                ["SmtpTransport:Host"] = "smtp.test",
                ["SmtpTransport:Port"] = "25",
                ["SmtpTransport:SecurityMode"] = "StartTls",
                ["SmtpTransport:AuthenticationRequired"] = "true",
                ["SmtpTransport:Username"] = "test-user",
                ["SmtpTransport:Password"] = "test-password",
                ["SmtpTransport:FromAddress"] = "no-reply@example.test",
                ["SmtpTransport:ConnectTimeoutSeconds"] = "1",
                ["SmtpTransport:SendTimeoutSeconds"] = "1",
                ["SmtpTransport:DisconnectTimeoutSeconds"] = "1",
                ["EmailDelivery:InitialRecipientAddress"] = "recipient@example.test"
            });
        });
        builder.ConfigureTestServices(services =>
        {
            contentStoreConnection.Open();
            services.RemoveAll<DbContextOptions<PortfolioDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<PortfolioDbContext>();
            services.AddSingleton(contentStoreConnection);
            services.AddDbContext<PortfolioDbContext>((_, options) =>
                options.UseSqlite(
                    contentStoreConnection,
                    sqlite => sqlite.MigrationsAssembly(typeof(PortfolioDbContext).Assembly.FullName)));

            services.RemoveAll<IEmailSender>();
            services.AddSingleton<TestEmailSender>();
            services.AddSingleton<IEmailSender>(serviceProvider => serviceProvider.GetRequiredService<TestEmailSender>());
            services.PostConfigure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme,
                options =>
                {
                    var configuration = new OpenIdConnectConfiguration
                    {
                        Issuer = Issuer
                    };
                    configuration.SigningKeys.Add(new RsaSecurityKey(SigningRsa));
                    options.Configuration = configuration;
                    options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration);
                });
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        scope.ServiceProvider
            .GetRequiredService<PortfolioDbContext>()
            .Database
            .Migrate();
        return host;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            contentStoreConnection.Dispose();
        }

        base.Dispose(disposing);
    }

    public string CreateToken(
        string subject,
        string? issuer = null,
        string? audience = null,
        DateTime? expires = null)
    {
        var now = DateTime.UtcNow;
        var tokenExpires = expires ?? now.AddMinutes(5);
        var tokenNotBefore = tokenExpires <= now ? tokenExpires.AddMinutes(-1) : now.AddMinutes(-1);
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new System.Security.Claims.ClaimsIdentity(
                CreateClaims(subject)),
            Issuer = issuer ?? Issuer,
            Audience = audience ?? Audience,
            NotBefore = tokenNotBefore,
            IssuedAt = tokenNotBefore,
            Expires = tokenExpires,
            SigningCredentials = new SigningCredentials(
                new RsaSecurityKey(SigningRsa),
                SecurityAlgorithms.RsaSha256)
        };

        return new JwtSecurityTokenHandler().CreateEncodedJwt(descriptor);
    }

    public string CreateTokenWithInvalidSignature(string subject)
    {
        using var otherRsa = RSA.Create(2048);
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new System.Security.Claims.ClaimsIdentity(
                CreateClaims(subject)),
            Issuer = Issuer,
            Audience = Audience,
            NotBefore = DateTime.UtcNow.AddMinutes(-1),
            Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new SigningCredentials(
                new RsaSecurityKey(otherRsa),
                SecurityAlgorithms.RsaSha256)
        };

        return new JwtSecurityTokenHandler().CreateEncodedJwt(descriptor);
    }

    public string CreateTokenWithInvalidAlgorithm(string subject)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new System.Security.Claims.ClaimsIdentity(
                CreateClaims(subject)),
            Issuer = Issuer,
            Audience = Audience,
            NotBefore = DateTime.UtcNow.AddMinutes(-1),
            Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes("test-only-hmac-key-that-is-long-enough")),
                SecurityAlgorithms.HmacSha256)
        };

        return new JwtSecurityTokenHandler().CreateEncodedJwt(descriptor);
    }

    private static RSA CreateSigningRsa() => RSA.Create(2048);

    private static IEnumerable<System.Security.Claims.Claim> CreateClaims(string subject)
    {
        yield return new System.Security.Claims.Claim(OidcJwtAuthentication.SubjectClaimType, subject);
        if (subject == AdministratorSubject)
        {
            yield return new System.Security.Claims.Claim("groups", "portfolio-admin");
        }
    }

    public sealed class TestEmailSender : IEmailSender
    {
        public List<EmailToSend> SentEmails { get; } = [];

        public Task SendEmailAsync(EmailToSend email, CancellationToken cancellationToken = default)
        {
            SentEmails.Add(email);
            return Task.CompletedTask;
        }
    }
}
