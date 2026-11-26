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

    private const string PrivateKeyPem = """
-----BEGIN PRIVATE KEY-----
MIIEvgIBADANBgkqhkiG9w0BAQEFAASCBKgwggSkAgEAAoIBAQCzx4JDwnFRyIHC
uMQiCdOBbuGTSqnsVVT+RJr2rpn9PkZUxFl11STsLJ4zAPIo0Ajm7OSnjOEuFKQA
QQKzodqZQYjhPdvoPuqNd37EdqY+twNx/inz1RLSJU7UL7qsSc62eJGDNwc7tD91
nwT7u64rNLQes3IezTnbE/o+17fEqGrDJVdo3WlOGMb+2h07pR6FjoeKI8Tiq77m
OVp3Yiqw5spQenoyOh+RHNqIVRXsyOMdsmfwwpy6X8/aJvdsywlH6FOIj+SkE7n6
3QXEPSEUEBl/zmY9/bKE2hVWjugYh6QqFWL4INKK18STcS78ydCR57KSbh2D8Oxn
jgU+BxofAgMBAAECggEALsfcDEDFFwhrR52nn8nZySTuutKja9V6k92LS2R8uoqh
Z35eXwyTGoeztMbqvJcBsgFzsGu2zrRdRpmkF9UmmlmUwjPEDOeORjiRepL7SHd8
BIYkKYhI6Pj/TbSLyTg9e3rdNn0zuyP4nWU/OTgJpsGEmzaLnBtAElNc+0xLy/at
ULC+I95sGQw/pz0UKLJcx0axIybqm8SqWH2xLhEa6vSH9dKrgQTLI6BTP2bz9n6w
tKy6MZfpNUhNzXBtOxt7iS2dEa2gCvOUlqjY4KZ0VYpTxr5ZSpQrWCjqvtW227JL
J3vCe5k3aNM1ICbDhyjFsGDu5GPanN46+RHFlEQDBQKBgQDkND++0CDS3qdaKhjq
Rq5vQEsS2BIG7t5662Yx4fZucRHGFtrqsSpFG44LjttUvRNMH/xuWrqPudcaSL+f
xzMkfB3JYN9p2bfoBUUZEn+xU7o2jJbD1S87zA9Tt0DKzPSlFgqTr4ZsytdT6b0V
IVNODN0K7nLbCrkAWAVGiIHTawKBgQDJrU1Mct54IoiVwLnK2PG1bFi6gmBvsbd7
ZnfgzED9d1xW9MNhe7p1JbJfcUuHS4QX9w/MeLCS4zGWmG7kAS7sUH+yGp5uso5T
de2tfPJgBtmX5/bZ9fnbG1qopZDJFOJ+EnW1KmOqRQ4b30JNksW4ga9IZQybAivk
iQ6Go+c1HQKBgAIPtsOyfZlqCXj3CGKVYN4AZq005nTJk5jop3Or5t34h4CSlEeq
cgdNyGqyT6jkTFhTsaQNjcHem+LQ5Wo8Emox9NLs3RfHr7GY2pbvY9fmObhkbLg4
JMUVgs3uydcLIuqa2F2dKEmzS16mrd6S/n2mABkpLkBsSF+IbAPHF+UhAoGBAJeu
Gbbnpd5gyq0mmBWx+Ihkho8vw2v+E2th88NEm5SGkxJrYA8PSXsV8QPLZ+zHpP+J
N36nPtYavY7v8mbVZP8ixAel8YZ8EINA7JNQp9IFFN4k/gmL8HEUL6krbKtvY7FC
HPk2emLzMNtRnMJPIxnTn4spRMPIbE6jriMhXqEZAoGBALXZ+uiGpyszAWW97b9f
Ul6kMq9/+ibeEAa0hQrmCIZcJDQnyWnlcMzhIamdG+Yl4xiPVztcNB7sVi0SOle3
OoMuuAkssVGcBQ8hgOMfN/LoGSyb6UhV7CY50oUtKAqj0Z1wn6M2yNB5EAzL/KeJ
rzimz4jovG1Q0c9EoxvTtV3c
-----END PRIVATE KEY-----
""";

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

    private static RSA CreateSigningRsa()
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(PrivateKeyPem);
        return rsa;
    }

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
