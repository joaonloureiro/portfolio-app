using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using JoaoLoureiro.Portfolio.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JoaoLoureiro.Portfolio.Api.Tests;

public sealed class EmailDeliveryEndpointTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient client = factory.CreateClient();

    [Fact]
    public async Task SettingsRequireThePortfolioAdministratorPolicy()
    {
        var response = await client.GetAsync("/admin/api/email-delivery");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AdministratorCanReadAndUpdateOnlyTheSafePolicyFields()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/admin/api/email-delivery");
        request.Headers.Authorization = new("Bearer", factory.CreateToken(TestWebApplicationFactory.AdministratorSubject));
        var currentResponse = await client.SendAsync(request);
        currentResponse.EnsureSuccessStatusCode();
        var current = await currentResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrWhiteSpace(current.GetProperty("recipientAddress").GetString()));
        Assert.True(current.GetProperty("transportConfigured").GetBoolean());
        Assert.False(current.TryGetProperty("password", out _));
        Assert.False(current.TryGetProperty("host", out _));

        using var update = new HttpRequestMessage(HttpMethod.Put, "/admin/api/email-delivery")
        {
            Content = JsonContent.Create(new
            {
                enabled = true,
                recipientAddress = "new-recipient@example.test",
                senderDisplayName = "Portfolio contact",
                subjectPrefix = "New Portfolio Contact",
                expectedRevision = current.GetProperty("revision").GetInt64()
            })
        };
        update.Headers.Authorization = new("Bearer", factory.CreateToken(TestWebApplicationFactory.AdministratorSubject));
        var updateResponse = await client.SendAsync(update);
        updateResponse.EnsureSuccessStatusCode();
        var updateBody = await updateResponse.Content.ReadAsStringAsync();
        Assert.Contains("new-recipient@example.test", updateBody, StringComparison.Ordinal);
        Assert.DoesNotContain("test-password", updateBody, StringComparison.Ordinal);
        Assert.DoesNotContain("smtp.test", updateBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StalePolicyRevisionIsRejected()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/admin/api/email-delivery");
        request.Headers.Authorization = new("Bearer", factory.CreateToken(TestWebApplicationFactory.AdministratorSubject));
        var current = await (await client.SendAsync(request)).Content.ReadFromJsonAsync<JsonElement>();

        using var firstUpdate = new HttpRequestMessage(HttpMethod.Put, "/admin/api/email-delivery")
        {
            Content = JsonContent.Create(new
            {
                enabled = true,
                recipientAddress = "first@example.test",
                senderDisplayName = "Portfolio contact",
                subjectPrefix = "New Portfolio Contact",
                expectedRevision = current.GetProperty("revision").GetInt64()
            })
        };
        firstUpdate.Headers.Authorization = new("Bearer", factory.CreateToken(TestWebApplicationFactory.AdministratorSubject));
        (await client.SendAsync(firstUpdate)).EnsureSuccessStatusCode();

        using var update = new HttpRequestMessage(HttpMethod.Put, "/admin/api/email-delivery")
        {
            Content = JsonContent.Create(new
            {
                enabled = true,
                recipientAddress = "stale@example.test",
                senderDisplayName = "Portfolio contact",
                subjectPrefix = "New Portfolio Contact",
                expectedRevision = current.GetProperty("revision").GetInt64()
            })
        };
        update.Headers.Authorization = new("Bearer", factory.CreateToken(TestWebApplicationFactory.AdministratorSubject));
        var response = await client.SendAsync(update);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("email_delivery_stale_revision", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errorKey").GetString());
    }

    [Fact]
    public async Task PublicContactUsesThePersistedPolicyAndDoesNotSendWhenDisabled()
    {
        using var adminGet = new HttpRequestMessage(HttpMethod.Get, "/admin/api/email-delivery");
        adminGet.Headers.Authorization = new("Bearer", factory.CreateToken(TestWebApplicationFactory.AdministratorSubject));
        var current = await (await client.SendAsync(adminGet)).Content.ReadFromJsonAsync<JsonElement>();
        var revision = current.GetProperty("revision").GetInt64();

        using var disable = new HttpRequestMessage(HttpMethod.Put, "/admin/api/email-delivery")
        {
            Content = JsonContent.Create(new
            {
                enabled = false,
                recipientAddress = current.GetProperty("recipientAddress").GetString(),
                senderDisplayName = current.GetProperty("senderDisplayName").GetString(),
                subjectPrefix = current.GetProperty("subjectPrefix").GetString(),
                expectedRevision = revision
            })
        };
        disable.Headers.Authorization = new("Bearer", factory.CreateToken(TestWebApplicationFactory.AdministratorSubject));
        (await client.SendAsync(disable)).EnsureSuccessStatusCode();

        var rejected = await client.PostAsJsonAsync("/api/email/send", new
        {
            name = "Visitor",
            email = "visitor@example.test",
            message = "Hello"
        });
        Assert.Equal(HttpStatusCode.ServiceUnavailable, rejected.StatusCode);
        Assert.Equal("contact_delivery_disabled", (await rejected.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errorKey").GetString());

        using var enableGet = new HttpRequestMessage(HttpMethod.Get, "/admin/api/email-delivery");
        enableGet.Headers.Authorization = new("Bearer", factory.CreateToken(TestWebApplicationFactory.AdministratorSubject));
        var enabledCurrent = await (await client.SendAsync(enableGet)).Content.ReadFromJsonAsync<JsonElement>();
        using var enable = new HttpRequestMessage(HttpMethod.Put, "/admin/api/email-delivery")
        {
            Content = JsonContent.Create(new
            {
                enabled = true,
                recipientAddress = "delivery@example.test",
                senderDisplayName = "Configured sender",
                subjectPrefix = "New Portfolio Contact",
                expectedRevision = enabledCurrent.GetProperty("revision").GetInt64()
            })
        };
        enable.Headers.Authorization = new("Bearer", factory.CreateToken(TestWebApplicationFactory.AdministratorSubject));
        (await client.SendAsync(enable)).EnsureSuccessStatusCode();

        var accepted = await client.PostAsJsonAsync("/api/email/send", new
        {
            name = "Visitor",
            email = "visitor@example.test",
            message = "Hello"
        });
        accepted.EnsureSuccessStatusCode();
        var sender = factory.Services.GetRequiredService<TestWebApplicationFactory.TestEmailSender>();
        var sent = Assert.Single(sender.SentEmails);
        Assert.Equal("delivery@example.test", sent.RecipientAddress);
        Assert.Equal("New Portfolio Contact: Visitor", sent.Subject);
        Assert.Equal("Configured sender", sent.SenderDisplayName);

        using var testRequest = new HttpRequestMessage(HttpMethod.Post, "/admin/api/email-delivery/test");
        testRequest.Headers.Authorization = new("Bearer", factory.CreateToken(TestWebApplicationFactory.AdministratorSubject));
        var testResponse = await client.SendAsync(testRequest);
        testResponse.EnsureSuccessStatusCode();
        var testEmail = sender.SentEmails[^1];
        Assert.Equal("delivery@example.test", testEmail.RecipientAddress);
        Assert.Equal("Portfolio email delivery test", testEmail.Subject);
        Assert.Equal("This is a bounded test message from the portfolio Admin workspace.", testEmail.MessageBody);
    }

    [Fact]
    public async Task InvalidContactInputDoesNotReachTheSenderAndReadinessIsRedacted()
    {
        var sender = factory.Services.GetRequiredService<TestWebApplicationFactory.TestEmailSender>();
        var before = sender.SentEmails.Count;
        var invalid = await client.PostAsJsonAsync("/api/email/send", new
        {
            name = new string('n', 121),
            email = "visitor@example.test",
            message = "Hello"
        });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal(before, sender.SentEmails.Count);

        var health = await client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, health.StatusCode);
        var healthBody = await health.Content.ReadAsStringAsync();
        Assert.DoesNotContain("smtp.test", healthBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("test-password", healthBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnauthenticatedTestDeliveryRequestsDoNotConsumeAdminRateLimit()
    {
        using var isolatedFactory = new TestWebApplicationFactory();
        using var isolatedClient = isolatedFactory.CreateClient();

        for (var attempt = 0; attempt < 4; attempt++)
        {
            var unauthenticated = await isolatedClient.PostAsync("/admin/api/email-delivery/test", content: null);
            Assert.Equal(HttpStatusCode.Unauthorized, unauthenticated.StatusCode);
        }

        using var authenticatedRequest = new HttpRequestMessage(HttpMethod.Post, "/admin/api/email-delivery/test");
        authenticatedRequest.Headers.Authorization = new("Bearer", isolatedFactory.CreateToken(TestWebApplicationFactory.AdministratorSubject));
        var authenticatedResponse = await isolatedClient.SendAsync(authenticatedRequest);

        Assert.Equal(HttpStatusCode.OK, authenticatedResponse.StatusCode);
    }
}
