using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using JoaoLoureiro.Portfolio.Api.Authentication;
using JoaoLoureiro.Portfolio.Api.Dtos;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace JoaoLoureiro.Portfolio.Api.Tests;

public sealed class AdminAuthenticationTests(TestWebApplicationFactory factory)
    : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false
    });

    [Fact]
    public async Task MissingJwt_ReturnsUnauthorized()
    {
        using var response = await client.GetAsync("/admin/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthorizationHeader_AuthenticatesAdministrator()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/admin/api/me");
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            factory.CreateToken(TestWebApplicationFactory.AdministratorSubject));

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task MalformedJwt_ReturnsUnauthorized()
    {
        using var response = await SendWithToken("not-a-jwt");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task InvalidSignature_ReturnsUnauthorized()
    {
        using var response = await SendWithToken(factory.CreateTokenWithInvalidSignature("any-subject"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task WrongIssuer_ReturnsUnauthorized()
    {
        using var response = await SendWithToken(
            factory.CreateToken("any-subject", issuer: "https://wrong-issuer.test/"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task WrongAudience_ReturnsUnauthorized()
    {
        using var response = await SendWithToken(
            factory.CreateToken("any-subject", audience: "wrong-audience"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ExpiredToken_ReturnsUnauthorized()
    {
        using var response = await SendWithToken(
            factory.CreateToken("any-subject", expires: DateTime.UtcNow.AddMinutes(-5)));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DisallowedAlgorithm_ReturnsUnauthorized()
    {
        using var response = await SendWithToken(factory.CreateTokenWithInvalidAlgorithm("any-subject"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task EmptyJwtHeader_ReturnsUnauthorized()
    {
        using var response = await SendWithHeader(string.Empty);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RepeatedAuthorizationHeader_ReturnsUnauthorized()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/admin/api/me");
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {factory.CreateToken(TestWebApplicationFactory.AdministratorSubject)}");
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {factory.CreateToken(TestWebApplicationFactory.AdministratorSubject)}");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task OversizedJwtHeader_ReturnsUnauthorized()
    {
        using var response = await SendWithHeader(new string('a', 8193));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task LegacyProxyHeader_DoesNotAuthenticate()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/admin/api/me");
        request.Headers.TryAddWithoutValidation("X-authentik-jwt", factory.CreateToken(TestWebApplicationFactory.AdministratorSubject));

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ValidNonAdministratorToken_ReturnsForbidden()
    {
        using var response = await SendWithToken(factory.CreateToken("different-subject"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ValidAdministratorToken_ReturnsValidatedIdentity()
    {
        using var response = await SendWithToken(factory.CreateToken(TestWebApplicationFactory.AdministratorSubject));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var identity = await response.Content.ReadFromJsonAsync<AdminIdentityResponse>();
        Assert.Equal(TestWebApplicationFactory.AdministratorSubject, identity?.Subject);
    }

    [Fact]
    public async Task EmailEndpoint_IsAnonymous()
    {
        using var response = await client.PostAsJsonAsync(
            "/api/email/send",
            new
            {
                Name = "Test sender",
                Email = "sender@example.test",
                Message = "Test message"
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task MappedAdminEndpoint_InheritsRouteGroupPolicy()
    {
        using var response = await client.GetAsync("/admin/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private Task<HttpResponseMessage> SendWithToken(string token)
    {
        return SendWithHeader(token);
    }

    private async Task<HttpResponseMessage> SendWithHeader(string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/admin/api/me");
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {token}");
        return await client.SendAsync(request);
    }
}
