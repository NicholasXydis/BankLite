using BankLite.Application.DTOs;
using BankLite.Application.Interfaces;
using BankLite.Application.Services;
using BankLite.Domain.Entities;
using BankLite.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BankLite.Tests.Services
{
    public class TransactionServiceTests
    {
        private readonly Mock<IAccountRepository> _mockAccountRepo;
        private readonly Mock<ITransactionRepository> _mockTransactionRepo;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IAuditLogRepository> _mockAuditLogRepo;
        private readonly Mock<IBalanceNotifier> _mockBalanceNotifier;
        private readonly TransactionService _transactionService;

        public TransactionServiceTests()
        {
            _mockAccountRepo = new Mock<IAccountRepository>();
            _mockTransactionRepo = new Mock<ITransactionRepository>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockAuditLogRepo = new Mock<IAuditLogRepository>();
            _mockBalanceNotifier = new Mock<IBalanceNotifier>();

            _transactionService = new TransactionService(_mockAccountRepo.Object, _mockTransactionRepo.Object, _mockUnitOfWork.Object, _mockAuditLogRepo.Object, new NullLogger<TransactionService>(), _mockBalanceNotifier.Object);

            _mockUnitOfWork
                .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
                .Returns((Func<Task> operation) => operation());

        }

        [Fact]
        public async Task DepositAsync_ShouldIncreaseBalance()
        {
            var userId = Guid.NewGuid();
            var account = new Account
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Balance = 1000,
                AccountNumber = "ACC001",
                Type = AccountType.Chequing
            };

            _mockAccountRepo
                .Setup(r => r.GetByIdAsync(account.Id))
                .ReturnsAsync(account);

            var dto = new DepositWithdrawDto
            {
                AccountId = account.Id,
                Amount = 250
            };

            await _transactionService.DepositAsync(dto, userId);

            Assert.Equal(1250, account.Balance);
            _mockTransactionRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Once);
            _mockAccountRepo.Verify(r => r.UpdateAsync(account), Times.Once);
            _mockUnitOfWork.Verify(r => r.SaveAsync(), Times.Once);
        }

        [Fact]
        public async Task DepositAsync_ShouldReturnTransaction_WithCorrectProperties()
        {
            var userId = Guid.NewGuid();
            var account = new Account { Id = Guid.NewGuid(), UserId = userId, Balance = 1000, AccountNumber = "ACC001", Type = AccountType.Chequing };
            _mockAccountRepo.Setup(r => r.GetByIdAsync(account.Id)).ReturnsAsync(account);
            var dto = new DepositWithdrawDto { AccountId = account.Id, Amount = 500 };

            var result = await _transactionService.DepositAsync(dto, userId);

            Assert.Equal(account.Id, result.AccountId);
            Assert.Equal(500, result.Amount);
            Assert.Equal(TransactionType.Deposit, result.Type);
        }

        [Fact]
        public async Task DepositAsync_ShouldReturnExisting_WhenDuplicateIdempotencyKey()
        {
            var userId = Guid.NewGuid();
            var account = new Account { Id = Guid.NewGuid(), UserId = userId, Balance = 1000, AccountNumber = "ACC001", Type = AccountType.Chequing };
            var existingTransaction = new Transaction { Id = Guid.NewGuid(), AccountId = account.Id, Amount = 250, Type = TransactionType.Deposit };
            _mockAccountRepo.Setup(r => r.GetByIdAsync(account.Id)).ReturnsAsync(account);
            _mockTransactionRepo.Setup(r => r.GetByIdempotencyKeyAsync("test-key")).ReturnsAsync(existingTransaction);
            var dto = new DepositWithdrawDto { AccountId = account.Id, Amount = 250 };

            var result = await _transactionService.DepositAsync(dto, userId, "test-key");

            Assert.Equal(existingTransaction.Id, result.Id);
            _mockTransactionRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Never);
        }

        [Fact]
        public async Task DepositAsync_ShouldNotCheckIdempotency_WhenKeyIsNull()
        {
            var userId = Guid.NewGuid();
            var account = new Account { Id = Guid.NewGuid(), UserId = userId, Balance = 1000, AccountNumber = "ACC001", Type = AccountType.Chequing };
            _mockAccountRepo.Setup(r => r.GetByIdAsync(account.Id)).ReturnsAsync(account);
            var dto = new DepositWithdrawDto { AccountId = account.Id, Amount = 250 };

            await _transactionService.DepositAsync(dto, userId);

            _mockTransactionRepo.Verify(r => r.GetByIdempotencyKeyAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task DepositAsync_ShouldThrow_WhenUnauthorized()
        {
            var account = new Account
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Balance = 1000,
                AccountNumber = "ACC001",
                Type = AccountType.Chequing
            };

            _mockAccountRepo
                .Setup(r => r.GetByIdAsync(account.Id))
                .ReturnsAsync(account);

            var dto = new DepositWithdrawDto
            {
                AccountId = account.Id,
                Amount = 250
            };

            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _transactionService.DepositAsync(dto, Guid.NewGuid()));

            _mockTransactionRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Never);
            _mockAccountRepo.Verify(r => r.UpdateAsync(It.IsAny<Account>()), Times.Never);
            _mockUnitOfWork.Verify(r => r.SaveAsync(), Times.Never);
        }

        [Fact]
        public async Task DepositAsync_ShouldThrow_WhenAccountNotFound()
        {
            _mockAccountRepo
                .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Account?)null);

            var dto = new DepositWithdrawDto
            {
                AccountId = Guid.NewGuid(),
                Amount = 250
            };

            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _transactionService.DepositAsync(dto, Guid.NewGuid()));

            _mockTransactionRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Never);
            _mockAccountRepo.Verify(r => r.UpdateAsync(It.IsAny<Account>()), Times.Never);
            _mockUnitOfWork.Verify(r => r.SaveAsync(), Times.Never);
        }

        [Fact]
        public async Task WithdrawAsync_ShouldDecreaseBalance()
        {
            var userId = Guid.NewGuid();
            var account = new Account { Id = Guid.NewGuid(), UserId = userId, Balance = 1000, AccountNumber = "ACC001", Type = AccountType.Chequing };
            _mockAccountRepo.Setup(r => r.GetByIdAsync(account.Id)).ReturnsAsync(account);
            var dto = new DepositWithdrawDto { AccountId = account.Id, Amount = 250 };

            await _transactionService.WithdrawAsync(dto, userId);

            Assert.Equal(750, account.Balance);
            _mockTransactionRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Once);
            _mockAccountRepo.Verify(r => r.UpdateAsync(account), Times.Once);
            _mockUnitOfWork.Verify(r => r.SaveAsync(), Times.Once);
        }

        [Fact]
        public async Task WithdrawAsync_ShouldReturnTransaction_WithCorrectProperties()
        {
            var userId = Guid.NewGuid();
            var account = new Account { Id = Guid.NewGuid(), UserId = userId, Balance = 1000, AccountNumber = "ACC001", Type = AccountType.Chequing };
            _mockAccountRepo.Setup(r => r.GetByIdAsync(account.Id)).ReturnsAsync(account);
            var dto = new DepositWithdrawDto { AccountId = account.Id, Amount = 300 };

            var result = await _transactionService.WithdrawAsync(dto, userId);

            Assert.Equal(account.Id, result.AccountId);
            Assert.Equal(300, result.Amount);
            Assert.Equal(TransactionType.Withdrawal, result.Type);
        }

        [Fact]
        public async Task WithdrawAsync_ShouldSucceed_WhenAmountEqualsBalance()
        {
            var userId = Guid.NewGuid();
            var account = new Account { Id = Guid.NewGuid(), UserId = userId, Balance = 1000, AccountNumber = "ACC001", Type = AccountType.Chequing };
            _mockAccountRepo.Setup(r => r.GetByIdAsync(account.Id)).ReturnsAsync(account);
            var dto = new DepositWithdrawDto { AccountId = account.Id, Amount = 1000 };

            await _transactionService.WithdrawAsync(dto, userId);

            Assert.Equal(0, account.Balance);
        }

        [Fact]
        public async Task WithdrawAsync_ShouldReturnExisting_WhenDuplicateIdempotencyKey()
        {
            var userId = Guid.NewGuid();
            var account = new Account { Id = Guid.NewGuid(), UserId = userId, Balance = 1000, AccountNumber = "ACC001", Type = AccountType.Chequing };
            var existingTransaction = new Transaction { Id = Guid.NewGuid(), AccountId = account.Id, Amount = 250, Type = TransactionType.Withdrawal };
            _mockAccountRepo.Setup(r => r.GetByIdAsync(account.Id)).ReturnsAsync(account);
            _mockTransactionRepo.Setup(r => r.GetByIdempotencyKeyAsync("test-key")).ReturnsAsync(existingTransaction);
            var dto = new DepositWithdrawDto { AccountId = account.Id, Amount = 250 };

            var result = await _transactionService.WithdrawAsync(dto, userId, "test-key");

            Assert.Equal(existingTransaction.Id, result.Id);
            _mockTransactionRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Never);
        }

        [Fact]
        public async Task WithdrawAsync_ShouldThrow_WhenInsufficientFunds()
        {
            var userId = Guid.NewGuid();
            var account = new Account
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Balance = 1000,
                AccountNumber = "ACC001",
                Type = AccountType.Chequing
            };

            _mockAccountRepo
                .Setup(r => r.GetByIdAsync(account.Id))
                .ReturnsAsync(account);

            var dto = new DepositWithdrawDto
            {
                AccountId = account.Id,
                Amount = 2000
            };

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _transactionService.WithdrawAsync(dto, userId));
        }

        [Fact]
        public async Task WithdrawAsync_ShouldThrow_WhenUnauthorized()
        {
            var account = new Account
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Balance = 1000,
                AccountNumber = "ACC001",
                Type = AccountType.Chequing
            };

            _mockAccountRepo
                .Setup(r => r.GetByIdAsync(account.Id))
                .ReturnsAsync(account);

            var dto = new DepositWithdrawDto
            {
                AccountId = account.Id,
                Amount = 250
            };

            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _transactionService.WithdrawAsync(dto, Guid.NewGuid()));

            _mockTransactionRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Never);
            _mockAccountRepo.Verify(r => r.UpdateAsync(It.IsAny<Account>()), Times.Never);
            _mockUnitOfWork.Verify(r => r.SaveAsync(), Times.Never);
        }

        [Fact]
        public async Task WithdrawAsync_ShouldThrow_WhenAccountNotFound()
        {
            _mockAccountRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Account?)null);
            var dto = new DepositWithdrawDto { AccountId = Guid.NewGuid(), Amount = 250 };

            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _transactionService.WithdrawAsync(dto, Guid.NewGuid()));
        }

        [Fact]
        public async Task WithdrawAsync_ShouldNotCheckIdempotency_WhenKeyIsNull()
        {
            var userId = Guid.NewGuid();
            var account = new Account { Id = Guid.NewGuid(), UserId = userId, Balance = 1000, AccountNumber = "ACC001", Type = AccountType.Chequing };
            _mockAccountRepo.Setup(r => r.GetByIdAsync(account.Id)).ReturnsAsync(account);
            var dto = new DepositWithdrawDto { AccountId = account.Id, Amount = 250 };

            await _transactionService.WithdrawAsync(dto, userId);

            _mockTransactionRepo.Verify(r => r.GetByIdempotencyKeyAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task TransferAsync_ShouldMoveMoney_BetweenAccounts()
        {
            var userId = Guid.NewGuid();
            var fromAccount = new Account
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Balance = 1000,
                AccountNumber = "ACC001",
                Type = AccountType.Chequing
            };

            var toAccount = new Account
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Balance = 1000,
                AccountNumber = "ACC002",
                Type = AccountType.Chequing
            };

            _mockAccountRepo
                .Setup(r => r.GetByIdAsync(fromAccount.Id))
                .ReturnsAsync(fromAccount);

            _mockAccountRepo
                .Setup(r => r.GetByIdAsync(toAccount.Id))
                .ReturnsAsync(toAccount);

            var dto = new TransferDto
            {
                FromAccountId = fromAccount.Id,
                ToAccountId = toAccount.Id,
                Amount = 500
            };

            await _transactionService.TransferAsync(dto, userId);

            Assert.Equal(500, fromAccount.Balance);
            Assert.Equal(1500, toAccount.Balance);
            _mockTransactionRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Exactly(2));
            _mockAccountRepo.Verify(r => r.UpdateAsync(It.IsAny<Account>()), Times.Exactly(2));
        }

        [Fact]
        public async Task TransferAsync_ShouldCreateTwoTransactions()
        {
            var userId = Guid.NewGuid();
            var fromAccount = new Account { Id = Guid.NewGuid(), UserId = userId, Balance = 1000, AccountNumber = "ACC001", Type = AccountType.Chequing };
            var toAccount = new Account { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Balance = 500, AccountNumber = "ACC002", Type = AccountType.Chequing };
            _mockAccountRepo.Setup(r => r.GetByIdAsync(fromAccount.Id)).ReturnsAsync(fromAccount);
            _mockAccountRepo.Setup(r => r.GetByIdAsync(toAccount.Id)).ReturnsAsync(toAccount);
            var dto = new TransferDto { FromAccountId = fromAccount.Id, ToAccountId = toAccount.Id, Amount = 200 };

            await _transactionService.TransferAsync(dto, userId);

            _mockTransactionRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Exactly(2));
        }

        [Fact]
        public async Task TransferAsync_ShouldThrow_WhenInsufficientFunds()
        {
            var userId = Guid.NewGuid();
            var fromAccount = new Account { Id = Guid.NewGuid(), UserId = userId, Balance = 100, AccountNumber = "ACC001", Type = AccountType.Chequing };
            var toAccount = new Account { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Balance = 1000, AccountNumber = "ACC002", Type = AccountType.Chequing };
            _mockAccountRepo.Setup(r => r.GetByIdAsync(fromAccount.Id)).ReturnsAsync(fromAccount);
            _mockAccountRepo.Setup(r => r.GetByIdAsync(toAccount.Id)).ReturnsAsync(toAccount);
            var dto = new TransferDto { FromAccountId = fromAccount.Id, ToAccountId = toAccount.Id, Amount = 500 };

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _transactionService.TransferAsync(dto, userId));
        }

        [Fact]
        public async Task TransferAsync_ShouldThrow_WhenToAccountNotFound()
        {
            var userId = Guid.NewGuid();
            var fromAccount = new Account { Id = Guid.NewGuid(), UserId = userId, Balance = 1000, AccountNumber = "ACC001", Type = AccountType.Chequing };
            _mockAccountRepo.Setup(r => r.GetByIdAsync(fromAccount.Id)).ReturnsAsync(fromAccount);
            _mockAccountRepo.Setup(r => r.GetByIdAsync(It.Is<Guid>(id => id != fromAccount.Id))).ReturnsAsync((Account?)null);
            var dto = new TransferDto { FromAccountId = fromAccount.Id, ToAccountId = Guid.NewGuid(), Amount = 500 };

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _transactionService.TransferAsync(dto, userId));
        }

        [Fact]
        public async Task TransferAsync_ShouldThrow_WhenUnauthorized()
        {
            var fromAccount = new Account
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Balance = 1000,
                AccountNumber = "ACC001",
                Type = AccountType.Chequing
            };

            var toAccount = new Account
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Balance = 1000,
                AccountNumber = "ACC002",
                Type = AccountType.Chequing
            };

            _mockAccountRepo
                .Setup(r => r.GetByIdAsync(fromAccount.Id))
                .ReturnsAsync(fromAccount);

            _mockAccountRepo
                .Setup(r => r.GetByIdAsync(toAccount.Id))
                .ReturnsAsync(toAccount);

            var dto = new TransferDto
            {
                FromAccountId = fromAccount.Id,
                ToAccountId = toAccount.Id,
                Amount = 500
            };

            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _transactionService.TransferAsync(dto, Guid.NewGuid()));

            _mockTransactionRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Never);
            _mockAccountRepo.Verify(r => r.UpdateAsync(It.IsAny<Account>()), Times.Never);
            _mockUnitOfWork.Verify(r => r.SaveAsync(), Times.Never);
        }

        [Fact]
        public async Task TransferAsync_ShouldThrow_WhenFromAccountNotFound()
        {
            _mockAccountRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Account?)null);
            var dto = new TransferDto { FromAccountId = Guid.NewGuid(), ToAccountId = Guid.NewGuid(), Amount = 500 };

            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _transactionService.TransferAsync(dto, Guid.NewGuid()));
        }

        [Fact]
        public async Task TransferExternalAsync_ShouldMoveMoney()
        {
            var userId = Guid.NewGuid();
            var fromAccount = new Account { Id = Guid.NewGuid(), UserId = userId, Balance = 1000, AccountNumber = "ACC001", Type = AccountType.Chequing };
            var toAccount = new Account { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Balance = 500, AccountNumber = "ACC002", Type = AccountType.Chequing };
            _mockAccountRepo.Setup(r => r.GetByIdAsync(fromAccount.Id)).ReturnsAsync(fromAccount);
            _mockAccountRepo.Setup(r => r.GetByAccountNumberAsync("ACC002")).ReturnsAsync(toAccount);
            var dto = new ExternalTransferDto { FromAccountId = fromAccount.Id, ToAccountNumber = "ACC002", Amount = 300 };

            await _transactionService.TransferExternalAsync(dto, userId);

            Assert.Equal(700, fromAccount.Balance);
            Assert.Equal(800, toAccount.Balance);
        }

        [Fact]
        public async Task TransferExternalAsync_ShouldCreateTwoTransactions()
        {
            var userId = Guid.NewGuid();
            var fromAccount = new Account { Id = Guid.NewGuid(), UserId = userId, Balance = 1000, AccountNumber = "ACC001", Type = AccountType.Chequing };
            var toAccount = new Account { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Balance = 500, AccountNumber = "ACC002", Type = AccountType.Chequing };
            _mockAccountRepo.Setup(r => r.GetByIdAsync(fromAccount.Id)).ReturnsAsync(fromAccount);
            _mockAccountRepo.Setup(r => r.GetByAccountNumberAsync("ACC002")).ReturnsAsync(toAccount);
            var dto = new ExternalTransferDto { FromAccountId = fromAccount.Id, ToAccountNumber = "ACC002", Amount = 200 };

            await _transactionService.TransferExternalAsync(dto, userId);

            _mockTransactionRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Exactly(2));
        }

        [Fact]
        public async Task TransferExternalAsync_ShouldReturnEarly_WhenDuplicateIdempotencyKey()
        {
            var userId = Guid.NewGuid();
            var fromAccount = new Account { Id = Guid.NewGuid(), UserId = userId, Balance = 1000, AccountNumber = "ACC001", Type = AccountType.Chequing };
            _mockAccountRepo.Setup(r => r.GetByIdAsync(fromAccount.Id)).ReturnsAsync(fromAccount);
            _mockTransactionRepo.Setup(r => r.GetByIdempotencyKeyAsync("test-key")).ReturnsAsync(new Transaction());
            var dto = new ExternalTransferDto { FromAccountId = fromAccount.Id, ToAccountNumber = "ACC002", Amount = 300 };

            await _transactionService.TransferExternalAsync(dto, userId, "test-key");

            _mockAccountRepo.Verify(r => r.UpdateAsync(It.IsAny<Account>()), Times.Never);
        }

        [Fact]
        public async Task TransferExternalAsync_ShouldThrow_WhenUnauthorized()
        {
            var fromAccount = new Account { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Balance = 1000, AccountNumber = "ACC001", Type = AccountType.Chequing };
            _mockAccountRepo.Setup(r => r.GetByIdAsync(fromAccount.Id)).ReturnsAsync(fromAccount);
            var dto = new ExternalTransferDto { FromAccountId = fromAccount.Id, ToAccountNumber = "ACC002", Amount = 300 };

            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _transactionService.TransferExternalAsync(dto, Guid.NewGuid()));
        }

        [Fact]
        public async Task TransferExternalAsync_ShouldThrow_WhenFromAccountNotFound()
        {
            _mockAccountRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Account?)null);
            var dto = new ExternalTransferDto { FromAccountId = Guid.NewGuid(), ToAccountNumber = "ACC002", Amount = 300 };

            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _transactionService.TransferExternalAsync(dto, Guid.NewGuid()));
        }

        [Fact]
        public async Task TransferExternalAsync_ShouldThrow_WhenToAccountNotFound()
        {
            var userId = Guid.NewGuid();
            var fromAccount = new Account { Id = Guid.NewGuid(), UserId = userId, Balance = 1000, AccountNumber = "ACC001", Type = AccountType.Chequing };
            _mockAccountRepo.Setup(r => r.GetByIdAsync(fromAccount.Id)).ReturnsAsync(fromAccount);
            _mockAccountRepo.Setup(r => r.GetByAccountNumberAsync(It.IsAny<string>())).ReturnsAsync((Account?)null);
            var dto = new ExternalTransferDto { FromAccountId = fromAccount.Id, ToAccountNumber = "NOTEXIST", Amount = 300 };

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _transactionService.TransferExternalAsync(dto, userId));
        }

        [Fact]
        public async Task TransferExternalAsync_ShouldThrow_WhenSameAccount()
        {
            var userId = Guid.NewGuid();
            var fromAccount = new Account { Id = Guid.NewGuid(), UserId = userId, Balance = 1000, AccountNumber = "ACC001", Type = AccountType.Chequing };
            _mockAccountRepo.Setup(r => r.GetByIdAsync(fromAccount.Id)).ReturnsAsync(fromAccount);
            _mockAccountRepo.Setup(r => r.GetByAccountNumberAsync("ACC001")).ReturnsAsync(fromAccount);
            var dto = new ExternalTransferDto { FromAccountId = fromAccount.Id, ToAccountNumber = "ACC001", Amount = 300 };

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _transactionService.TransferExternalAsync(dto, userId));
        }

        [Fact]
        public async Task TransferExternalAsync_ShouldThrow_WhenInsufficientFunds()
        {
            var userId = Guid.NewGuid();
            var fromAccount = new Account { Id = Guid.NewGuid(), UserId = userId, Balance = 100, AccountNumber = "ACC001", Type = AccountType.Chequing };
            var toAccount = new Account { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Balance = 500, AccountNumber = "ACC002", Type = AccountType.Chequing };
            _mockAccountRepo.Setup(r => r.GetByIdAsync(fromAccount.Id)).ReturnsAsync(fromAccount);
            _mockAccountRepo.Setup(r => r.GetByAccountNumberAsync("ACC002")).ReturnsAsync(toAccount);
            var dto = new ExternalTransferDto { FromAccountId = fromAccount.Id, ToAccountNumber = "ACC002", Amount = 500 };

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _transactionService.TransferExternalAsync(dto, userId));
        }

        [Fact]
        public async Task GetTransactionsByAccountIdAsync_ShouldReturnTransactions()
        {
            var userId = Guid.NewGuid();
            var account = new Account { Id = Guid.NewGuid(), UserId = userId, Balance = 1000, AccountNumber = "ACC001", Type = AccountType.Chequing };
            _mockAccountRepo.Setup(r => r.GetByIdAsync(account.Id)).ReturnsAsync(account);
            _mockTransactionRepo.Setup(r => r.GetByAccountIdAsync(account.Id, 1, 10, null)).ReturnsAsync(new List<Transaction>());
            _mockTransactionRepo.Setup(r => r.GetTotalCountAsync(account.Id, null)).ReturnsAsync(0);

            var result = await _transactionService.GetTransactionsByAccountIdAsync(account.Id, userId, 1, 10);

            Assert.NotNull(result);
            Assert.Equal(0, result.TotalCount);
        }

        [Fact]
        public async Task GetTransactionsByAccountIdAsync_ShouldReturnCorrectTotalCount()
        {
            var userId = Guid.NewGuid();
            var account = new Account { Id = Guid.NewGuid(), UserId = userId, Balance = 1000, AccountNumber = "ACC001", Type = AccountType.Chequing };
            _mockAccountRepo.Setup(r => r.GetByIdAsync(account.Id)).ReturnsAsync(account);
            _mockTransactionRepo.Setup(r => r.GetByAccountIdAsync(account.Id, 1, 10, null)).ReturnsAsync(new List<Transaction>());
            _mockTransactionRepo.Setup(r => r.GetTotalCountAsync(account.Id, null)).ReturnsAsync(42);

            var result = await _transactionService.GetTransactionsByAccountIdAsync(account.Id, userId, 1, 10);

            Assert.Equal(42, result.TotalCount);
            Assert.Equal(1, result.Page);
            Assert.Equal(10, result.PageSize);
        }

        [Fact]
        public async Task GetTransactionsByAccountIdAsync_ShouldThrow_WhenUnauthorized()
        {
            var account = new Account
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Balance = 1000,
                AccountNumber = "ACC001",
                Type = AccountType.Chequing
            };

            _mockAccountRepo
                .Setup(r => r.GetByIdAsync(account.Id))
                .ReturnsAsync(account);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _transactionService.GetTransactionsByAccountIdAsync(account.Id, Guid.NewGuid(), 1, 10));

            _mockTransactionRepo.Verify(r => r.GetByAccountIdAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetTransactionsByDateRangeAsync_ShouldReturnTransactions()
        {
            var userId = Guid.NewGuid();
            var account = new Account { Id = Guid.NewGuid(), UserId = userId, Balance = 1000, AccountNumber = "ACC001", Type = AccountType.Chequing };
            _mockAccountRepo.Setup(r => r.GetByIdAsync(account.Id)).ReturnsAsync(account);
            _mockTransactionRepo.Setup(r => r.GetByAccountIdAndDateRangeAsync(account.Id, It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(new List<Transaction>());

            var result = await _transactionService.GetTransactionsByDateRangeAsync(account.Id, userId, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetTransactionsByDateRangeAsync_ShouldThrow_WhenUnauthorized()
        {
            var account = new Account { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Balance = 1000, AccountNumber = "ACC001", Type = AccountType.Chequing };
            _mockAccountRepo.Setup(r => r.GetByIdAsync(account.Id)).ReturnsAsync(account);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _transactionService.GetTransactionsByDateRangeAsync(account.Id, Guid.NewGuid(), DateTime.UtcNow.AddDays(-7), DateTime.UtcNow));
        }
    }
}