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
public class AccountIntegrationTests : IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly BankLiteWebApplicationFactory _factory;
    private readonly Faker _faker = new();

    public AccountIntegrationTests(BankLiteWebApplicationFactory factory)
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

    private void AuthenticateClient(string accessToken)
    {
        _client.DefaultRequestHeaders.Remove("Cookie");
        _client.DefaultRequestHeaders.Add("Cookie", $"accessToken={accessToken}");
    }

    [Fact]
    public async Task CreateAccount_ChequingType_Returns201()
    {
        var token = await RegisterAndGetTokenAsync();
        AuthenticateClient(token);

        var response = await _client.PostAsJsonAsync("/api/account/create",
            new CreateAccountDto { Type = AccountType.Chequing });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateAccount_SavingsType_Returns201()
    {
        var token = await RegisterAndGetTokenAsync();
        AuthenticateClient(token);

        var response = await _client.PostAsJsonAsync("/api/account/create",
            new CreateAccountDto { Type = AccountType.Savings });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateAccount_ChequingType_ReturnsCorrectStructure()
    {
        var token = await RegisterAndGetTokenAsync();
        AuthenticateClient(token);

        var response = await _client.PostAsJsonAsync("/api/account/create",
            new CreateAccountDto { Type = AccountType.Chequing });
        var data = await response.Content.ReadFromJsonAsync<AccountResponseDto>();

        data.Should().NotBeNull();
        data!.Id.Should().NotBeEmpty();
        data.AccountNumber.Should().NotBeNullOrEmpty();
        data.Type.Should().Be("Chequing");
        data.Balance.Should().Be(0);
    }

    [Fact]
    public async Task CreateAccount_SavingsType_ReturnsCorrectStructure()
    {
        var token = await RegisterAndGetTokenAsync();
        AuthenticateClient(token);

        var response = await _client.PostAsJsonAsync("/api/account/create",
            new CreateAccountDto { Type = AccountType.Savings });
        var data = await response.Content.ReadFromJsonAsync<AccountResponseDto>();

        data.Should().NotBeNull();
        data!.Id.Should().NotBeEmpty();
        data.AccountNumber.Should().NotBeNullOrEmpty();
        data.Type.Should().Be("Savings");
        data.Balance.Should().Be(0);
    }

    [Fact]
    public async Task CreateAccount_NewAccount_StartsWithZeroBalance()
    {
        var token = await RegisterAndGetTokenAsync();
        AuthenticateClient(token);

        var response = await _client.PostAsJsonAsync("/api/account/create",
            new CreateAccountDto { Type = AccountType.Chequing });
        var data = await response.Content.ReadFromJsonAsync<AccountResponseDto>();

        data!.Balance.Should().Be(0);
    }

    [Fact]
    public async Task CreateAccount_NewAccount_AccountNumberIsExactly12Chars()
    {
        var token = await RegisterAndGetTokenAsync();
        AuthenticateClient(token);

        var response = await _client.PostAsJsonAsync("/api/account/create",
            new CreateAccountDto { Type = AccountType.Chequing });
        var data = await response.Content.ReadFromJsonAsync<AccountResponseDto>();

        data!.AccountNumber.Length.Should().Be(12);
    }

    [Fact]
    public async Task CreateAccount_NewAccount_AccountNumberIsUpperCase()
    {
        var token = await RegisterAndGetTokenAsync();
        AuthenticateClient(token);

        var response = await _client.PostAsJsonAsync("/api/account/create",
            new CreateAccountDto { Type = AccountType.Chequing });
        var data = await response.Content.ReadFromJsonAsync<AccountResponseDto>();

        data!.AccountNumber.Should().Be(data.AccountNumber.ToUpper());
    }

    [Fact]
    public async Task CreateAccount_NewAccount_AccountNumberIsAlphanumeric()
    {
        var token = await RegisterAndGetTokenAsync();
        AuthenticateClient(token);

        var response = await _client.PostAsJsonAsync("/api/account/create",
            new CreateAccountDto { Type = AccountType.Chequing });
        var data = await response.Content.ReadFromJsonAsync<AccountResponseDto>();

        data!.AccountNumber.Should().MatchRegex("^[A-Z0-9]+$");
    }

    [Fact]
    public async Task CreateAccount_NewAccount_CreatedAtIsRecent()
    {
        var token = await RegisterAndGetTokenAsync();
        AuthenticateClient(token);

        var response = await _client.PostAsJsonAsync("/api/account/create",
            new CreateAccountDto { Type = AccountType.Chequing });
        var data = await response.Content.ReadFromJsonAsync<AccountResponseDto>();

        data!.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task CreateAccount_BothTypes_Creates2Accounts()
    {
        var token = await RegisterAndGetTokenAsync();
        AuthenticateClient(token);

        await _client.PostAsJsonAsync("/api/account/create",
            new CreateAccountDto { Type = AccountType.Chequing });
        await _client.PostAsJsonAsync("/api/account/create",
            new CreateAccountDto { Type = AccountType.Savings });

        var response = await _client.GetAsync("/api/account");
        var accounts = await response.Content.ReadFromJsonAsync<List<AccountResponseDto>>();

        accounts!.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateAccount_DuplicateChequing_Returns400()
    {
        var token = await RegisterAndGetTokenAsync();
        AuthenticateClient(token);
        await _client.PostAsJsonAsync("/api/account/create",
            new CreateAccountDto { Type = AccountType.Chequing });

        var response = await _client.PostAsJsonAsync("/api/account/create",
            new CreateAccountDto { Type = AccountType.Chequing });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateAccount_DuplicateSavings_Returns400()
    {
        var token = await RegisterAndGetTokenAsync();
        AuthenticateClient(token);
        await _client.PostAsJsonAsync("/api/account/create",
            new CreateAccountDto { Type = AccountType.Savings });

        var response = await _client.PostAsJsonAsync("/api/account/create",
            new CreateAccountDto { Type = AccountType.Savings });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateAccount_ValidRequest_PersistsInDatabase()
    {
        var token = await RegisterAndGetTokenAsync();
        AuthenticateClient(token);

        var response = await _client.PostAsJsonAsync("/api/account/create",
            new CreateAccountDto { Type = AccountType.Chequing });
        var data = await response.Content.ReadFromJsonAsync<AccountResponseDto>();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BankLiteDbContext>();
        var account = db.Accounts.FirstOrDefault(a => a.Id == data!.Id);

        account.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAccount_ValidRequest_IdIsValidGuid()
    {
        var token = await RegisterAndGetTokenAsync();
        AuthenticateClient(token);

        var response = await _client.PostAsJsonAsync("/api/account/create",
            new CreateAccountDto { Type = AccountType.Chequing });
        var data = await response.Content.ReadFromJsonAsync<AccountResponseDto>();

        data!.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task CreateAccount_ValidRequest_IdDifferentFromUserId()
    {
        var token = await RegisterAndGetTokenAsync();
        AuthenticateClient(token);

        var response = await _client.PostAsJsonAsync("/api/account/create",
            new CreateAccountDto { Type = AccountType.Chequing });
        var data = await response.Content.ReadFromJsonAsync<AccountResponseDto>();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BankLiteDbContext>();
        var account = db.Accounts.FirstOrDefault(a => a.Id == data!.Id);

        account!.Id.Should().NotBe(account.UserId);
    }

    [Fact]
    public async Task CreateAccount_ValidRequest_ResponseContainsNoSensitiveData()
    {
        var token = await RegisterAndGetTokenAsync();
        AuthenticateClient(token);

        var response = await _client.PostAsJsonAsync("/api/account/create",
            new CreateAccountDto { Type = AccountType.Chequing });
        var content = await response.Content.ReadAsStringAsync();

        content.Should().NotContain("passwordHash");
        content.Should().NotContain("PasswordHash");
    }


    [Fact]
    public async Task CreateAccount_InvalidType_Returns400()
    {
        var token = await RegisterAndGetTokenAsync();
        AuthenticateClient(token);

        var response = await _client.PostAsJsonAsync("/api/account/create",
            new { type = 99 });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateAccount_Unauthenticated_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/account/create",
            new CreateAccountDto { Type = AccountType.Chequing });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAccounts_NoAccounts_Returns200WithEmptyList()
    {
        var token = await RegisterAndGetTokenAsync();
        AuthenticateClient(token);

        var response = await _client.GetAsync("/api/account");
        var accounts = await response.Content.ReadFromJsonAsync<List<AccountResponseDto>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        accounts!.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAccounts_AfterCreatingOne_ReturnsCorrectAccount()
    {
        var token = await RegisterAndGetTokenAsync();
        AuthenticateClient(token);
        await _client.PostAsJsonAsync("/api/account/create",
            new CreateAccountDto { Type = AccountType.Chequing });

        var response = await _client.GetAsync("/api/account");
        var accounts = await response.Content.ReadFromJsonAsync<List<AccountResponseDto>>();

        accounts.Should().NotBeNull();
        accounts!.Should().HaveCount(1);
        accounts![0].Type.Should().Be("Chequing");
    }

    [Fact]
    public async Task GetAccounts_TwoAccountsCreated_ReturnsBothWithCorrectTypes()
    {
        var token = await RegisterAndGetTokenAsync();
        AuthenticateClient(token);
        await _client.PostAsJsonAsync("/api/account/create",
            new CreateAccountDto { Type = AccountType.Chequing });
        await _client.PostAsJsonAsync("/api/account/create",
            new CreateAccountDto { Type = AccountType.Savings });

        var response = await _client.GetAsync("/api/account");
        var accounts = await response.Content.ReadFromJsonAsync<List<AccountResponseDto>>();

        accounts!.Should().HaveCount(2);
        accounts.Should().Contain(a => a.Type == "Chequing");
        accounts.Should().Contain(a => a.Type == "Savings");
    }

    [Fact]
    public async Task GetAccounts_DifferentUsers_CannotSeeEachOthersAccounts()
    {
        var token1 = await RegisterAndGetTokenAsync();
        AuthenticateClient(token1);
        await _client.PostAsJsonAsync("/api/account/create",
            new CreateAccountDto { Type = AccountType.Chequing });

        var token2 = await RegisterAndGetTokenAsync();
        AuthenticateClient(token2);

        var response = await _client.GetAsync("/api/account");
        var accounts = await response.Content.ReadFromJsonAsync<List<AccountResponseDto>>();

        accounts!.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAccounts_AccountNumbersAreUnique_AcrossTwoUsers()
    {
        var token1 = await RegisterAndGetTokenAsync();
        AuthenticateClient(token1);
        var response1 = await _client.PostAsJsonAsync("/api/account/create",
            new CreateAccountDto { Type = AccountType.Chequing });
        var account1 = await response1.Content.ReadFromJsonAsync<AccountResponseDto>();

        var token2 = await RegisterAndGetTokenAsync();
        AuthenticateClient(token2);
        var response2 = await _client.PostAsJsonAsync("/api/account/create",
            new CreateAccountDto { Type = AccountType.Chequing });
        var account2 = await response2.Content.ReadFromJsonAsync<AccountResponseDto>();

        account1!.AccountNumber.Should().NotBe(account2!.AccountNumber);
    }

    [Fact]
    public async Task GetAccounts_Unauthenticated_Returns401()
    {
        var response = await _client.GetAsync("/api/account");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAccounts_AfterCreate_ReturnsSameIdAsCreateResponse()
    {
        var token = await RegisterAndGetTokenAsync();
        AuthenticateClient(token);

        var createResponse = await _client.PostAsJsonAsync("/api/account/create",
            new CreateAccountDto { Type = AccountType.Chequing });
        var created = await createResponse.Content.ReadFromJsonAsync<AccountResponseDto>();

        var getResponse = await _client.GetAsync("/api/account");
        var accounts = await getResponse.Content.ReadFromJsonAsync<List<AccountResponseDto>>();

        accounts!.Should().Contain(a => a.Id == created!.Id);
    }
}