using BankLite.Application.DTOs;
using BankLite.Application.Exceptions;
using BankLite.Application.Services;
using BankLite.Domain.Entities;
using BankLite.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BankLite.Tests.Services;

public class AccountServiceTests
{
    private readonly AccountService _accountService;
    private readonly Mock<IAccountRepository> _mockAccountRepo;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;

    public AccountServiceTests()
    {
        _mockAccountRepo = new Mock<IAccountRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockAccountRepo.Setup(r => r.ExistsByAccountNumberAsync(It.IsAny<string>())).ReturnsAsync(false);
        _accountService = new AccountService(_mockAccountRepo.Object, _mockUnitOfWork.Object,
            new NullLogger<AccountService>());
    }

    [Theory]
    [InlineData(AccountType.Chequing, "Chequing")]
    [InlineData(AccountType.Savings, "Savings")]
    public async Task CreateAccountAsync_NoExistingAccounts_ReturnsCorrectAccountType(AccountType accountType,
        string expectedType)
    {
        var userId = Guid.NewGuid();
        _mockAccountRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(new List<Account>());
        var dto = new CreateAccountDto { Type = accountType };

        var result = await _accountService.CreateAccountAsync(dto, userId);

        result.Should().NotBeNull();
        result.Type.Should().Be(expectedType);
        _mockAccountRepo.Verify(r => r.AddAsync(It.IsAny<Account>()), Times.Once);
    }

    [Fact]
    public async Task CreateAccountAsync_NoExistingAccounts_GeneratesAccountNumber()
    {
        var userId = Guid.NewGuid();
        _mockAccountRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(new List<Account>());
        var dto = new CreateAccountDto { Type = AccountType.Chequing };

        var result = await _accountService.CreateAccountAsync(dto, userId);

        result.AccountNumber.Should().NotBeNullOrEmpty();
        result.AccountNumber.Length.Should().Be(12);
    }

    [Fact]
    public async Task CreateAccountAsync_NoExistingAccounts_GeneratesUpperCaseAccountNumber()
    {
        var userId = Guid.NewGuid();
        _mockAccountRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(new List<Account>());
        var dto = new CreateAccountDto { Type = AccountType.Chequing };

        var result = await _accountService.CreateAccountAsync(dto, userId);

        result.AccountNumber.Should().Be(result.AccountNumber.ToUpper());
    }

    [Fact]
    public async Task CreateAccountAsync_TwoAccountsCreated_GeneratesUniqueAccountNumbers()
    {
        var userId = Guid.NewGuid();
        _mockAccountRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(new List<Account>());
        var dto1 = new CreateAccountDto { Type = AccountType.Chequing };

        var result1 = await _accountService.CreateAccountAsync(dto1, userId);

        _mockAccountRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(new List<Account>
        {
            new() { Id = Guid.NewGuid(), UserId = userId, Type = AccountType.Chequing, AccountNumber = "ACC001" }
        });
        var dto2 = new CreateAccountDto { Type = AccountType.Savings };

        var result2 = await _accountService.CreateAccountAsync(dto2, userId);

        result1.AccountNumber.Should().NotBe(result2.AccountNumber);
    }

    [Fact]
    public async Task CreateAccountAsync_ValidRequest_CallsAddAsyncOnce()
    {
        var userId = Guid.NewGuid();
        _mockAccountRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(new List<Account>());
        var dto = new CreateAccountDto { Type = AccountType.Chequing };

        await _accountService.CreateAccountAsync(dto, userId);

        _mockAccountRepo.Verify(r => r.AddAsync(It.IsAny<Account>()), Times.Once);
    }

    [Fact]
    public async Task CreateAccountAsync_ValidRequest_CallsSaveAsyncOnce()
    {
        var userId = Guid.NewGuid();
        _mockAccountRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(new List<Account>());
        var dto = new CreateAccountDto { Type = AccountType.Chequing };

        await _accountService.CreateAccountAsync(dto, userId);

        _mockUnitOfWork.Verify(r => r.SaveAsync(), Times.Once);
    }

    [Theory]
    [InlineData(AccountType.Savings, "Savings")]
    [InlineData(AccountType.Chequing, "Chequing")]
    public async Task CreateAccountAsync_OppositeTypeExists_ReturnsNewAccountType(AccountType newType,
        string expectedType)
    {
        var userId = Guid.NewGuid();
        _mockAccountRepo.Setup(r => r.ExistsByUserIdAndTypeAsync(userId, newType)).ReturnsAsync(false);
        var dto = new CreateAccountDto { Type = newType };

        var result = await _accountService.CreateAccountAsync(dto, userId);

        result.Type.Should().Be(expectedType);
        _mockAccountRepo.Verify(r => r.AddAsync(It.IsAny<Account>()), Times.Once);
    }

