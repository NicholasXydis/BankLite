using System.Net;
using System.Net.Http.Json;
using BankLite.Application.DTOs;
using BankLite.Domain.Entities;
using BankLite.Infrastructure.Data;
using Bogus;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BankLite.Tests.Integration
{
    [Collection("Integration")]
    public class TransactionIntegrationTests : IAsyncLifetime
    {
        private readonly HttpClient _client;
        private readonly BankLiteWebApplicationFactory _factory;
        private readonly Faker _faker = new Faker();

        public TransactionIntegrationTests(BankLiteWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = false
            });
        }

        public async Task InitializeAsync() => await _factory.ResetDatabaseAsync();
        public Task DisposeAsync() => Task.CompletedTask;

        private async Task<(string Token, Guid AccountId)> RegisterAndCreateAccountAsync(AccountType type = AccountType.Chequing)
        {
            var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new RegisterUserDto
            {
                FullName = "Test User",
                Email = _faker.Internet.Email(),
                Password = "Password123!"
            });
            var cookies = registerResponse.Headers.GetValues("Set-Cookie").ToList();
            var token = cookies.First(c => c.Contains("accessToken")).Split(";")[0].Split("=")[1];
            _client.DefaultRequestHeaders.Remove("Cookie");
            _client.DefaultRequestHeaders.Add("Cookie", $"accessToken={token}");

            var accountResponse = await _client.PostAsJsonAsync("/api/account/create",
                new CreateAccountDto { Type = type });
            var account = await accountResponse.Content.ReadFromJsonAsync<AccountResponseDto>();

            return (token, account!.Id);
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
        public async Task Deposit_ValidRequest_Returns200()
        {
            var (_, accountId) = await RegisterAndCreateAccountAsync();

            var response = await _client.PostAsJsonAsync("/api/transaction/deposit",
                new DepositWithdrawDto { AccountId = accountId, Amount = 100 });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Deposit_ValidRequest_IncreasesBalanceInDb()
        {
            var (_, accountId) = await RegisterAndCreateAccountAsync();

            await _client.PostAsJsonAsync("/api/transaction/deposit",
                new DepositWithdrawDto { AccountId = accountId, Amount = 250 });

            var db = GetDb();
            var account = db.Accounts.FirstOrDefault(a => a.Id == accountId);
            account!.Balance.Should().Be(250);
        }

        [Fact]
        public async Task Deposit_ValidRequest_ReturnsCorrectStructure()
        {
            var (_, accountId) = await RegisterAndCreateAccountAsync();

            var response = await _client.PostAsJsonAsync("/api/transaction/deposit",
                new DepositWithdrawDto { AccountId = accountId, Amount = 100 });
            var data = await response.Content.ReadFromJsonAsync<TransactionResponseDto>();

            data.Should().NotBeNull();
            data!.Id.Should().NotBeEmpty();
            data.AccountId.Should().Be(accountId);
            data.Amount.Should().Be(100);
            data.Type.Should().Be("Deposit");
        }

        [Fact]
        public async Task Deposit_ValidRequest_TypeFieldIsDeposit()
        {
            var (_, accountId) = await RegisterAndCreateAccountAsync();

            var response = await _client.PostAsJsonAsync("/api/transaction/deposit",
                new DepositWithdrawDto { AccountId = accountId, Amount = 100 });
            var data = await response.Content.ReadFromJsonAsync<TransactionResponseDto>();

            data!.Type.Should().Be("Deposit");
        }

        [Fact]
        public async Task Deposit_ValidRequest_AmountMatchesRequest()
        {
            var (_, accountId) = await RegisterAndCreateAccountAsync();

            var response = await _client.PostAsJsonAsync("/api/transaction/deposit",
                new DepositWithdrawDto { AccountId = accountId, Amount = 500 });
            var data = await response.Content.ReadFromJsonAsync<TransactionResponseDto>();

            data!.Amount.Should().Be(500);
        }

        [Fact]
        public async Task Deposit_ValidRequest_AccountIdMatchesRequest()
        {
            var (_, accountId) = await RegisterAndCreateAccountAsync();

            var response = await _client.PostAsJsonAsync("/api/transaction/deposit",
                new DepositWithdrawDto { AccountId = accountId, Amount = 100 });
            var data = await response.Content.ReadFromJsonAsync<TransactionResponseDto>();

            data!.AccountId.Should().Be(accountId);
        }

        [Fact]
        public async Task Deposit_ValidRequest_CreatedAtIsRecent()
        {
            var (_, accountId) = await RegisterAndCreateAccountAsync();

            var response = await _client.PostAsJsonAsync("/api/transaction/deposit",
                new DepositWithdrawDto { AccountId = accountId, Amount = 100 });
            var data = await response.Content.ReadFromJsonAsync<TransactionResponseDto>();

            data!.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
        }

        [Fact]
        public async Task Deposit_MinimumAmount_Returns200()
        {
            var (_, accountId) = await RegisterAndCreateAccountAsync();

            var response = await _client.PostAsJsonAsync("/api/transaction/deposit",
                new DepositWithdrawDto { AccountId = accountId, Amount = 0.01m });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Deposit_MaximumAmount_Returns200()
        {
            var (_, accountId) = await RegisterAndCreateAccountAsync();

            var response = await _client.PostAsJsonAsync("/api/transaction/deposit",
                new DepositWithdrawDto { AccountId = accountId, Amount = 1000000 });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Deposit_ZeroAmount_Returns400()
        {
            var (_, accountId) = await RegisterAndCreateAccountAsync();

            var response = await _client.PostAsJsonAsync("/api/transaction/deposit",
                new DepositWithdrawDto { AccountId = accountId, Amount = 0 });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Deposit_NegativeAmount_Returns400()
        {
            var (_, accountId) = await RegisterAndCreateAccountAsync();

            var response = await _client.PostAsJsonAsync("/api/transaction/deposit",
                new DepositWithdrawDto { AccountId = accountId, Amount = -100 });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Deposit_ExceedsMaxAmount_Returns400()
        {
            var (_, accountId) = await RegisterAndCreateAccountAsync();

            var response = await _client.PostAsJsonAsync("/api/transaction/deposit",
                new DepositWithdrawDto { AccountId = accountId, Amount = 1000001 });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Deposit_DuplicateIdempotencyKey_ReturnsSameResult()
        {
            var (_, accountId) = await RegisterAndCreateAccountAsync();
            var key = Guid.NewGuid().ToString();
            var request1 = new HttpRequestMessage(HttpMethod.Post, "/api/transaction/deposit");
            request1.Headers.Add("Idempotency-Key", key);
            request1.Content = JsonContent.Create(new DepositWithdrawDto { AccountId = accountId, Amount = 100 });
            var response1 = await _client.SendAsync(request1);
            var data1 = await response1.Content.ReadFromJsonAsync<TransactionResponseDto>();

            var request2 = new HttpRequestMessage(HttpMethod.Post, "/api/transaction/deposit");
            request2.Headers.Add("Idempotency-Key", key);
            request2.Content = JsonContent.Create(new DepositWithdrawDto { AccountId = accountId, Amount = 100 });
            var response2 = await _client.SendAsync(request2);
            var data2 = await response2.Content.ReadFromJsonAsync<TransactionResponseDto>();

            data1!.Id.Should().Be(data2!.Id);
            var db = GetDb();
            db.Accounts.First(a => a.Id == accountId).Balance.Should().Be(100);
        }

        [Fact]
        public async Task Deposit_DifferentIdempotencyKeys_CreatesSeparateTransactions()
        {
            var (_, accountId) = await RegisterAndCreateAccountAsync();

            var request1 = new HttpRequestMessage(HttpMethod.Post, "/api/transaction/deposit");
            request1.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
            request1.Content = JsonContent.Create(new DepositWithdrawDto { AccountId = accountId, Amount = 100 });
            var response1 = await _client.SendAsync(request1);
            var data1 = await response1.Content.ReadFromJsonAsync<TransactionResponseDto>();

            var request2 = new HttpRequestMessage(HttpMethod.Post, "/api/transaction/deposit");
            request2.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
            request2.Content = JsonContent.Create(new DepositWithdrawDto { AccountId = accountId, Amount = 100 });
            var response2 = await _client.SendAsync(request2);
            var data2 = await response2.Content.ReadFromJsonAsync<TransactionResponseDto>();

            data1!.Id.Should().NotBe(data2!.Id);
        }

        [Fact]
        public async Task Deposit_IdempotencyKey_IsCaseSensitive()
        {
            var (_, accountId) = await RegisterAndCreateAccountAsync();
            var key = "TestKey123";

            var request1 = new HttpRequestMessage(HttpMethod.Post, "/api/transaction/deposit");
            request1.Headers.Add("Idempotency-Key", key);
            request1.Content = JsonContent.Create(new DepositWithdrawDto { AccountId = accountId, Amount = 100 });
            var response1 = await _client.SendAsync(request1);
            var data1 = await response1.Content.ReadFromJsonAsync<TransactionResponseDto>();

            var request2 = new HttpRequestMessage(HttpMethod.Post, "/api/transaction/deposit");
            request2.Headers.Add("Idempotency-Key", key.ToLower());
            request2.Content = JsonContent.Create(new DepositWithdrawDto { AccountId = accountId, Amount = 100 });
            var response2 = await _client.SendAsync(request2);
            var data2 = await response2.Content.ReadFromJsonAsync<TransactionResponseDto>();

            data1!.Id.Should().NotBe(data2!.Id);
        }

        [Fact]
        public async Task Deposit_Unauthenticated_Returns401()
        {
            _client.DefaultRequestHeaders.Remove("Cookie");

            var response = await _client.PostAsJsonAsync("/api/transaction/deposit",
                new DepositWithdrawDto { AccountId = Guid.NewGuid(), Amount = 100 });

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Deposit_CrossUserAccount_Returns401()
        {
            var (_, accountId) = await RegisterAndCreateAccountAsync();
            var (token2, _) = await RegisterAndCreateAccountAsync();
            AuthenticateClient(token2);

            var response = await _client.PostAsJsonAsync("/api/transaction/deposit",
                new DepositWithdrawDto { AccountId = accountId, Amount = 100 });

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Deposit_ThenGetTransactions_ShowsDeposit()
        {
            var (_, accountId) = await RegisterAndCreateAccountAsync();
            await _client.PostAsJsonAsync("/api/transaction/deposit",
                new DepositWithdrawDto { AccountId = accountId, Amount = 100 });

            var response = await _client.GetAsync($"/api/transaction/{accountId}");
            var data = await response.Content.ReadFromJsonAsync<PagedResultDto<TransactionResponseDto>>();

            data!.Items.Should().Contain(t => t.Type == "Deposit" && t.Amount == 100);
        }

        [Fact]
        public async Task Deposit_MultipleDeposits_AccumulatesCorrectly()
        {
            var (_, accountId) = await RegisterAndCreateAccountAsync();

            await _client.PostAsJsonAsync("/api/transaction/deposit",
                new DepositWithdrawDto { AccountId = accountId, Amount = 100 });
            await _client.PostAsJsonAsync("/api/transaction/deposit",
                new DepositWithdrawDto { AccountId = accountId, Amount = 100 });
            await _client.PostAsJsonAsync("/api/transaction/deposit",
                new DepositWithdrawDto { AccountId = accountId, Amount = 100 });

            var db = GetDb();
            db.Accounts.First(a => a.Id == accountId).Balance.Should().Be(300);
        }

        [Fact]
        public async Task Deposit_ResponseContainsNoSensitiveData()
        {
            var (_, accountId) = await RegisterAndCreateAccountAsync();

            var response = await _client.PostAsJsonAsync("/api/transaction/deposit",
                new DepositWithdrawDto { AccountId = accountId, Amount = 100 });
            var content = await response.Content.ReadAsStringAsync();

            content.Should().NotContain("passwordHash");
            content.Should().NotContain("PasswordHash");
        }

        [Fact]
        public async Task Withdraw_ValidRequest_Returns200()
        {
            var (_, accountId) = await RegisterAndCreateAccountAsync();
            await _client.PostAsJsonAsync("/api/transaction/deposit",
                new DepositWithdrawDto { AccountId = accountId, Amount = 500 });

            var response = await _client.PostAsJsonAsync("/api/transaction/withdraw",
                new DepositWithdrawDto { AccountId = accountId, Amount = 200 });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Withdraw_ValidRequest_DecreasesBalanceInDb()
        {
            var (_, accountId) = await RegisterAndCreateAccountAsync();
            await _client.PostAsJsonAsync("/api/transaction/deposit",
                new DepositWithdrawDto { AccountId = accountId, Amount = 500 });

            await _client.PostAsJsonAsync("/api/transaction/withdraw",
                new DepositWithdrawDto { AccountId = accountId, Amount = 200 });

            var db = GetDb();
            db.Accounts.First(a => a.Id == accountId).Balance.Should().Be(300);
        }

        [Fact]
        public async Task Withdraw_ValidRequest_ReturnsCorrectStructure()
        {
            var (_, accountId) = await RegisterAndCreateAccountAsync();
            await _client.PostAsJsonAsync("/api/transaction/deposit",
                new DepositWithdrawDto { AccountId = accountId, Amount = 500 });

            var response = await _client.PostAsJsonAsync("/api/transaction/withdraw",
                new DepositWithdrawDto { AccountId = accountId, Amount = 200 });
            var data = await response.Content.ReadFromJsonAsync<TransactionResponseDto>();

            data.Should().NotBeNull();
            data!.AccountId.Should().Be(accountId);
            data.Amount.Should().Be(200);
            data.Type.Should().Be("Withdrawal");
        }

        [Fact]
        public async Task Withdraw_ValidRequest_TypeFieldIsWithdrawal()
        {
            var (_, accountId) = await RegisterAndCreateAccountAsync();
            await _client.PostAsJsonAsync("/api/transaction/deposit",
                new DepositWithdrawDto { AccountId = accountId, Amount = 500 });

            var response = await _client.PostAsJsonAsync("/api/transaction/withdraw",
                new DepositWithdrawDto { AccountId = accountId, Amount = 200 });
            var data = await response.Content.ReadFromJsonAsync<TransactionResponseDto>();

            data!.Type.Should().Be("Withdrawal");
        }

        [Fact]
        public async Task Withdraw_ExactBalance_ReducesToZero()
        {
            var (_, accountId) = await RegisterAndCreateAccountAsync();
            await _client.PostAsJsonAsync("/api/transaction/deposit",
                new DepositWithdrawDto { AccountId = accountId, Amount = 500 });

            await _client.PostAsJsonAsync("/api/transaction/withdraw",
                new DepositWithdrawDto { AccountId = accountId, Amount = 500 });

            var db = GetDb();
            db.Accounts.First(a => a.Id == accountId).Balance.Should().Be(0);
        }

        [Fact]
        public async Task Withdraw_InsufficientFunds_Returns400()
        {
            var (_, accountId) = await RegisterAndCreateAccountAsync();
            await _client.PostAsJsonAsync("/api/transaction/deposit",
                new DepositWithdrawDto { AccountId = accountId, Amount = 100 });

            var response = await _client.PostAsJsonAsync("/api/transaction/withdraw",
                new DepositWithdrawDto { AccountId = accountId, Amount = 500 });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Withdraw_ZeroAmount_Returns400()
        {
            var (_, accountId) = await RegisterAndCreateAccountAsync();

            var response = await _client.PostAsJsonAsync("/api/transaction/withdraw",
                new DepositWithdrawDto { AccountId = accountId, Amount = 0 });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Withdraw_NegativeAmount_Returns400()
        {
            var (_, accountId) = await RegisterAndCreateAccountAsync();

            var response = await _client.PostAsJsonAsync("/api/transaction/withdraw",
                new DepositWithdrawDto { AccountId = accountId, Amount = -100 });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Withdraw_DuplicateIdempotencyKey_ReturnsSameResult()
        {
            var (_, accountId) = await RegisterAndCreateAccountAsync();
            await _client.PostAsJsonAsync("/api/transaction/deposit",
                new DepositWithdrawDto { AccountId = accountId, Amount = 500 });
            var key = Guid.NewGuid().ToString();

            var request1 = new HttpRequestMessage(HttpMethod.Post, "/api/transaction/withdraw");
            request1.Headers.Add("Idempotency-Key", key);
            request1.Content = JsonContent.Create(new DepositWithdrawDto { AccountId = accountId, Amount = 100 });
            var response1 = await _client.SendAsync(request1);
            var data1 = await response1.Content.ReadFromJsonAsync<TransactionResponseDto>();

            var request2 = new HttpRequestMessage(HttpMethod.Post, "/api/transaction/withdraw");
            request2.Headers.Add("Idempotency-Key", key);
            request2.Content = JsonContent.Create(new DepositWithdrawDto { AccountId = accountId, Amount = 100 });
            var response2 = await _client.SendAsync(request2);
            var data2 = await response2.Content.ReadFromJsonAsync<TransactionResponseDto>();

            data1!.Id.Should().Be(data2!.Id);
            var db = GetDb();
            db.Accounts.First(a => a.Id == accountId).Balance.Should().Be(400);
        }

        [Fact]
        public async Task Withdraw_Unauthenticated_Returns401()
        {
            _client.DefaultRequestHeaders.Remove("Cookie");

            var response = await _client.PostAsJsonAsync("/api/transaction/withdraw",
                new DepositWithdrawDto { AccountId = Guid.NewGuid(), Amount = 100 });

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Withdraw_CrossUserAccount_Returns401()
        {
            var (_, accountId) = await RegisterAndCreateAccountAsync();
            var (token2, _) = await RegisterAndCreateAccountAsync();
            AuthenticateClient(token2);

            var response = await _client.PostAsJsonAsync("/api/transaction/withdraw",
                new DepositWithdrawDto { AccountId = accountId, Amount = 100 });

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Withdraw_ThenGetTransactions_ShowsWithdrawal()
        {
            var (_, accountId) = await RegisterAndCreateAccountAsync();
            await _client.PostAsJsonAsync("/api/transaction/deposit",
                new DepositWithdrawDto { AccountId = accountId, Amount = 500 });
            await _client.PostAsJsonAsync("/api/transaction/withdraw",
                new DepositWithdrawDto { AccountId = accountId, Amount = 200 });

            var response = await _client.GetAsync($"/api/transaction/{accountId}");
            var data = await response.Content.ReadFromJsonAsync<PagedResultDto<TransactionResponseDto>>();

            data!.Items.Should().Contain(t => t.Type == "Withdrawal" && t.Amount == 200);
        }

        [Fact]
        public async Task Withdraw_ResponseContainsNoSensitiveData()
        {
            var (_, accountId) = await RegisterAndCreateAccountAsync();
            await _client.PostAsJsonAsync("/api/transaction/deposit",
                new DepositWithdrawDto { AccountId = accountId, Amount = 500 });

            var response = await _client.PostAsJsonAsync("/api/transaction/withdraw",
                new DepositWithdrawDto { AccountId = accountId, Amount = 100 });
            var content = await response.Content.ReadAsStringAsync();

            content.Should().NotContain("passwordHash");
            content.Should().NotContain("PasswordHash");
        }

        [Fact]
        public async Task Transfer_ValidRequest_Returns200()
        {
            var (_, fromAccountId) = await RegisterAndCreateAccountAsync();
            await _client.PostAsJsonAsync("/api/transaction/deposit",
                new DepositWithdrawDto { AccountId = fromAccountId, Amount = 500 });
            var toAccountResponse = await _client.PostAsJsonAsync("/api/account/create",
                new CreateAccountDto { Type = AccountType.Savings });
            var toAccount = await toAccountResponse.Content.ReadFromJsonAsync<AccountResponseDto>();

            var response = await _client.PostAsJsonAsync("/api/transaction/transfer",
                new TransferDto { FromAccountId = fromAccountId, ToAccountId = toAccount!.Id, Amount = 200 });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Transfer_ValidRequest_BothBalancesCorrectInDb()
        {
            var (_, fromAccountId) = await RegisterAndCreateAccountAsync();
            await _client.PostAsJsonAsync("/api/transaction/deposit",
                new DepositWithdrawDto { AccountId = fromAccountId, Amount = 500 });
            var toAccountResponse = await _client.PostAsJsonAsync("/api/account/create",
                new CreateAccountDto { Type = AccountType.Savings });
            var toAccount = await toAccountResponse.Content.ReadFromJsonAsync<AccountResponseDto>();

            await _client.PostAsJsonAsync("/api/transaction/transfer",
                new TransferDto { FromAccountId = fromAccountId, ToAccountId = toAccount!.Id, Amount = 200 });

            var db = GetDb();
            db.Accounts.First(a => a.Id == fromAccountId).Balance.Should().Be(300);
            db.Accounts.First(a => a.Id == toAccount.Id).Balance.Should().Be(200);
        }

        [Fact]
        public async Task Transfer_ValidRequest_ReturnsCorrectMessage()
        {
            var (_, fromAccountId) = await RegisterAndCreateAccountAsync();
            await _client.PostAsJsonAsync("/api/transaction/deposit",
                new DepositWithdrawDto { AccountId = fromAccountId, Amount = 500 });
            var toAccountResponse = await _client.PostAsJsonAsync("/api/account/create",
                new CreateAccountDto { Type = AccountType.Savings });
            var toAccount = await toAccountResponse.Content.ReadFromJsonAsync<AccountResponseDto>();

            var response = await _client.PostAsJsonAsync("/api/transaction/transfer",
                new TransferDto { FromAccountId = fromAccountId, ToAccountId = toAccount!.Id, Amount = 200 });
            var content = await response.Content.ReadAsStringAsync();

            content.Should().Contain("Transfer successful");
        }

        [Fact]
        public async Task Transfer_ExactBalance_ReducesToZero()
        {
            var (_, fromAccountId) = await RegisterAndCreateAccountAsync();
            await _client.PostAsJsonAsync("/api/transaction/deposit",
                new DepositWithdrawDto { AccountId = fromAccountId, Amount = 500 });
            var toAccountResponse = await _client.PostAsJsonAsync("/api/account/create",
                new CreateAccountDto { Type = AccountType.Savings });
            var toAccount = await toAccountResponse.Content.ReadFromJsonAsync<AccountResponseDto>();

            await _client.PostAsJsonAsync("/api/transaction/transfer",
                new TransferDto { FromAccountId = fromAccountId, ToAccountId = toAccount!.Id, Amount = 500 });

            var db = GetDb();
            db.Accounts.First(a => a.Id == fromAccountId).Balance.Should().Be(0);
        }

        [Fact]
        public async Task Transfer_InsufficientFunds_Returns400()
        {
            var (_, fromAccountId) = await RegisterAndCreateAccountAsync();
            await _client.PostAsJsonAsync("/api/transaction/deposit",
                new DepositWithdrawDto { AccountId = fromAccountId, Amount = 100 });
            var toAccountResponse = await _client.PostAsJsonAsync("/api/account/create",
                new CreateAccountDto { Type = AccountType.Savings });
            var toAccount = await toAccountResponse.Content.ReadFromJsonAsync<AccountResponseDto>();

            var response = await _client.PostAsJsonAsync("/api/transaction/transfer",
                new TransferDto { FromAccountId = fromAccountId, ToAccountId = toAccount!.Id, Amount = 500 });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Transfer_SameAccount_Returns400()
        {
            var (_, accountId) = await RegisterAndCreateAccountAsync();
            await _client.PostAsJsonAsync("/api/transaction/deposit",
                new DepositWithdrawDto { AccountId = accountId, Amount = 500 });

            var response = await _client.PostAsJsonAsync("/api/transaction/transfer",
                new TransferDto { FromAccountId = accountId, ToAccountId = accountId, Amount = 100 });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Transfer_ToAccountNotFound_Returns400()
        {
            var (_, fromAccountId) = await RegisterAndCreateAccountAsync();
            await _client.PostAsJsonAsync("/api/transaction/deposit",
                new DepositWithdrawDto { AccountId = fromAccountId, Amount = 500 });

            var response = await _client.PostAsJsonAsync("/api/transaction/transfer",
                new TransferDto { FromAccountId = fromAccountId, ToAccountId = Guid.NewGuid(), Amount = 100 });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Transfer_DuplicateIdempotencyKey_NoDoubleCharge()
        {
            var (_, fromAccountId) = await RegisterAndCreateAccountAsync();
            await _client.PostAsJsonAsync("/api/transaction/deposit",
                new DepositWithdrawDto { AccountId = fromAccountId, Amount = 500 });
            var toAccountResponse = await _client.PostAsJsonAsync("/api/account/create",
                new CreateAccountDto { Type = AccountType.Savings });
            var toAccount = await toAccountResponse.Content.ReadFromJsonAsync<AccountResponseDto>();
            var key = Guid.NewGuid().ToString();

            var request1 = new HttpRequestMessage(HttpMethod.Post, "/api/transaction/transfer");
            request1.Headers.Add("Idempotency-Key", key);
            request1.Content = JsonContent.Create(new TransferDto { FromAccountId = fromAccountId, ToAccountId = toAccount!.Id, Amount = 100 });
            await _client.SendAsync(request1);

            var request2 = new HttpRequestMessage(HttpMethod.Post, "/api/transaction/transfer");
            request2.Headers.Add("Idempotency-Key", key);
            request2.Content = JsonContent.Create(new TransferDto { FromAccountId = fromAccountId, ToAccountId = toAccount.Id, Amount = 100 });
            await _client.SendAsync(request2);

            var db = GetDb();
            db.Accounts.First(a => a.Id == fromAccountId).Balance.Should().Be(400);
        }

        [Fact]
        public async Task Transfer_Unauthenticated_Returns401()
        {
            _client.DefaultRequestHeaders.Remove("Cookie");

            var response = await _client.PostAsJsonAsync("/api/transaction/transfer",
                new TransferDto { FromAccountId = Guid.NewGuid(), ToAccountId = Guid.NewGuid(), Amount = 100 });

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Transfer_CrossUserFromAccount_Returns401()
        {
            var (_, fromAccountId) = await RegisterAndCreateAccountAsync();
            var (token2, toAccountId) = await RegisterAndCreateAccountAsync();
            AuthenticateClient(token2);

            var response = await _client.PostAsJsonAsync("/api/transaction/transfer",
                new TransferDto { FromAccountId = fromAccountId, ToAccountId = toAccountId, Amount = 100 });

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Transfer_ThenGetTransactions_ShowsDebitOnFromAccount()
        {
            var (_, fromAccountId) = await RegisterAndCreateAccountAsync();
            await _client.PostAsJsonAsync("/api/transaction/deposit",
                new DepositWithdrawDto { AccountId = fromAccountId, Amount = 500 });
            var toAccountResponse = await _client.PostAsJsonAsync("/api/account/create",
                new CreateAccountDto { Type = AccountType.Savings });
            var toAccount = await toAccountResponse.Content.ReadFromJsonAsync<AccountResponseDto>();
            await _client.PostAsJsonAsync("/api/transaction/transfer",
                new TransferDto { FromAccountId = fromAccountId, ToAccountId = toAccount!.Id, Amount = 200 });

            var response = await _client.GetAsync($"/api/transaction/{fromAccountId}");
            var data = await response.Content.ReadFromJsonAsync<PagedResultDto<TransactionResponseDto>>();

            data!.Items.Should().Contain(t => t.Amount == 200);
        }

        [Fact]
        public async Task Transfer_ThenGetTransactions_ShowsCreditOnToAccount()
        {
            var (_, fromAccountId) = await RegisterAndCreateAccountAsync();
            await _client.PostAsJsonAsync("/api/transaction/deposit",
                new DepositWithdrawDto { AccountId = fromAccountId, Amount = 500 });
            var toAccountResponse = await _client.PostAsJsonAsync("/api/account/create",
                new CreateAccountDto { Type = AccountType.Savings });
            var toAccount = await toAccountResponse.Content.ReadFromJsonAsync<AccountResponseDto>();
            await _client.PostAsJsonAsync("/api/transaction/transfer",
                new TransferDto { FromAccountId = fromAccountId, ToAccountId = toAccount!.Id, Amount = 200 });

            var response = await _client.GetAsync($"/api/transaction/{toAccount.Id}");
            var data = await response.Content.ReadFromJsonAsync<PagedResultDto<TransactionResponseDto>>();

            data!.Items.Should().Contain(t => t.Amount == 200);
        }

        [Fact]
        public async Task ExternalTransfer_ValidRequest_Returns200()
        {
            var (token1, fromAccountId) = await RegisterAndCreateAccountAsync();
            await _client.PostAsJsonAsync("/api/transaction/deposit",
                new DepositWithdrawDto { AccountId = fromAccountId, Amount = 500 });

            var db = GetDb();

            var (_, toAccountId) = await RegisterAndCreateAccountAsync();
            var toAccount = db.Accounts.First(a => a.Id == toAccountId);

            AuthenticateClient(token1);
            var response = await _client.PostAsJsonAsync("/api/transaction/transferexternal",
                new ExternalTransferDto { FromAccountId = fromAccountId, ToAccountNumber = toAccount.AccountNumber, Amount = 200 });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task ExternalTransfer_ValidRequest_BothBalancesCorrectInDb()
        {
            var (token1, fromAccountId) = await RegisterAndCreateAccountAsync();
            await _client.PostAsJsonAsync("/api/transaction/deposit",
                new DepositWithdrawDto { AccountId = fromAccountId, Amount = 500 });

            var db = GetDb();
            var (_, toAccountId) = await RegisterAndCreateAccountAsync();
            var toAccount = db.Accounts.First(a => a.Id == toAccountId);

            AuthenticateClient(token1);
            await _client.PostAsJsonAsync("/api/transaction/transferexternal",
                new ExternalTransferDto { FromAccountId = fromAccountId, ToAccountNumber = toAccount.AccountNumber, Amount = 200 });

            db = GetDb();
            db.Accounts.First(a => a.Id == fromAccountId).Balance.Should().Be(300);
            db.Accounts.First(a => a.Id == toAccountId).Balance.Should().Be(200);
        }

        [Fact]
        public async Task ExternalTransfer_ValidRequest_ReturnsCorrectMessage()
        {
            var (token1, fromAccountId) = await RegisterAndCreateAccountAsync();
            await _client.PostAsJsonAsync("/api/transaction/deposit",
                new DepositWithdrawDto { AccountId = fromAccountId, Amount = 500 });

            var db = GetDb();
            var (_, toAccountId) = await RegisterAndCreateAccountAsync();
            var toAccount = db.Accounts.First(a => a.Id == toAccountId);

            AuthenticateClient(token1);
            var response = await _client.PostAsJsonAsync("/api/transaction/transferexternal",
                new ExternalTransferDto { FromAccountId = fromAccountId, ToAccountNumber = toAccount.AccountNumber, Amount = 200 });
            var content = await response.Content.ReadAsStringAsync();

            content.Should().Contain("Transfer successful");
        }

        [Fact]
        public async Task ExternalTransfer_ExactBalance_ReducesToZero()
        {
            var (token1, fromAccountId) = await RegisterAndCreateAccountAsync();
            await _client.PostAsJsonAsync("/api/transaction/deposit",
                new DepositWithdrawDto { AccountId = fromAccountId, Amount = 500 });

            var db = GetDb();
            var (_, toAccountId) = await RegisterAndCreateAccountAsync();
            var toAccount = db.Accounts.First(a => a.Id == toAccountId);

            AuthenticateClient(token1);
            await _client.PostAsJsonAsync("/api/transaction/transferexternal",
                new ExternalTransferDto { FromAccountId = fromAccountId, ToAccountNumber = toAccount.AccountNumber, Amount = 500 });

            db = GetDb();
            db.Accounts.First(a => a.Id == fromAccountId).Balance.Should().Be(0);
        }

        [Fact]
        public async Task ExternalTransfer_InsufficientFunds_Returns400()
        {
            var (token1, fromAccountId) = await RegisterAndCreateAccountAsync();
            await _client.PostAsJsonAsync("/api/transaction/deposit",
                new DepositWithdrawDto { AccountId = fromAccountId, Amount = 100 });

            var db = GetDb();
            var (_, toAccountId) = await RegisterAndCreateAccountAsync();
            var toAccount = db.Accounts.First(a => a.Id == toAccountId);

            AuthenticateClient(token1);
            var response = await _client.PostAsJsonAsync("/api/transaction/transferexternal",
                new ExternalTransferDto { FromAccountId = fromAccountId, ToAccountNumber = toAccount.AccountNumber, Amount = 500 });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task ExternalTransfer_SameAccount_Returns400()
        {
            var (_, fromAccountId) = await RegisterAndCreateAccountAsync();
            await _client.PostAsJsonAsync("/api/transaction/deposit",
                new DepositWithdrawDto { AccountId = fromAccountId, Amount = 500 });

            var db = GetDb();
            var fromAccount = db.Accounts.First(a => a.Id == fromAccountId);

            var response = await _client.PostAsJsonAsync("/api/transaction/transferexternal",
                new ExternalTransferDto { FromAccountId = fromAccountId, ToAccountNumber = fromAccount.AccountNumber, Amount = 100 });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task ExternalTransfer_ToAccountNotFound_Returns400()
        {
            var (_, fromAccountId) = await RegisterAndCreateAccountAsync();
            await _client.PostAsJsonAsync("/api/transaction/deposit",
                new DepositWithdrawDto { AccountId = fromAccountId, Amount = 500 });

            var response = await _client.PostAsJsonAsync("/api/transaction/transferexternal",
                new ExternalTransferDto { FromAccountId = fromAccountId, ToAccountNumber = "NOTEXIST1234", Amount = 100 });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task ExternalTransfer_DuplicateIdempotencyKey_NoDoubleCharge()
        {
            var (token1, fromAccountId) = await RegisterAndCreateAccountAsync();
            await _client.PostAsJsonAsync("/api/transaction/deposit",
                new DepositWithdrawDto { AccountId = fromAccountId, Amount = 500 });

            var db = GetDb();
            var (_, toAccountId) = await RegisterAndCreateAccountAsync();
            var toAccount = db.Accounts.First(a => a.Id == toAccountId);
            var key = Guid.NewGuid().ToString();

            AuthenticateClient(token1);
            var request1 = new HttpRequestMessage(HttpMethod.Post, "/api/transaction/transferexternal");
            request1.Headers.Add("Idempotency-Key", key);
            request1.Content = JsonContent.Create(new ExternalTransferDto { FromAccountId = fromAccountId, ToAccountNumber = toAccount.AccountNumber, Amount = 100 });
            await _client.SendAsync(request1);

            var request2 = new HttpRequestMessage(HttpMethod.Post, "/api/transaction/transferexternal");
            request2.Headers.Add("Idempotency-Key", key);
            request2.Content = JsonContent.Create(new ExternalTransferDto { FromAccountId = fromAccountId, ToAccountNumber = toAccount.AccountNumber, Amount = 100 });
            await _client.SendAsync(request2);

            db = GetDb();
            db.Accounts.First(a => a.Id == fromAccountId).Balance.Should().Be(400);
        }

        [Fact]
        public async Task ExternalTransfer_Unauthenticated_Returns401()
        {
            _client.DefaultRequestHeaders.Remove("Cookie");

            var response = await _client.PostAsJsonAsync("/api/transaction/transferexternal",
                new ExternalTransferDto { FromAccountId = Guid.NewGuid(), ToAccountNumber = "ACC123456789", Amount = 100 });

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task ExternalTransfer_CrossUserFromAccount_Returns401()
        {
            var (_, fromAccountId) = await RegisterAndCreateAccountAsync();
            var (token2, _) = await RegisterAndCreateAccountAsync();
            AuthenticateClient(token2);

            var response = await _client.PostAsJsonAsync("/api/transaction/transferexternal",
                new ExternalTransferDto { FromAccountId = fromAccountId, ToAccountNumber = "ACC123456789", Amount = 100 });

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetTransactions_ValidRequest_Returns200()
        {
            var (_, accountId) = await RegisterAndCreateAccountAsync();

            var response = await _client.GetAsync($"/api/transaction/{accountId}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetTransactions_EmptyAccount_ReturnsEmptyListWithZeroCount()
        {
            var (_, accountId) = await RegisterAndCreateAccountAsync();

            var response = await _client.GetAsync($"/api/transaction/{accountId}");
            var data = await response.Content.ReadFromJsonAsync<PagedResultDto<TransactionResponseDto>>();

            data!.Items.Should().BeEmpty();
            data.TotalCount.Should().Be(0);
        }

        [Fact]
        public async Task GetTransactions_ValidRequest_ReturnsCorrectPaginationMetadata()
        {
            var (_, accountId) = await RegisterAndCreateAccountAsync();
            await _client.PostAsJsonAsync("/api/transaction/deposit",
                new DepositWithdrawDto { AccountId = accountId, Amount = 100 });

            var response = await _client.GetAsync($"/api/transaction/{accountId}?page=1&pageSize=10");
            var data = await response.Content.ReadFromJsonAsync<PagedResultDto<TransactionResponseDto>>();

            data!.Page.Should().Be(1);
            data.PageSize.Should().Be(10);
            data.TotalCount.Should().Be(1);
        }

        [Fact]
        public async Task GetTransactions_DefaultPageAndPageSize_AreCorrect()
        {
            var (_, accountId) = await RegisterAndCreateAccountAsync();

            var response = await _client.GetAsync($"/api/transaction/{accountId}");
            var data = await response.Content.ReadFromJsonAsync<PagedResultDto<TransactionResponseDto>>();

            data!.Page.Should().Be(1);
            data.PageSize.Should().Be(10);
        }

        [Fact]
        public async Task GetTransactions_PageSizeOver100_ClampedTo100()
        {
            var (_, accountId) = await RegisterAndCreateAccountAsync();

            var response = await _client.GetAsync($"/api/transaction/{accountId}?pageSize=200");
            var data = await response.Content.ReadFromJsonAsync<PagedResultDto<TransactionResponseDto>>();

            data!.PageSize.Should().Be(100);
        }

        [Fact]
        public async Task GetTransactions_PageUnder1_ClampedTo1()
        {
            var (_, accountId) = await RegisterAndCreateAccountAsync();

            var response = await _client.GetAsync($"/api/transaction/{accountId}?page=0");
            var data = await response.Content.ReadFromJsonAsync<PagedResultDto<TransactionResponseDto>>();

            data!.Page.Should().Be(1);
        }

        [Fact]
        public async Task GetTransactions_TypeFilterDeposit_ReturnsOnlyDeposits()
        {
            var (_, accountId) = await RegisterAndCreateAccountAsync();
            await _client.PostAsJsonAsync("/api/transaction/deposit",
                new DepositWithdrawDto { AccountId = accountId, Amount = 500 });
            await _client.PostAsJsonAsync("/api/transaction/withdraw",
                new DepositWithdrawDto { AccountId = accountId, Amount = 100 });

            var response = await _client.GetAsync($"/api/transaction/{accountId}?type=Deposit");
            var data = await response.Content.ReadFromJsonAsync<PagedResultDto<TransactionResponseDto>>();

            data!.Items.Should().AllSatisfy(t => t.Type.Should().Be("Deposit"));
        }

        [Fact]
        public async Task GetTransactions_TypeFilterWithdrawal_ReturnsOnlyWithdrawals()
        {
            var (_, accountId) = await RegisterAndCreateAccountAsync();
            await _client.PostAsJsonAsync("/api/transaction/deposit",
                new DepositWithdrawDto { AccountId = accountId, Amount = 500 });
            await _client.PostAsJsonAsync("/api/transaction/withdraw",
                new DepositWithdrawDto { AccountId = accountId, Amount = 100 });

            var response = await _client.GetAsync($"/api/transaction/{accountId}?type=Withdrawal");
            var data = await response.Content.ReadFromJsonAsync<PagedResultDto<TransactionResponseDto>>();

            data!.Items.Should().AllSatisfy(t => t.Type.Should().Be("Withdrawal"));
        }

        [Fact]
        public async Task GetTransactions_Unauthenticated_Returns401()
        {
            _client.DefaultRequestHeaders.Remove("Cookie");

            var response = await _client.GetAsync($"/api/transaction/{Guid.NewGuid()}");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetTransactions_CrossUserAccount_Returns401()
        {
            var (_, accountId) = await RegisterAndCreateAccountAsync();
            var (token2, _) = await RegisterAndCreateAccountAsync();
            AuthenticateClient(token2);

            var response = await _client.GetAsync($"/api/transaction/{accountId}");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetTransactionsByDateRange_ValidRequest_Returns200()
        {
            var (_, accountId) = await RegisterAndCreateAccountAsync();
            await _client.PostAsJsonAsync("/api/transaction/deposit",
                new DepositWithdrawDto { AccountId = accountId, Amount = 100 });

            var start = DateTime.UtcNow.AddDays(-1).ToString("o");
            var end = DateTime.UtcNow.AddDays(1).ToString("o");
            var response = await _client.GetAsync($"/api/transaction/{accountId}/range?startDate={start}&endDate={end}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetTransactionsByDateRange_ValidRequest_ReturnsTransactionsWithinRangeOnly()
        {
            var (_, accountId) = await RegisterAndCreateAccountAsync();
            await _client.PostAsJsonAsync("/api/transaction/deposit",
                new DepositWithdrawDto { AccountId = accountId, Amount = 100 });

            var start = DateTime.UtcNow.AddDays(-1).ToString("o");
            var end = DateTime.UtcNow.AddDays(1).ToString("o");
            var response = await _client.GetAsync($"/api/transaction/{accountId}/range?startDate={start}&endDate={end}");
            var data = await response.Content.ReadFromJsonAsync<List<TransactionResponseDto>>();

            data!.Should().NotBeEmpty();
            data.Should().AllSatisfy(t => t.CreatedAt.Should().BeAfter(DateTime.UtcNow.AddDays(-1)));
        }

        [Fact]
        public async Task GetTransactionsByDateRange_EmptyRange_ReturnsEmptyList()
        {
            var (_, accountId) = await RegisterAndCreateAccountAsync();

            var start = DateTime.UtcNow.AddDays(-10).ToString("o");
            var end = DateTime.UtcNow.AddDays(-5).ToString("o");
            var response = await _client.GetAsync($"/api/transaction/{accountId}/range?startDate={start}&endDate={end}");
            var data = await response.Content.ReadFromJsonAsync<List<TransactionResponseDto>>();

            data!.Should().BeEmpty();
        }

        [Fact]
        public async Task GetTransactionsByDateRange_EndBeforeStart_Returns400()
        {
            var (_, accountId) = await RegisterAndCreateAccountAsync();

            var start = DateTime.UtcNow.AddDays(1).ToString("o");
            var end = DateTime.UtcNow.AddDays(-1).ToString("o");
            var response = await _client.GetAsync($"/api/transaction/{accountId}/range?startDate={start}&endDate={end}");

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task GetTransactionsByDateRange_ExceedsMaxDays_Returns400()
        {
            var (_, accountId) = await RegisterAndCreateAccountAsync();

            var start = DateTime.UtcNow.AddDays(-400).ToString("o");
            var end = DateTime.UtcNow.ToString("o");
            var response = await _client.GetAsync($"/api/transaction/{accountId}/range?startDate={start}&endDate={end}");

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task GetTransactionsByDateRange_Unauthenticated_Returns401()
        {
            _client.DefaultRequestHeaders.Remove("Cookie");

            var start = DateTime.UtcNow.AddDays(-1).ToString("o");
            var end = DateTime.UtcNow.AddDays(1).ToString("o");
            var response = await _client.GetAsync($"/api/transaction/{Guid.NewGuid()}/range?startDate={start}&endDate={end}");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetTransactionsByDateRange_CrossUserAccount_Returns401()
        {
            var (_, accountId) = await RegisterAndCreateAccountAsync();
            var (token2, _) = await RegisterAndCreateAccountAsync();
            AuthenticateClient(token2);

            var start = DateTime.UtcNow.AddDays(-1).ToString("o");
            var end = DateTime.UtcNow.AddDays(1).ToString("o");
            var response = await _client.GetAsync($"/api/transaction/{accountId}/range?startDate={start}&endDate={end}");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}