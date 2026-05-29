using System.Net;
using System.Net.Http.Json;
using BankLite.Application.DTOs;
using BankLite.Domain.Entities;
using BankLite.Infrastructure.Data;
using Bogus;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace BankLite.Tests.Integration;

[Collection("Integration")]
public class UserIntegrationTests : IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly BankLiteWebApplicationFactory _factory;
    private readonly Faker _faker = new();

    public UserIntegrationTests(BankLiteWebApplicationFactory factory)
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

    private async Task<(string Token, string Email)> RegisterAndAuthenticateAsync()
    {
        var email = _faker.Internet.Email();
        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterUserDto
        {
            FullName = "Test User",
            Email = email,
            Password = "Password123!"
        });
        var cookies = response.Headers.GetValues("Set-Cookie").ToList();
        var token = cookies.First(c => c.Contains("accessToken")).Split(";")[0].Split("=")[1];
        AuthenticateClient(token);
        return (token, email);
    }

    private void AuthenticateClient(string token)
    {
        _client.DefaultRequestHeaders.Remove("Cookie");
        _client.DefaultRequestHeaders.Add("Cookie", $"accessToken={token}");
    }

    private BankLiteDbContext GetDb()
    {
        var scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<BankLiteDbContext>();
    }

    [Fact]
    public async Task GetProfile_ValidRequest_Returns200()
    {
        await RegisterAndAuthenticateAsync();

        var response = await _client.GetAsync("/api/user/profile");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetProfile_ValidRequest_ReturnsCorrectData()
    {
        var fullName = "Nicholas Xydis";
        var email = _faker.Internet.Email();
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new RegisterUserDto
        {
            FullName = fullName,
            Email = email,
            Password = "Password123!"
        });
        var cookies = registerResponse.Headers.GetValues("Set-Cookie").ToList();
        var token = cookies.First(c => c.Contains("accessToken")).Split(";")[0].Split("=")[1];
        AuthenticateClient(token);

        var response = await _client.GetAsync("/api/user/profile");
        var data = await response.Content.ReadFromJsonAsync<UserProfileDto>();

        data.Should().NotBeNull();
        data!.FullName.Should().Be(fullName);
        data.Email.Should().Be(email.ToLower());
    }

    [Fact]
    public async Task GetProfile_ValidRequest_ReturnsCorrectCreatedAt()
    {
        await RegisterAndAuthenticateAsync();

        var response = await _client.GetAsync("/api/user/profile");
        var data = await response.Content.ReadFromJsonAsync<UserProfileDto>();

        data!.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task GetProfile_BeforeLogin_ReturnsNullLastLoginAt()
    {
        await RegisterAndAuthenticateAsync();

        var response = await _client.GetAsync("/api/user/profile");
        var data = await response.Content.ReadFromJsonAsync<UserProfileDto>();

        data!.LastLoginAt.Should().BeNull();
    }

    [Fact]
    public async Task GetProfile_AfterLogin_ReturnsLastLoginAt()
    {
        var (_, email) = await RegisterAndAuthenticateAsync();
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginUserDto
        {
            Email = email,
            Password = "Password123!"
        });
        var cookies = loginResponse.Headers.GetValues("Set-Cookie").ToList();
        var token = cookies.First(c => c.Contains("accessToken")).Split(";")[0].Split("=")[1];
        AuthenticateClient(token);

        var response = await _client.GetAsync("/api/user/profile");
        var data = await response.Content.ReadFromJsonAsync<UserProfileDto>();

        data!.LastLoginAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetProfile_ValidRequest_ReturnsCorrectEmail()
    {
        var (_, email) = await RegisterAndAuthenticateAsync();

        var response = await _client.GetAsync("/api/user/profile");
        var data = await response.Content.ReadFromJsonAsync<UserProfileDto>();

        data!.Email.Should().Be(email.ToLower());
    }

    [Fact]
    public async Task GetProfile_ValidRequest_ContainsNoSensitiveData()
    {
        await RegisterAndAuthenticateAsync();

        var response = await _client.GetAsync("/api/user/profile");
        var content = await response.Content.ReadAsStringAsync();

        content.Should().NotContain("passwordHash");
        content.Should().NotContain("PasswordHash");
    }

    [Fact]
    public async Task GetProfile_Unauthenticated_Returns401()
    {
        _client.DefaultRequestHeaders.Remove("Cookie");

        var response = await _client.GetAsync("/api/user/profile");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProfile_InvalidJwt_Returns401()
    {
        AuthenticateClient("invalid.jwt.token");

        var response = await _client.GetAsync("/api/user/profile");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProfile_AfterChangePassword_StillWorks()
    {
        await RegisterAndAuthenticateAsync();
        await _client.PostAsJsonAsync("/api/user/change-password", new ChangePasswordDto
        {
            CurrentPassword = "Password123!",
            NewPassword = "NewPassword123!"
        });

        var response = await _client.GetAsync("/api/user/profile");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ChangePassword_ValidRequest_Returns200()
    {
        await RegisterAndAuthenticateAsync();

        var response = await _client.PostAsJsonAsync("/api/user/change-password", new ChangePasswordDto
        {
            CurrentPassword = "Password123!",
            NewPassword = "NewPassword123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ChangePassword_ValidRequest_ReturnsCorrectMessage()
    {
        await RegisterAndAuthenticateAsync();

        var response = await _client.PostAsJsonAsync("/api/user/change-password", new ChangePasswordDto
        {
            CurrentPassword = "Password123!",
            NewPassword = "NewPassword123!"
        });
        var content = await response.Content.ReadAsStringAsync();

        content.Should().Contain("Password changed successfully");
    }

    [Fact]
    public async Task ChangePassword_ValidRequest_ChangesPasswordInDb()
    {
        var (_, email) = await RegisterAndAuthenticateAsync();

        await _client.PostAsJsonAsync("/api/user/change-password", new ChangePasswordDto
        {
            CurrentPassword = "Password123!",
            NewPassword = "NewPassword123!"
        });

        var db = GetDb();
        var user = db.Users.FirstOrDefault(u => u.Email == email.ToLower());
        BCrypt.Net.BCrypt.Verify("NewPassword123!", user!.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task ChangePassword_ValidRequest_HashesNewPasswordInDb()
    {
        var (_, email) = await RegisterAndAuthenticateAsync();

        await _client.PostAsJsonAsync("/api/user/change-password", new ChangePasswordDto
        {
            CurrentPassword = "Password123!",
            NewPassword = "NewPassword123!"
        });

        var db = GetDb();
        var user = db.Users.FirstOrDefault(u => u.Email == email.ToLower());
        user!.PasswordHash.Should().NotBe("NewPassword123!");
    }

    [Fact]
    public async Task ChangePassword_ValidRequest_RevokesAllRefreshTokensInDb()
    {
        var (_, email) = await RegisterAndAuthenticateAsync();
        await _client.PostAsJsonAsync("/api/auth/login", new LoginUserDto
        {
            Email = email,
            Password = "Password123!"
        });

        await _client.PostAsJsonAsync("/api/user/change-password", new ChangePasswordDto
        {
            CurrentPassword = "Password123!",
            NewPassword = "NewPassword123!"
        });

        var db = GetDb();
        var user = db.Users.FirstOrDefault(u => u.Email == email.ToLower());
        var tokens = db.RefreshTokens.Where(rt => rt.UserId == user!.Id).ToList();
        tokens.Should().AllSatisfy(rt => rt.IsRevoked.Should().BeTrue());
    }

    [Fact]
    public async Task ChangePassword_ValidRequest_AllowsLoginWithNewPassword()
    {
        var (_, email) = await RegisterAndAuthenticateAsync();

        await _client.PostAsJsonAsync("/api/user/change-password", new ChangePasswordDto
        {
            CurrentPassword = "Password123!",
            NewPassword = "NewPassword123!"
        });

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginUserDto
        {
            Email = email,
            Password = "NewPassword123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ChangePassword_ValidRequest_BlocksLoginWithOldPassword()
    {
        var (_, email) = await RegisterAndAuthenticateAsync();

        await _client.PostAsJsonAsync("/api/user/change-password", new ChangePasswordDto
        {
            CurrentPassword = "Password123!",
            NewPassword = "NewPassword123!"
        });

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginUserDto
        {
            Email = email,
            Password = "Password123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangePassword_ValidRequest_ContainsNoSensitiveData()
    {
        await RegisterAndAuthenticateAsync();

        var response = await _client.PostAsJsonAsync("/api/user/change-password", new ChangePasswordDto
        {
            CurrentPassword = "Password123!",
            NewPassword = "NewPassword123!"
        });
        var content = await response.Content.ReadAsStringAsync();

        content.Should().NotContain("passwordHash");
        content.Should().NotContain("PasswordHash");
    }

    [Fact]
    public async Task ChangePassword_TwiceInARow_Works()
    {
        await RegisterAndAuthenticateAsync();

        await _client.PostAsJsonAsync("/api/user/change-password", new ChangePasswordDto
        {
            CurrentPassword = "Password123!",
            NewPassword = "NewPassword123!"
        });

        var response = await _client.PostAsJsonAsync("/api/user/change-password", new ChangePasswordDto
        {
            CurrentPassword = "NewPassword123!",
            NewPassword = "AnotherPassword123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_Returns400()
    {
        await RegisterAndAuthenticateAsync();

        var response = await _client.PostAsJsonAsync("/api/user/change-password", new ChangePasswordDto
        {
            CurrentPassword = "WrongPassword123!",
            NewPassword = "NewPassword123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangePassword_SamePassword_Returns400()
    {
        await RegisterAndAuthenticateAsync();

        var response = await _client.PostAsJsonAsync("/api/user/change-password", new ChangePasswordDto
        {
            CurrentPassword = "Password123!",
            NewPassword = "Password123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangePassword_EmptyCurrentPassword_Returns400()
    {
        await RegisterAndAuthenticateAsync();

        var response = await _client.PostAsJsonAsync("/api/user/change-password", new ChangePasswordDto
        {
            CurrentPassword = "",
            NewPassword = "NewPassword123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangePassword_EmptyNewPassword_Returns400()
    {
        await RegisterAndAuthenticateAsync();

        var response = await _client.PostAsJsonAsync("/api/user/change-password", new ChangePasswordDto
        {
            CurrentPassword = "Password123!",
            NewPassword = ""
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangePassword_NewPasswordSevenChars_Returns400()
    {
        await RegisterAndAuthenticateAsync();

        var response = await _client.PostAsJsonAsync("/api/user/change-password", new ChangePasswordDto
        {
            CurrentPassword = "Password123!",
            NewPassword = "Pass12!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangePassword_NewPasswordExactly8Chars_Returns200()
    {
        await RegisterAndAuthenticateAsync();

        var response = await _client.PostAsJsonAsync("/api/user/change-password", new ChangePasswordDto
        {
            CurrentPassword = "Password123!",
            NewPassword = "Pass123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ChangePassword_NewPasswordExactly100Chars_Returns200()
    {
        await RegisterAndAuthenticateAsync();

        var response = await _client.PostAsJsonAsync("/api/user/change-password", new ChangePasswordDto
        {
            CurrentPassword = "Password123!",
            NewPassword = new string('a', 100)
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ChangePassword_NewPassword101Chars_Returns400()
    {
        await RegisterAndAuthenticateAsync();

        var response = await _client.PostAsJsonAsync("/api/user/change-password", new ChangePasswordDto
        {
            CurrentPassword = "Password123!",
            NewPassword = new string('a', 101)
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangePassword_Unauthenticated_Returns401()
    {
        _client.DefaultRequestHeaders.Remove("Cookie");

        var response = await _client.PostAsJsonAsync("/api/user/change-password", new ChangePasswordDto
        {
            CurrentPassword = "Password123!",
            NewPassword = "NewPassword123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChangePassword_OldRefreshTokenRevoked_Returns401()
    {
        var (_, email) = await RegisterAndAuthenticateAsync();
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginUserDto
        {
            Email = email,
            Password = "Password123!"
        });
        var refreshToken = loginResponse.Headers.GetValues("Set-Cookie")
            .First(c => c.Contains("refreshToken")).Split(";")[0].Split("=")[1];

        await _client.PostAsJsonAsync("/api/user/change-password", new ChangePasswordDto
        {
            CurrentPassword = "Password123!",
            NewPassword = "NewPassword123!"
        });

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("Cookie", $"refreshToken={refreshToken}");
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteAccount_ValidRequest_Returns200()
    {
        await RegisterAndAuthenticateAsync();

        var response = await _client.DeleteAsync("/api/user/delete-account");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteAccount_ValidRequest_ReturnsCorrectMessage()
    {
        await RegisterAndAuthenticateAsync();

        var response = await _client.DeleteAsync("/api/user/delete-account");
        var content = await response.Content.ReadAsStringAsync();

        content.Should().Contain("deleted successfully");
    }

    [Fact]
    public async Task DeleteAccount_ValidRequest_RemovesUserFromDb()
    {
        var (_, email) = await RegisterAndAuthenticateAsync();

        await _client.DeleteAsync("/api/user/delete-account");

        var db = GetDb();
        db.Users.FirstOrDefault(u => u.Email == email.ToLower()).Should().BeNull();
    }

    [Fact]
    public async Task DeleteAccount_ValidRequest_CascadesAccountsFromDb()
    {
        var (_, email) = await RegisterAndAuthenticateAsync();
        await _client.PostAsJsonAsync("/api/account/create",
            new CreateAccountDto { Type = AccountType.Chequing });

        await _client.DeleteAsync("/api/user/delete-account");

        var db = GetDb();
        var user = db.Users.FirstOrDefault(u => u.Email == email.ToLower());
        db.Accounts.Where(a => a.UserId == (user == null ? Guid.Empty : user.Id)).AsEnumerable().Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAccount_ValidRequest_CascadesTransactionsFromDb()
    {
        await RegisterAndAuthenticateAsync();
        var accountResponse = await _client.PostAsJsonAsync("/api/account/create",
            new CreateAccountDto { Type = AccountType.Chequing });
        var account = await accountResponse.Content.ReadFromJsonAsync<AccountResponseDto>();
        await _client.PostAsJsonAsync("/api/transaction/deposit",
            new DepositWithdrawDto { AccountId = account!.Id, Amount = 100 });

        await _client.DeleteAsync("/api/user/delete-account");

        var db = GetDb();
        db.Transactions.Where(t => t.AccountId == account.Id).AsEnumerable().Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAccount_ValidRequest_CascadesRefreshTokensFromDb()
    {
        var (_, email) = await RegisterAndAuthenticateAsync();

        await _client.DeleteAsync("/api/user/delete-account");

        var db = GetDb();
        var user = db.Users.FirstOrDefault(u => u.Email == email.ToLower());
        db.RefreshTokens.Where(rt => rt.UserId == (user == null ? Guid.Empty : user.Id)).AsEnumerable().Should()
            .BeEmpty();
    }

    [Fact]
    public async Task DeleteAccount_ValidRequest_CascadesPasswordResetTokensFromDb()
    {
        var (_, email) = await RegisterAndAuthenticateAsync();

        await _client.DeleteAsync("/api/user/delete-account");

        var db = GetDb();
        var user = db.Users.FirstOrDefault(u => u.Email == email.ToLower());
        db.PasswordResetTokens.Where(p => p.UserId == (user == null ? Guid.Empty : user.Id)).AsEnumerable().Should()
            .BeEmpty();
    }

    [Fact]
    public async Task DeleteAccount_ValidRequest_CascadesAuditLogsFromDb()
    {
        var (_, email) = await RegisterAndAuthenticateAsync();

        await _client.DeleteAsync("/api/user/delete-account");

        var db = GetDb();
        var user = db.Users.FirstOrDefault(u => u.Email == email.ToLower());
        db.AuditLogs.Where(a => a.UserId == (user == null ? Guid.Empty : user.Id)).AsEnumerable().Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAccount_ValidRequest_ClearsCookies()
    {
        await RegisterAndAuthenticateAsync();

        var response = await _client.DeleteAsync("/api/user/delete-account");
        var cookies = response.Headers.GetValues("Set-Cookie").ToList();

        cookies.Should().Contain(c => c.Contains("accessToken") && c.Contains("expires="));
    }

    [Fact]
    public async Task DeleteAccount_ValidRequest_LoginFailsAfterDelete()
    {
        var (_, email) = await RegisterAndAuthenticateAsync();

        await _client.DeleteAsync("/api/user/delete-account");

        _client.DefaultRequestHeaders.Remove("Cookie");
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginUserDto
        {
            Email = email,
            Password = "Password123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteAccount_ValidRequest_GetProfileFailsAfterDelete()
    {
        await RegisterAndAuthenticateAsync();
        await _client.DeleteAsync("/api/user/delete-account");

        _client.DefaultRequestHeaders.Remove("Cookie");
        var response = await _client.GetAsync("/api/user/profile");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteAccount_ValidRequest_RefreshTokenFailsAfterDelete()
    {
        var (_, email) = await RegisterAndAuthenticateAsync();
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginUserDto
        {
            Email = email,
            Password = "Password123!"
        });
        var refreshToken = loginResponse.Headers.GetValues("Set-Cookie")
            .First(c => c.Contains("refreshToken")).Split(";")[0].Split("=")[1];

        await _client.DeleteAsync("/api/user/delete-account");

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("Cookie", $"refreshToken={refreshToken}");
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteAccount_Unauthenticated_Returns401()
    {
        _client.DefaultRequestHeaders.Remove("Cookie");

        var response = await _client.DeleteAsync("/api/user/delete-account");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteAccount_SameEmailCanRegisterAgain()
    {
        var (_, email) = await RegisterAndAuthenticateAsync();
        await _client.DeleteAsync("/api/user/delete-account");

        _client.DefaultRequestHeaders.Remove("Cookie");
        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterUserDto
        {
            FullName = _faker.Name.FullName(),
            Email = email,
            Password = "Password123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}