    [Theory]
    [InlineData(AccountType.Chequing)]
    [InlineData(AccountType.Savings)]
    public async Task CreateAccountAsync_SameTypeAlreadyExists_ThrowsBadRequestException(AccountType accountType)
    {
        var userId = Guid.NewGuid();
        _mockAccountRepo.Setup(r => r.ExistsByUserIdAndTypeAsync(userId, accountType)).ReturnsAsync(true);
        var dto = new CreateAccountDto { Type = accountType };

        var act = async () => await _accountService.CreateAccountAsync(dto, userId);

        await act.Should().ThrowAsync<BadRequestException>();
        _mockAccountRepo.Verify(r => r.AddAsync(It.IsAny<Account>()), Times.Never);
    }

    [Theory]
    [InlineData(AccountType.Chequing, "Chequing")]
    [InlineData(AccountType.Savings, "Savings")]
    public async Task CreateAccountAsync_SameTypeAlreadyExists_ThrowsWithCorrectMessage(AccountType accountType,
        string expectedMessage)
    {
        var userId = Guid.NewGuid();
        _mockAccountRepo.Setup(r => r.ExistsByUserIdAndTypeAsync(userId, accountType)).ReturnsAsync(true);
        var dto = new CreateAccountDto { Type = accountType };

        var act = async () => await _accountService.CreateAccountAsync(dto, userId);

        var ex = await act.Should().ThrowAsync<BadRequestException>();
        ex.Which.Message.Should().Contain(expectedMessage);
    }

    [Fact]
    public async Task CreateAccountAsync_ValidRequest_ChecksExistingAccountTypeOnce()
    {
        var userId = Guid.NewGuid();
        var dto = new CreateAccountDto { Type = AccountType.Chequing };

        await _accountService.CreateAccountAsync(dto, userId);

        _mockAccountRepo.Verify(r => r.ExistsByUserIdAndTypeAsync(userId, AccountType.Chequing), Times.Once);
    }

    [Fact]
    public async Task GetAccountsByUserIdAsync_TwoAccountsExist_ReturnsBothAccounts()
    {
        var userId = Guid.NewGuid();
        var accounts = new List<Account>
        {
            new() { Id = Guid.NewGuid(), UserId = userId, Type = AccountType.Chequing, AccountNumber = "ACC001" },
            new() { Id = Guid.NewGuid(), UserId = userId, Type = AccountType.Savings, AccountNumber = "ACC002" }
        };
        _mockAccountRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(accounts);

        var result = await _accountService.GetAccountsByUserIdAsync(userId);

        var accountResponseDtos = result as AccountResponseDto[] ?? result.ToArray();
        accountResponseDtos.Should().NotBeNull();
        accountResponseDtos.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAccountsByUserIdAsync_NoAccountsExist_ReturnsEmptyList()
    {
        var userId = Guid.NewGuid();
        _mockAccountRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(new List<Account>());

        var result = await _accountService.GetAccountsByUserIdAsync(userId);

        var accountResponseDtos = result as AccountResponseDto[] ?? result.ToArray();
        accountResponseDtos.Should().NotBeNull();
        accountResponseDtos.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAccountsByUserIdAsync_ValidRequest_CallsRepositoryOnce()
    {
        var userId = Guid.NewGuid();
        _mockAccountRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(new List<Account>());

        await _accountService.GetAccountsByUserIdAsync(userId);

        _mockAccountRepo.Verify(r => r.GetByUserIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetAccountsByUserIdAsync_TwoAccountsExist_ReturnsBothCorrectTypes()
    {
        var userId = Guid.NewGuid();
        var accounts = new List<Account>
        {
            new() { Id = Guid.NewGuid(), UserId = userId, Type = AccountType.Chequing, AccountNumber = "ACC001" },
            new() { Id = Guid.NewGuid(), UserId = userId, Type = AccountType.Savings, AccountNumber = "ACC002" }
        };
        _mockAccountRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(accounts);

        var result = await _accountService.GetAccountsByUserIdAsync(userId);

        var accountResponseDtos = result as AccountResponseDto[] ?? result.ToArray();
        if (accountResponseDtos == null) throw new ArgumentNullException(nameof(accountResponseDtos));
        accountResponseDtos.Should().Contain(a => a.Type == "Chequing");
        accountResponseDtos.Should().Contain(a => a.Type == "Savings");
    }

    [Fact]
    public async Task GetAccountsByUserIdAsync_OneAccountExists_ReturnsSingleAccount()
    {
        var userId = Guid.NewGuid();
        var accounts = new List<Account>
        {
            new() { Id = Guid.NewGuid(), UserId = userId, Type = AccountType.Chequing, AccountNumber = "ACC001" }
        };
        _mockAccountRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(accounts);

        var result = await _accountService.GetAccountsByUserIdAsync(userId);

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetAccountsByUserIdAsync_ValidRequest_NeverCallsAddAsync()
    {
        var userId = Guid.NewGuid();
        _mockAccountRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(new List<Account>());

        await _accountService.GetAccountsByUserIdAsync(userId);

        _mockAccountRepo.Verify(r => r.AddAsync(It.IsAny<Account>()), Times.Never);
    }
}
