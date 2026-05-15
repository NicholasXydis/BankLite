using BankLite.Application.DTOs;
using BankLite.Application.Services;
using BankLite.Domain.Entities;
using BankLite.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BankLite.Tests.Services
{
    public class AccountServiceTests
    {
        private readonly Mock<IAccountRepository> _mockAccountRepo;
        private readonly AccountService _accountService;

        public AccountServiceTests()
        {
            _mockAccountRepo = new Mock<IAccountRepository>();
            _accountService = new AccountService(_mockAccountRepo.Object, new NullLogger<AccountService>());
        }

        [Fact]
        public async Task CreateAccountAsync_ShouldCreateAccount_WhenNoExistingAccounts()
        {
            var userId = Guid.NewGuid();
            _mockAccountRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(new List<Account>());
            var dto = new CreateAccountDto { Type = AccountType.Chequing };

            var result = await _accountService.CreateAccountAsync(dto, userId);

            Assert.NotNull(result);
            Assert.Equal(userId, result.UserId);
            Assert.Equal(AccountType.Chequing, result.Type);
            _mockAccountRepo.Verify(r => r.AddAsync(It.IsAny<Account>()), Times.Once);
        }

        [Fact]
        public async Task CreateAccountAsync_ShouldCreateSavingsAccount()
        {
            var userId = Guid.NewGuid();
            _mockAccountRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(new List<Account>());
            var dto = new CreateAccountDto { Type = AccountType.Savings };

            var result = await _accountService.CreateAccountAsync(dto, userId);

            Assert.Equal(AccountType.Savings, result.Type);
            Assert.Equal(userId, result.UserId);
        }

        [Fact]
        public async Task CreateAccountAsync_ShouldGenerateAccountNumber()
        {
            var userId = Guid.NewGuid();
            _mockAccountRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(new List<Account>());
            var dto = new CreateAccountDto { Type = AccountType.Chequing };

            var result = await _accountService.CreateAccountAsync(dto, userId);

            Assert.NotEmpty(result.AccountNumber);
            Assert.Equal(12, result.AccountNumber.Length);
        }

        [Fact]
        public async Task CreateAccountAsync_ShouldGenerateUpperCaseAccountNumber()
        {
            var userId = Guid.NewGuid();
            _mockAccountRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(new List<Account>());
            var dto = new CreateAccountDto { Type = AccountType.Chequing };

            var result = await _accountService.CreateAccountAsync(dto, userId);

            Assert.Equal(result.AccountNumber, result.AccountNumber.ToUpper());
        }

        [Fact]
        public async Task CreateAccountAsync_ShouldGenerateUniqueAccountNumbers()
        {
            var userId = Guid.NewGuid();
            _mockAccountRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(new List<Account>());

            var dto1 = new CreateAccountDto { Type = AccountType.Chequing };
            var dto2 = new CreateAccountDto { Type = AccountType.Savings };

            var result1 = await _accountService.CreateAccountAsync(dto1, userId);

            _mockAccountRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(new List<Account> { result1 });

            var result2 = await _accountService.CreateAccountAsync(dto2, userId);

            Assert.NotEqual(result1.AccountNumber, result2.AccountNumber);
        }

        [Fact]
        public async Task CreateAccountAsync_ShouldSetCorrectUserId()
        {
            var userId = Guid.NewGuid();
            _mockAccountRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(new List<Account>());
            var dto = new CreateAccountDto { Type = AccountType.Chequing };

            var result = await _accountService.CreateAccountAsync(dto, userId);

            Assert.Equal(userId, result.UserId);
        }

        [Fact]
        public async Task CreateAccountAsync_ShouldCallAddAsync_Once()
        {
            var userId = Guid.NewGuid();
            _mockAccountRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(new List<Account>());
            var dto = new CreateAccountDto { Type = AccountType.Chequing };

            await _accountService.CreateAccountAsync(dto, userId);

            _mockAccountRepo.Verify(r => r.AddAsync(It.IsAny<Account>()), Times.Once);
        }

        [Fact]
        public async Task CreateAccountAsync_ShouldAllowSavings_WhenOnlyChequingExists()
        {
            var userId = Guid.NewGuid();
            var existing = new List<Account>
            {
                new Account { Id = Guid.NewGuid(), UserId = userId, Type = AccountType.Chequing, AccountNumber = "ACC001" }
            };
            _mockAccountRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(existing);
            var dto = new CreateAccountDto { Type = AccountType.Savings };

            var result = await _accountService.CreateAccountAsync(dto, userId);

            Assert.Equal(AccountType.Savings, result.Type);
            _mockAccountRepo.Verify(r => r.AddAsync(It.IsAny<Account>()), Times.Once);
        }

        [Fact]
        public async Task CreateAccountAsync_ShouldAllowChequing_WhenOnlySavingsExists()
        {
            var userId = Guid.NewGuid();
            var existing = new List<Account>
            {
                new Account { Id = Guid.NewGuid(), UserId = userId, Type = AccountType.Savings, AccountNumber = "ACC001" }
            };
            _mockAccountRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(existing);
            var dto = new CreateAccountDto { Type = AccountType.Chequing };

            var result = await _accountService.CreateAccountAsync(dto, userId);

            Assert.Equal(AccountType.Chequing, result.Type);
            _mockAccountRepo.Verify(r => r.AddAsync(It.IsAny<Account>()), Times.Once);
        }

        [Fact]
        public async Task CreateAccountAsync_ShouldThrow_WhenChequingAlreadyExists()
        {
            var userId = Guid.NewGuid();
            var existing = new List<Account>
            {
                new Account { Id = Guid.NewGuid(), UserId = userId, Type = AccountType.Chequing, AccountNumber = "ACC001" }
            };
            _mockAccountRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(existing);
            var dto = new CreateAccountDto { Type = AccountType.Chequing };

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _accountService.CreateAccountAsync(dto, userId));

            _mockAccountRepo.Verify(r => r.AddAsync(It.IsAny<Account>()), Times.Never);
        }

        [Fact]
        public async Task CreateAccountAsync_ShouldThrow_WhenSavingsAlreadyExists()
        {
            var userId = Guid.NewGuid();
            var existing = new List<Account>
            {
                new Account { Id = Guid.NewGuid(), UserId = userId, Type = AccountType.Savings, AccountNumber = "ACC001" }
            };
            _mockAccountRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(existing);
            var dto = new CreateAccountDto { Type = AccountType.Savings };

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _accountService.CreateAccountAsync(dto, userId));

            _mockAccountRepo.Verify(r => r.AddAsync(It.IsAny<Account>()), Times.Never);
        }

        [Fact]
        public async Task CreateAccountAsync_ShouldThrow_WithCorrectMessage_WhenChequingExists()
        {
            var userId = Guid.NewGuid();
            var existing = new List<Account>
            {
                new Account { Id = Guid.NewGuid(), UserId = userId, Type = AccountType.Chequing, AccountNumber = "ACC001" }
            };
            _mockAccountRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(existing);
            var dto = new CreateAccountDto { Type = AccountType.Chequing };

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _accountService.CreateAccountAsync(dto, userId));

            Assert.Contains("Chequing", ex.Message);
        }

        [Fact]
        public async Task CreateAccountAsync_ShouldThrow_WithCorrectMessage_WhenSavingsExists()
        {
            var userId = Guid.NewGuid();
            var existing = new List<Account>
            {
                new Account { Id = Guid.NewGuid(), UserId = userId, Type = AccountType.Savings, AccountNumber = "ACC001" }
            };
            _mockAccountRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(existing);
            var dto = new CreateAccountDto { Type = AccountType.Savings };

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _accountService.CreateAccountAsync(dto, userId));

            Assert.Contains("Savings", ex.Message);
        }

        [Fact]
        public async Task CreateAccountAsync_ShouldNotCallAddAsync_WhenThrows()
        {
            var userId = Guid.NewGuid();
            var existing = new List<Account>
            {
                new Account { Id = Guid.NewGuid(), UserId = userId, Type = AccountType.Chequing, AccountNumber = "ACC001" }
            };
            _mockAccountRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(existing);
            var dto = new CreateAccountDto { Type = AccountType.Chequing };

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _accountService.CreateAccountAsync(dto, userId));

            _mockAccountRepo.Verify(r => r.AddAsync(It.IsAny<Account>()), Times.Never);
        }

        [Fact]
        public async Task CreateAccountAsync_ShouldCallGetByUserIdAsync_BeforeAdding()
        {
            var userId = Guid.NewGuid();
            _mockAccountRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(new List<Account>());
            var dto = new CreateAccountDto { Type = AccountType.Chequing };

            await _accountService.CreateAccountAsync(dto, userId);

            _mockAccountRepo.Verify(r => r.GetByUserIdAsync(userId), Times.Once);
        }

        [Fact]
        public async Task GetAccountsByUserIdAsync_ShouldReturnAccounts()
        {
            var userId = Guid.NewGuid();
            var accounts = new List<Account>
            {
                new Account { Id = Guid.NewGuid(), UserId = userId, Type = AccountType.Chequing, AccountNumber = "ACC001" },
                new Account { Id = Guid.NewGuid(), UserId = userId, Type = AccountType.Savings, AccountNumber = "ACC002" }
            };
            _mockAccountRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(accounts);

            var result = await _accountService.GetAccountsByUserIdAsync(userId);

            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task GetAccountsByUserIdAsync_ShouldReturnEmpty_WhenNoAccounts()
        {
            var userId = Guid.NewGuid();
            _mockAccountRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(new List<Account>());

            var result = await _accountService.GetAccountsByUserIdAsync(userId);

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAccountsByUserIdAsync_ShouldCallRepository_Once()
        {
            var userId = Guid.NewGuid();
            _mockAccountRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(new List<Account>());

            await _accountService.GetAccountsByUserIdAsync(userId);

            _mockAccountRepo.Verify(r => r.GetByUserIdAsync(userId), Times.Once);
        }

        [Fact]
        public async Task GetAccountsByUserIdAsync_ShouldReturnCorrectAccountTypes()
        {
            var userId = Guid.NewGuid();
            var accounts = new List<Account>
            {
                new Account { Id = Guid.NewGuid(), UserId = userId, Type = AccountType.Chequing, AccountNumber = "ACC001" },
                new Account { Id = Guid.NewGuid(), UserId = userId, Type = AccountType.Savings, AccountNumber = "ACC002" }
            };
            _mockAccountRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(accounts);

            var result = await _accountService.GetAccountsByUserIdAsync(userId);

            Assert.Contains(result, a => a.Type == AccountType.Chequing);
            Assert.Contains(result, a => a.Type == AccountType.Savings);
        }

        [Fact]
        public async Task GetAccountsByUserIdAsync_ShouldReturnSingleAccount()
        {
            var userId = Guid.NewGuid();
            var accounts = new List<Account>
            {
                new Account { Id = Guid.NewGuid(), UserId = userId, Type = AccountType.Chequing, AccountNumber = "ACC001" }
            };
            _mockAccountRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(accounts);

            var result = await _accountService.GetAccountsByUserIdAsync(userId);

            Assert.Single(result);
        }

        [Fact]
        public async Task GetAccountsByUserIdAsync_ShouldReturnAccountsWithCorrectUserId()
        {
            var userId = Guid.NewGuid();
            var accounts = new List<Account>
            {
                new Account { Id = Guid.NewGuid(), UserId = userId, Type = AccountType.Chequing, AccountNumber = "ACC001" }
            };
            _mockAccountRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(accounts);

            var result = await _accountService.GetAccountsByUserIdAsync(userId);

            Assert.All(result, a => Assert.Equal(userId, a.UserId));
        }

        [Fact]
        public async Task GetAccountsByUserIdAsync_ShouldNotCallAddAsync()
        {
            var userId = Guid.NewGuid();
            _mockAccountRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(new List<Account>());

            await _accountService.GetAccountsByUserIdAsync(userId);

            _mockAccountRepo.Verify(r => r.AddAsync(It.IsAny<Account>()), Times.Never);
        }
    }
}