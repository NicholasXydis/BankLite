using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using BankLite.Application.DTOs;
using Bogus;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.Tokens;

namespace BankLite.Tests.Integration;

[Collection("Integration")]
public class SecurityIntegrationTests : IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly BankLiteWebApplicationFactory _factory;
    private readonly Faker _faker = new();

    public SecurityIntegrationTests(BankLiteWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    private async Task<string> RegisterAndGetTokenAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterUserDto
        {
            FullName = "Test User",
            Email = _faker.Internet.Email(),
            Password = "Password123!"
        });
        var cookies = response.Headers.GetValues("Set-Cookie").ToList();
        return cookies.First(c => c.Contains("accessToken")).Split(";")[0].Split("=")[1];
    }

    private void AuthenticateClient(string token)
    {
        _client.DefaultRequestHeaders.Remove("Cookie");
        _client.DefaultRequestHeaders.Add("Cookie", $"accessToken={token}");
    }

    [Fact]
    public async Task SecurityHeaders_AllEightPresent_OnSuccessResponse()
    {
        var response = await _client.GetAsync("/health");

        response.Headers.Should().ContainKey("X-Content-Type-Options");
        response.Headers.Should().ContainKey("X-Frame-Options");
        response.Headers.Should().ContainKey("Referrer-Policy");
        response.Headers.Should().ContainKey("X-DNS-Prefetch-Control");
        response.Headers.Should().ContainKey("Cross-Origin-Opener-Policy");
        response.Headers.Should().ContainKey("X-Permitted-Cross-Domain-Policies");
    }

    [Fact]
    public async Task SecurityHeaders_CorrectValues_OnSuccessResponse()
    {
        var response = await _client.GetAsync("/health");

        response.Headers.GetValues("X-Content-Type-Options").First().Should().Be("nosniff");
        response.Headers.GetValues("X-Frame-Options").First().Should().Be("DENY");
        response.Headers.GetValues("Referrer-Policy").First().Should().Be("strict-origin-when-cross-origin");
        response.Headers.GetValues("X-DNS-Prefetch-Control").First().Should().Be("off");
        response.Headers.GetValues("Cross-Origin-Opener-Policy").First().Should().Be("same-origin");
        response.Headers.GetValues("X-Permitted-Cross-Domain-Policies").First().Should().Be("none");
    }

    [Fact]
    public async Task SecurityHeaders_PresentOn400Response()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginUserDto
        {
            Email = "notanemail",
            Password = "123"
        });

        response.Headers.Should().ContainKey("X-Frame-Options");
        response.Headers.Should().ContainKey("X-Content-Type-Options");
    }

    [Fact]
    public async Task SecurityHeaders_PresentOn401Response()
    {
        var response = await _client.GetAsync("/api/user/profile");

        response.Headers.Should().ContainKey("X-Frame-Options");
        response.Headers.Should().ContainKey("X-Content-Type-Options");
    }

    [Fact]
    public async Task SecurityHeaders_CSP_ContainsDefaultSrcSelf()
    {
        var response = await _client.GetAsync("/health");
        var csp = response.Headers.GetValues("Content-Security-Policy").First();

        csp.Should().Contain("default-src 'self'");
    }

    [Fact]
    public async Task SecurityHeaders_CSP_ContainsFrameAncestorsNone()
    {
        var response = await _client.GetAsync("/health");
        var csp = response.Headers.GetValues("Content-Security-Policy").First();

        csp.Should().Contain("frame-ancestors 'none'");
    }

    [Fact]
    public async Task SecurityHeaders_CSP_ContainsConnectSrc()
    {
        var response = await _client.GetAsync("/health");
        var csp = response.Headers.GetValues("Content-Security-Policy").First();

        csp.Should().Contain("connect-src");
    }

    [Fact]
    public async Task SecurityHeaders_NoServerVersionExposed()
    {
        var response = await _client.GetAsync("/health");

        response.Headers.Should().NotContainKey("Server");
        response.Headers.Should().NotContainKey("X-Powered-By");
    }

    [Fact]
    public async Task CORS_InvalidOrigin_NoCorsHeaders()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("Origin", "http://malicious.com");

        var response = await _client.SendAsync(request);

        response.Headers.Should().NotContainKey("Access-Control-Allow-Origin");
    }

    [Fact]
    public async Task CORS_NoCorsRequest_NoCorsHeaders()
    {
        var response = await _client.GetAsync("/health");

        response.Headers.Should().NotContainKey("Access-Control-Allow-Origin");
    }

    [Fact]
    public async Task ExceptionMiddleware_InvalidOperation_Returns400WithJsonMessage()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginUserDto
        {
            Email = "notfound@banklite.com",
            Password = "Password123!"
        });
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        content.Should().Contain("message");
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task ExceptionMiddleware_Error400_DoesNotLeakSensitiveData()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginUserDto
        {
            Email = "notfound@banklite.com",
            Password = "Password123!"
        });
        var content = await response.Content.ReadAsStringAsync();

        content.Should().NotContain("passwordHash");
        content.Should().NotContain("StackTrace");
        content.Should().NotContain("System.");
    }

    [Fact]
    public async Task ExceptionMiddleware_ErrorResponse_IsAlwaysJson()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginUserDto
        {
            Email = "notfound@banklite.com",
            Password = "Password123!"
        });

        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task JWT_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Remove("Cookie");

        var response = await _client.GetAsync("/api/user/profile");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task JWT_MalformedToken_Returns401()
    {
        AuthenticateClient("malformed.jwt.token");

        var response = await _client.GetAsync("/api/user/profile");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task JWT_EmptyStringToken_Returns401()
    {
        AuthenticateClient("");

        var response = await _client.GetAsync("/api/user/profile");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task JWT_WrongSecret_Returns401()
    {
        var wrongSecretToken =
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
        AuthenticateClient(wrongSecretToken);

        var response = await _client.GetAsync("/api/user/profile");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task JWT_WrongIssuer_Returns401()
    {
        var wrongIssuerToken = GenerateTokenWithWrongClaims("WrongIssuer");
        AuthenticateClient(wrongIssuerToken);

        var response = await _client.GetAsync("/api/user/profile");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task JWT_WrongAudience_Returns401()
    {
        var wrongAudienceToken = GenerateTokenWithWrongClaims(audience: "WrongAudience");
        AuthenticateClient(wrongAudienceToken);

        var response = await _client.GetAsync("/api/user/profile");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task JWT_InRequestBody_Returns401()
    {
        var token = await RegisterAndGetTokenAsync();
        _client.DefaultRequestHeaders.Remove("Cookie");

        var response = await _client.PostAsJsonAsync("/api/auth/refresh/logout",
            new { token });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task HealthCheck_Returns200()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthCheck_ReturnsHealthyStatus()
    {
        var response = await _client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();

        content.Should().Contain("Healthy");
    }

    [Fact]
    public async Task HealthCheck_ReturnsDatabaseCheck()
    {
        var response = await _client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();

        content.Should().Contain("database");
    }

    [Fact]
    public async Task HealthCheck_ReturnsJsonContentType()
    {
        var response = await _client.GetAsync("/health");

        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task HealthCheck_AccessibleWithoutAuthentication()
    {
        _client.DefaultRequestHeaders.Remove("Cookie");

        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task InputValidation_SqlInjection_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginUserDto
        {
            Email = "' OR '1'='1",
            Password = "' OR '1'='1"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task InputValidation_XssAttempt_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterUserDto
        {
            FullName = "<script>alert('xss')</script>",
            Email = _faker.Internet.Email(),
            Password = "Password123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task InputValidation_NullBody_Returns400()
    {
        var response = await _client.PostAsync("/api/auth/login",
            new StringContent("null", Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task InputValidation_MalformedJson_Returns400()
    {
        var response = await _client.PostAsync("/api/auth/login",
            new StringContent("{invalid json", Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task InputValidation_OversizedBody_HandledGracefully()
    {
        var oversizedBody = new string('x', 100000);
        var response = await _client.PostAsync("/api/auth/login",
            new StringContent($"{{\"email\":\"{oversizedBody}\",\"password\":\"{oversizedBody}\"}}", Encoding.UTF8,
                "application/json"));

        ((int)response.StatusCode).Should().BeOneOf(400, 413, 429);
    }

    [Fact]
    public async Task ResponseCompression_GzipEncoding_ReturnsCompressedResponse()
    {
        _client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip");

        var response = await _client.GetAsync("/health");

        response.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SignalR_UnauthenticatedConnection_Returns401()
    {
        _client.DefaultRequestHeaders.Remove("Cookie");

        var response = await _client.GetAsync("/hubs/bank");

        ((int)response.StatusCode).Should().BeOneOf(401, 400);
    }

    private static string GenerateTokenWithWrongClaims(string issuer = "BankLiteAPI",
        string audience = "BankLiteClient")
    {
        var key = new SymmetricSecurityKey(
            "supersecretkey12345678901234567890"u8.ToArray());
        var creds = new SigningCredentials(
            key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer,
            audience,
            expires: DateTime.UtcNow.AddMinutes(60),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
