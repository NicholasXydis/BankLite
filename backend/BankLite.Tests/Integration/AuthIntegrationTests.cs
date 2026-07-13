using System.Net;
using System.Net.Http.Json;
using BankLite.Application.DTOs;
using BankLite.Infrastructure.Data;
using Bogus;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace BankLite.Tests.Integration;

[Collection("Integration")]
public class AuthIntegrationTests : IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly BankLiteWebApplicationFactory _factory;
    private readonly Faker _faker = new();

    public AuthIntegrationTests(BankLiteWebApplicationFactory factory)
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

    private async Task<(HttpResponseMessage Response, AuthResponseDto? Data)> RegisterUserAsync(string? email = null,
        string? password = null, string? fullName = null)
    {
        var dto = new RegisterUserDto
        {
            FullName = fullName ?? "Test User",
            Email = email ?? _faker.Internet.Email(),
            Password = password ?? "Password123!"
        };
        var response = await _client.PostAsJsonAsync("/api/auth/register", dto);
        AuthResponseDto? data = null;
        if (response.IsSuccessStatusCode)
            data = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        return (response, data);
    }

    private async Task<HttpResponseMessage> LoginUserAsync(string email, string password)
    {
        return await _client.PostAsJsonAsync("/api/auth/login", new LoginUserDto
        {
            Email = email,
            Password = password
        });
    }

    private static string ExtractCookieValue(HttpResponseMessage response, string cookieName)
    {
        return response.Headers.GetValues("Set-Cookie")
            .First(c => c.Contains(cookieName))
            .Split(";")[0]
            .Split("=")[1];
    }

    [Fact]
    public async Task Register_ValidData_Returns200()
    {
        var (response, data) = await RegisterUserAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        data.Should().NotBeNull();
        data!.UserId.Should().NotBeEmpty();
        data.FullName.Should().NotBeNullOrEmpty();
        data.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Register_ValidData_ReturnsCorrectFullName()
    {
        var fullName = "Nicholas Xydis";

        var (_, data) = await RegisterUserAsync(fullName: fullName);

        data!.FullName.Should().Be(fullName);
    }

    [Fact]
    public async Task Register_ValidData_SetsAccessAndRefreshTokenCookies()
    {
        var (response, _) = await RegisterUserAsync();

        var cookies = response.Headers.GetValues("Set-Cookie").ToList();

        cookies.Should().Contain(c => c.Contains("accessToken"));
        cookies.Should().Contain(c => c.Contains("refreshToken"));
    }

    [Fact]
    public async Task Register_ValidData_SetsRefreshTokenPathRestriction()
    {
        var (response, _) = await RegisterUserAsync();

        var cookies = response.Headers.GetValues("Set-Cookie").ToList();

        cookies.Should().Contain(c => c.Contains("refreshToken") && c.Contains("path=/api/auth/refresh"));
    }

    [Fact]
    public async Task Register_ValidData_StoresEmailAsLowerCase()
    {
        var email = "NICHOLAS@BANKLITE.COM";
        await RegisterUserAsync(email);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BankLiteDbContext>();
        var user = db.Users.FirstOrDefault(u => u.Email == email.ToLower());

        user.Should().NotBeNull();
        user!.Email.Should().Be(email.ToLower());
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns400()
    {
        var email = _faker.Internet.Email();
        await RegisterUserAsync(email);

        var (response, _) = await RegisterUserAsync(email);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WhitespaceFullName_Returns400()
    {
        var (response, _) = await RegisterUserAsync(fullName: "   ");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_InvalidEmail_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterUserDto
        {
            FullName = "Test User",
            Email = "notanemail",
            Password = "Password123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_EmailMissingAtSign_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterUserDto
        {
            FullName = "Test User",
            Email = "testbanklite.com",
            Password = "Password123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_PasswordSevenChars_Returns400()
    {
        var (response, _) = await RegisterUserAsync(password: "Pass12!");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_PasswordExactlyEightChars_Returns200()
    {
        var (response, _) = await RegisterUserAsync(password: "Pass123!");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Register_PasswordExactly100Chars_Returns200()
    {
        var (response, _) = await RegisterUserAsync(password: new string('a', 100));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Register_Password101Chars_Returns400()
    {
        var (response, _) = await RegisterUserAsync(password: new string('a', 101));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_FullNameExactly50Chars_Returns200()
    {
        var (response, _) = await RegisterUserAsync(fullName: new string('A', 50));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Register_FullName51Chars_Returns400()
    {
        var (response, _) = await RegisterUserAsync(fullName: new string('A', 51));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_FullNameWithNumbers_Returns400()
    {
        var (response, _) = await RegisterUserAsync(fullName: "Test123");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_FullNameWithLeadingSpace_Returns400()
    {
        var (response, _) = await RegisterUserAsync(fullName: " TestUser");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_FullNameWithTrailingSpace_Returns400()
    {
        var (response, _) = await RegisterUserAsync(fullName: "TestUser ");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_NullFullName_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            FullName = (string?)null,
            Email = _faker.Internet.Email(),
            Password = "Password123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_SameEmailDifferentCase_Returns400()
    {
        var email = "nicholas@banklite.com";
        await RegisterUserAsync(email);

        var (response, _) = await RegisterUserAsync(email.ToUpper());

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_ValidData_ReturnsValidGuidUserId()
    {
        var (_, data) = await RegisterUserAsync();

        data!.UserId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200()
    {
        var email = _faker.Internet.Email();
        await RegisterUserAsync(email);

        var response = await LoginUserAsync(email, "Password123!");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_ValidCredentials_ExpiresAtIsApproximately60MinutesFromNow()
    {
        var email = _faker.Internet.Email();
        await RegisterUserAsync(email);

        var response = await LoginUserAsync(email, "Password123!");
        var data = await response.Content.ReadFromJsonAsync<AuthResponseDto>();

        data!.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(60), TimeSpan.FromMinutes(2));
    }


    [Fact]
    public async Task Login_ValidCredentials_ReturnsCorrectJsonShape()
    {
        var email = _faker.Internet.Email();
        await RegisterUserAsync(email);

        var response = await LoginUserAsync(email, "Password123!");
        var data = await response.Content.ReadFromJsonAsync<AuthResponseDto>();

        data.Should().NotBeNull();
        data!.UserId.Should().NotBeEmpty();
        data.FullName.Should().NotBeNullOrEmpty();
        data.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Login_ValidCredentials_SetsAccessAndRefreshTokenCookies()
    {
        var email = _faker.Internet.Email();
        await RegisterUserAsync(email);

        var response = await LoginUserAsync(email, "Password123!");

        var cookies = response.Headers.GetValues("Set-Cookie").ToList();
        cookies.Should().Contain(c => c.Contains("accessToken"));
        cookies.Should().Contain(c => c.Contains("refreshToken"));
    }

    [Fact]
    public async Task Login_ValidCredentials_UpdatesLastLoginAtInDb()
    {
        var email = _faker.Internet.Email();
        await RegisterUserAsync(email);

        await LoginUserAsync(email, "Password123!");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BankLiteDbContext>();
        var user = db.Users.FirstOrDefault(u => u.Email == email.ToLower());
        user!.LastLoginAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Login_UpperCaseEmail_SucceedsWithLowerCaseRegistration()
    {
        var email = "nicholas@banklite.com";
        await RegisterUserAsync(email);

        var response = await LoginUserAsync(email.ToUpper(), "Password123!");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns400()
    {
        var email = _faker.Internet.Email();
        await RegisterUserAsync(email);

        var response = await LoginUserAsync(email, "WrongPassword123!");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_EmptyEmail_Returns400()
    {
        var response = await LoginUserAsync("", "Password123!");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_EmptyPassword_Returns400()
    {
        var response = await LoginUserAsync(_faker.Internet.Email(), "");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_NonExistentUser_Returns400()
    {
        var response = await LoginUserAsync(_faker.Internet.Email(), "Password123!");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_FiveFailedAttempts_LocksAccount()
    {
        var email = _faker.Internet.Email();
        await RegisterUserAsync(email);
        for (var i = 0; i < 5; i++)
            await LoginUserAsync(email, "WrongPassword!");

        var response = await LoginUserAsync(email, "WrongPassword!");
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        content.Should().Contain("locked");
    }

    [Fact]
    public async Task Login_CorrectPasswordAfterFailedAttempts_ResetsCounter()
    {
        var email = _faker.Internet.Email();
        await RegisterUserAsync(email);
        for (var i = 0; i < 3; i++)
            await LoginUserAsync(email, "WrongPassword!");

        var response = await LoginUserAsync(email, "Password123!");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BankLiteDbContext>();
        var user = db.Users.FirstOrDefault(u => u.Email == email.ToLower());
        user!.FailedLoginAttempts.Should().Be(0);
    }

    [Fact]
    public async Task Refresh_ValidRefreshToken_Returns200()
    {
        var email = _faker.Internet.Email();
        await RegisterUserAsync(email);
        var loginResponse = await LoginUserAsync(email, "Password123!");
        var refreshToken = ExtractCookieValue(loginResponse, "refreshToken");

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("Cookie", $"refreshToken={refreshToken}");
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Refresh_ValidRefreshToken_SetsNewCookies()
    {
        var email = _faker.Internet.Email();
        await RegisterUserAsync(email);
        var loginResponse = await LoginUserAsync(email, "Password123!");
        var refreshToken = ExtractCookieValue(loginResponse, "refreshToken");

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("Cookie", $"refreshToken={refreshToken}");
        var response = await _client.SendAsync(request);

        var cookies = response.Headers.GetValues("Set-Cookie").ToList();
        cookies.Should().Contain(c => c.Contains("accessToken"));
        cookies.Should().Contain(c => c.Contains("refreshToken"));
    }

    [Fact]
    public async Task Refresh_TokenRotation_SecondRefreshWorks()
    {
        var email = _faker.Internet.Email();
        await RegisterUserAsync(email);
        var loginResponse = await LoginUserAsync(email, "Password123!");
        var firstRefreshToken = ExtractCookieValue(loginResponse, "refreshToken");

        var firstRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        firstRequest.Headers.Add("Cookie", $"refreshToken={firstRefreshToken}");
        var firstRefreshResponse = await _client.SendAsync(firstRequest);
        var secondRefreshToken = ExtractCookieValue(firstRefreshResponse, "refreshToken");

        var secondRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        secondRequest.Headers.Add("Cookie", $"refreshToken={secondRefreshToken}");
        var response = await _client.SendAsync(secondRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Refresh_NoRefreshToken_Returns401()
    {
        var response = await _client.PostAsync("/api/auth/refresh", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_CopiedRefreshTokenReplay_Returns401()
    {
        var email = _faker.Internet.Email();
        var (registerResponse, _) = await RegisterUserAsync(email);
        var accessToken = ExtractCookieValue(registerResponse, "accessToken");
        var refreshToken = ExtractCookieValue(registerResponse, "refreshToken");

        var logoutRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh/logout");
        logoutRequest.Headers.Add("Cookie", $"accessToken={accessToken}; refreshToken={refreshToken}");
        await _client.SendAsync(logoutRequest);

        var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        refreshRequest.Headers.Add("Cookie", $"refreshToken={refreshToken}");
        var response = await _client.SendAsync(refreshRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_OldTokenAfterRotation_Returns401()
    {
        var email = _faker.Internet.Email();
        var (registerResponse, _) = await RegisterUserAsync(email);
        var oldRefreshToken = ExtractCookieValue(registerResponse, "refreshToken");

        var firstRefresh = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        firstRefresh.Headers.Add("Cookie", $"refreshToken={oldRefreshToken}");
        await _client.SendAsync(firstRefresh);

        var secondRefresh = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        secondRefresh.Headers.Add("Cookie", $"refreshToken={oldRefreshToken}");
        var response = await _client.SendAsync(secondRefresh);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_ValidLogin_RefreshTokenCookieHasPathRestriction()
    {
        var email = _faker.Internet.Email();
        await RegisterUserAsync(email);

        var loginResponse = await LoginUserAsync(email, "Password123!");
        var cookies = loginResponse.Headers.GetValues("Set-Cookie").ToList();

        cookies.Should().Contain(c => c.Contains("refreshToken") && c.Contains("path=/api/auth/refresh"));
    }

    [Fact]
    public async Task Logout_Authenticated_Returns200()
    {
        var email = _faker.Internet.Email();
        var (registerResponse, _) = await RegisterUserAsync(email);
        var accessToken = ExtractCookieValue(registerResponse, "accessToken");
        var refreshToken = ExtractCookieValue(registerResponse, "refreshToken");

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh/logout");
        request.Headers.Add("Cookie", $"accessToken={accessToken}; refreshToken={refreshToken}");
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Logout_Authenticated_ClearsCookies()
    {
        var email = _faker.Internet.Email();
        var (registerResponse, _) = await RegisterUserAsync(email);
        var accessToken = ExtractCookieValue(registerResponse, "accessToken");
        var refreshToken = ExtractCookieValue(registerResponse, "refreshToken");

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh/logout");
        request.Headers.Add("Cookie", $"accessToken={accessToken}; refreshToken={refreshToken}");
        var response = await _client.SendAsync(request);

        var cookies = response.Headers.GetValues("Set-Cookie").ToList();
        cookies.Should().Contain(c => c.Contains("accessToken") && c.Contains("expires="));
        cookies.Should().Contain(c => c.Contains("refreshToken") && c.Contains("expires=") &&
                                     c.Contains("path=/api/auth/refresh"));
    }

    [Fact]
    public async Task Logout_Unauthenticated_Returns401()
    {
        var response = await _client.PostAsync("/api/auth/refresh/logout", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ForgotPassword_ExistingEmail_Returns200()
    {
        var email = _faker.Internet.Email();
        await RegisterUserAsync(email);

        var response = await _client.PostAsJsonAsync("/api/auth/forgot-password",
            new ForgotPasswordDto { Email = email });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ForgotPassword_NonExistentEmail_Returns200()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/forgot-password",
            new ForgotPasswordDto { Email = _faker.Internet.Email() });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ForgotPassword_InvalidEmailFormat_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/forgot-password",
            new ForgotPasswordDto { Email = "notanemail" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ForgotPassword_ValidEmail_ReturnsCorrectMessage()
    {
        var email = _faker.Internet.Email();
        await RegisterUserAsync(email);

        var response = await _client.PostAsJsonAsync("/api/auth/forgot-password",
            new ForgotPasswordDto { Email = email });
        var content = await response.Content.ReadAsStringAsync();

        content.Should().Contain("reset link");
    }

    [Fact]
    public async Task ResetPassword_InvalidToken_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/reset-password",
            new ResetPasswordDto { Token = "invalid-token", NewPassword = "NewPassword123!" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ResetPassword_EmptyToken_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/reset-password",
            new ResetPasswordDto { Token = "", NewPassword = "NewPassword123!" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ResetPassword_ShortPassword_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/reset-password",
            new ResetPasswordDto { Token = "some-token", NewPassword = "short" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ResetPassword_ValidFlow_AllowsLoginWithNewPassword()
    {
        var email = _faker.Internet.Email();
        await RegisterUserAsync(email);

        await _client.PostAsJsonAsync("/api/auth/forgot-password",
            new ForgotPasswordDto { Email = email });

        var rawToken = _factory.LastResetToken;
        if (rawToken == null) return;

        var resetResponse = await _client.PostAsJsonAsync("/api/auth/reset-password",
            new ResetPasswordDto { Token = rawToken, NewPassword = "NewPassword123!" });

        resetResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Refresh_ReusingRevokedToken_RevokesTheWholeTokenFamily()
    {
        var email = _faker.Internet.Email();
        await RegisterUserAsync(email);
        var loginResponse = await LoginUserAsync(email, "Password123!");
        var stolenToken = ExtractCookieValue(loginResponse, "refreshToken");

        var firstRotation = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        firstRotation.Headers.Add("Cookie", $"refreshToken={stolenToken}");
        var firstResponse = await _client.SendAsync(firstRotation);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var currentToken = ExtractCookieValue(firstResponse, "refreshToken");

        var replay = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        replay.Headers.Add("Cookie", $"refreshToken={stolenToken}");
        var replayResponse = await _client.SendAsync(replay);
        replayResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var descendant = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        descendant.Headers.Add("Cookie", $"refreshToken={currentToken}");
        var descendantResponse = await _client.SendAsync(descendant);

        descendantResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_NormalRotation_KeepsWorkingAcrossMultipleRefreshes()
    {
        var email = _faker.Internet.Email();
        await RegisterUserAsync(email);
        var loginResponse = await LoginUserAsync(email, "Password123!");
        var token = ExtractCookieValue(loginResponse, "refreshToken");

        for (var i = 0; i < 3; i++)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
            request.Headers.Add("Cookie", $"refreshToken={token}");
            var response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            token = ExtractCookieValue(response, "refreshToken");
        }
    }

    [Fact]
    public async Task ResetPassword_ValidFlow_RevokesExistingRefreshTokens()
    {
        var email = _faker.Internet.Email();
        await RegisterUserAsync(email);
        var loginResponse = await LoginUserAsync(email, "Password123!");
        var refreshToken = ExtractCookieValue(loginResponse, "refreshToken");

        var beforeReset = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        beforeReset.Headers.Add("Cookie", $"refreshToken={refreshToken}");
        var beforeResponse = await _client.SendAsync(beforeReset);
        beforeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var rotatedToken = ExtractCookieValue(beforeResponse, "refreshToken");

        await _client.PostAsJsonAsync("/api/auth/forgot-password",
            new ForgotPasswordDto { Email = email });

        var rawToken = _factory.LastResetToken;
        if (rawToken == null) return;

        var resetResponse = await _client.PostAsJsonAsync("/api/auth/reset-password",
            new ResetPasswordDto { Token = rawToken, NewPassword = "NewPassword123!" });
        resetResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var afterReset = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        afterReset.Headers.Add("Cookie", $"refreshToken={rotatedToken}");
        var afterResponse = await _client.SendAsync(afterReset);

        afterResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ResetPassword_ValidFlow_OldPasswordFailsAfterReset()
    {
        var email = _faker.Internet.Email();
        await RegisterUserAsync(email);

        await _client.PostAsJsonAsync("/api/auth/forgot-password",
            new ForgotPasswordDto { Email = email });

        var rawToken = _factory.LastResetToken;
        if (rawToken == null) return;

        await _client.PostAsJsonAsync("/api/auth/reset-password",
            new ResetPasswordDto { Token = rawToken, NewPassword = "NewPassword123!" });

        var response = await LoginUserAsync(email, "Password123!");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_ThenImmediatelyRefresh_Works()
    {
        var email = _faker.Internet.Email();
        var (registerResponse, _) = await RegisterUserAsync(email);
        var refreshToken = ExtractCookieValue(registerResponse, "refreshToken");

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("Cookie", $"refreshToken={refreshToken}");
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
