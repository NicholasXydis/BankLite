using BankLite.Application.DTOs;
using BankLite.Application.Interfaces;
using BankLite.Application.Services;
using BankLite.Domain.Entities;
using BankLite.Domain.Interfaces;
using FluentAssertions;
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

            _mockUnitOfWork
                .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
                .Returns((Func<Task> operation) => operation());

            _transactionService = new TransactionService(
                _mockAccountRepo.Object,
                _mockTransactionRepo.Object,
                _mockUnitOfWork.Object,
                _mockAuditLogRepo.Object,
                new NullLogger<TransactionService>(),
                _mockBalanceNotifier.Object);
        }

        private Account CreateAccount(Guid userId, decimal balance = 1000, string accountNumber = "ACC001")
        {
            return new Account
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Balance = balance,
                AccountNumber = accountNumber,
                Type = AccountType.Chequing
            };
        }

        private Transaction CreateTransaction(Guid accountId, decimal amount, TransactionType type)
        {
            return new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                Amount = amount,
                Type = type
            };
        }

        [Fact]
        public async Task DepositAsync_ValidRequest_IncreasesBalance()
        {
            var userId = Guid.NewGuid();
            var account = CreateAccount(userId, balance: 1000);
            _mockAccountRepo.Setup(r => r.GetByIdAsync(account.Id)).ReturnsAsync(account);
            var dto = new DepositWithdrawDto { AccountId = account.Id, Amount = 250 };

            await _transactionService.DepositAsync(dto, userId);

            account.Balance.Should().Be(1250);
            _mockTransactionRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Once);
            _mockAccountRepo.Verify(r => r.UpdateAsync(account), Times.Once);
            _mockUnitOfWork.Verify(r => r.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()), Times.Once);
        }

        [Fact]
        public async Task DepositAsync_ValidRequest_ReturnsTransactionWithCorrectProperties()
        {
            var userId = Guid.NewGuid();
            var account = CreateAccount(userId);
            _mockAccountRepo.Setup(r => r.GetByIdAsync(account.Id)).ReturnsAsync(account);
            var dto = new DepositWithdrawDto { AccountId = account.Id, Amount = 500 };

            var result = await _transactionService.DepositAsync(dto, userId);

            result.AccountId.Should().Be(account.Id);
            result.Amount.Should().Be(500);
            result.Type.Should().Be("Deposit");
        }

        [Fact]
        public async Task DepositAsync_DuplicateIdempotencyKey_ReturnsExistingTransaction()
        {
            var userId = Guid.NewGuid();
            var account = CreateAccount(userId);
            var existingTransaction = CreateTransaction(account.Id, 250, TransactionType.Deposit);
            _mockAccountRepo.Setup(r => r.GetByIdAsync(account.Id)).ReturnsAsync(account);
            _mockTransactionRepo.Setup(r => r.GetByIdempotencyKeyAsync("test-key")).ReturnsAsync(existingTransaction);
            var dto = new DepositWithdrawDto { AccountId = account.Id, Amount = 250 };

            var result = await _transactionService.DepositAsync(dto, userId, "test-key");

            result.Id.Should().Be(existingTransaction.Id);
            _mockTransactionRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Never);
        }

        [Fact]
        public async Task DepositAsync_NullIdempotencyKey_NeverChecksIdempotency()
        {
            var userId = Guid.NewGuid();
            var account = CreateAccount(userId);
            _mockAccountRepo.Setup(r => r.GetByIdAsync(account.Id)).ReturnsAsync(account);
            var dto = new DepositWithdrawDto { AccountId = account.Id, Amount = 250 };

            await _transactionService.DepositAsync(dto, userId);

            _mockTransactionRepo.Verify(r => r.GetByIdempotencyKeyAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task DepositAsync_UnauthorizedUser_ThrowsUnauthorizedAccessException()
        {
            var account = CreateAccount(Guid.NewGuid());
            _mockAccountRepo.Setup(r => r.GetByIdAsync(account.Id)).ReturnsAsync(account);
            var dto = new DepositWithdrawDto { AccountId = account.Id, Amount = 250 };

            var act = async () => await _transactionService.DepositAsync(dto, Guid.NewGuid());

            await act.Should().ThrowAsync<UnauthorizedAccessException>();
            _mockTransactionRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Never);
            _mockAccountRepo.Verify(r => r.UpdateAsync(It.IsAny<Account>()), Times.Never);
        }

        [Fact]
        public async Task DepositAsync_AccountNotFound_ThrowsUnauthorizedAccessException()
        {
            _mockAccountRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Account?)null);
            var dto = new DepositWithdrawDto { AccountId = Guid.NewGuid(), Amount = 250 };

            var act = async () => await _transactionService.DepositAsync(dto, Guid.NewGuid());

            await act.Should().ThrowAsync<UnauthorizedAccessException>();
            _mockTransactionRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Never);
            _mockAccountRepo.Verify(r => r.UpdateAsync(It.IsAny<Account>()), Times.Never);
        }

        [Fact]
        public async Task WithdrawAsync_ValidRequest_DecreasesBalance()
        {
            var userId = Guid.NewGuid();
            var account = CreateAccount(userId, balance: 1000);
            _mockAccountRepo.Setup(r => r.GetByIdAsync(account.Id)).ReturnsAsync(account);
            var dto = new DepositWithdrawDto { AccountId = account.Id, Amount = 250 };

            await _transactionService.WithdrawAsync(dto, userId);

            account.Balance.Should().Be(750);
            _mockTransactionRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Once);
            _mockAccountRepo.Verify(r => r.UpdateAsync(account), Times.Once);
            _mockUnitOfWork.Verify(r => r.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()), Times.Once);
        }

        [Fact]
        public async Task WithdrawAsync_ValidRequest_ReturnsTransactionWithCorrectProperties()
        {
            var userId = Guid.NewGuid();
            var account = CreateAccount(userId);
            _mockAccountRepo.Setup(r => r.GetByIdAsync(account.Id)).ReturnsAsync(account);
            var dto = new DepositWithdrawDto { AccountId = account.Id, Amount = 300 };

            var result = await _transactionService.WithdrawAsync(dto, userId);

            result.AccountId.Should().Be(account.Id);
            result.Amount.Should().Be(300);
            result.Type.Should().Be("Withdrawal");
        }

        [Fact]
        public async Task WithdrawAsync_AmountEqualsBalance_ReducesBalanceToZero()
        {
            var userId = Guid.NewGuid();
            var account = CreateAccount(userId, balance: 1000);
            _mockAccountRepo.Setup(r => r.GetByIdAsync(account.Id)).ReturnsAsync(account);
            var dto = new DepositWithdrawDto { AccountId = account.Id, Amount = 1000 };

            await _transactionService.WithdrawAsync(dto, userId);

            account.Balance.Should().Be(0);
        }

        [Fact]
        public async Task WithdrawAsync_DuplicateIdempotencyKey_ReturnsExistingTransaction()
        {
            var userId = Guid.NewGuid();
            var account = CreateAccount(userId);
            var existingTransaction = CreateTransaction(account.Id, 250, TransactionType.Withdrawal);
            _mockAccountRepo.Setup(r => r.GetByIdAsync(account.Id)).ReturnsAsync(account);
            _mockTransactionRepo.Setup(r => r.GetByIdempotencyKeyAsync("test-key")).ReturnsAsync(existingTransaction);
            var dto = new DepositWithdrawDto { AccountId = account.Id, Amount = 250 };

            var result = await _transactionService.WithdrawAsync(dto, userId, "test-key");

            result.Id.Should().Be(existingTransaction.Id);
            _mockTransactionRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Never);
        }

        [Fact]
        public async Task WithdrawAsync_InsufficientFunds_ThrowsInvalidOperationException()
        {
            var userId = Guid.NewGuid();
            var account = CreateAccount(userId, balance: 1000);
            _mockAccountRepo.Setup(r => r.GetByIdAsync(account.Id)).ReturnsAsync(account);
            var dto = new DepositWithdrawDto { AccountId = account.Id, Amount = 2000 };

            var act = async () => await _transactionService.WithdrawAsync(dto, userId);

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task WithdrawAsync_UnauthorizedUser_ThrowsUnauthorizedAccessException()
        {
            var account = CreateAccount(Guid.NewGuid());
            _mockAccountRepo.Setup(r => r.GetByIdAsync(account.Id)).ReturnsAsync(account);
            var dto = new DepositWithdrawDto { AccountId = account.Id, Amount = 250 };

            var act = async () => await _transactionService.WithdrawAsync(dto, Guid.NewGuid());

            await act.Should().ThrowAsync<UnauthorizedAccessException>();
            _mockTransactionRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Never);
            _mockAccountRepo.Verify(r => r.UpdateAsync(It.IsAny<Account>()), Times.Never);
        }

        [Fact]
        public async Task WithdrawAsync_AccountNotFound_ThrowsUnauthorizedAccessException()
        {
            _mockAccountRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Account?)null);
            var dto = new DepositWithdrawDto { AccountId = Guid.NewGuid(), Amount = 250 };

            var act = async () => await _transactionService.WithdrawAsync(dto, Guid.NewGuid());

            await act.Should().ThrowAsync<UnauthorizedAccessException>();
        }

        [Fact]
        public async Task WithdrawAsync_NullIdempotencyKey_NeverChecksIdempotency()
        {
            var userId = Guid.NewGuid();
            var account = CreateAccount(userId);
            _mockAccountRepo.Setup(r => r.GetByIdAsync(account.Id)).ReturnsAsync(account);
            var dto = new DepositWithdrawDto { AccountId = account.Id, Amount = 250 };

            await _transactionService.WithdrawAsync(dto, userId);

            _mockTransactionRepo.Verify(r => r.GetByIdempotencyKeyAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task TransferAsync_ValidRequest_MovesMoneyBetweenAccounts()
        {
            var userId = Guid.NewGuid();
            var fromAccount = CreateAccount(userId, balance: 1000, accountNumber: "ACC001");
            var toAccount = CreateAccount(Guid.NewGuid(), balance: 1000, accountNumber: "ACC002");
            _mockAccountRepo.Setup(r => r.GetByIdAsync(fromAccount.Id)).ReturnsAsync(fromAccount);
            _mockAccountRepo.Setup(r => r.GetByIdAsync(toAccount.Id)).ReturnsAsync(toAccount);
            var dto = new TransferDto { FromAccountId = fromAccount.Id, ToAccountId = toAccount.Id, Amount = 500 };

            await _transactionService.TransferAsync(dto, userId);

            fromAccount.Balance.Should().Be(500);
            toAccount.Balance.Should().Be(1500);
            _mockTransactionRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Exactly(2));
            _mockAccountRepo.Verify(r => r.UpdateAsync(It.IsAny<Account>()), Times.Exactly(2));
        }

        [Fact]
        public async Task TransferAsync_ValidRequest_CreatesTwoTransactions()
        {
            var userId = Guid.NewGuid();
            var fromAccount = CreateAccount(userId, accountNumber: "ACC001");
            var toAccount = CreateAccount(Guid.NewGuid(), accountNumber: "ACC002");
            _mockAccountRepo.Setup(r => r.GetByIdAsync(fromAccount.Id)).ReturnsAsync(fromAccount);
            _mockAccountRepo.Setup(r => r.GetByIdAsync(toAccount.Id)).ReturnsAsync(toAccount);
            var dto = new TransferDto { FromAccountId = fromAccount.Id, ToAccountId = toAccount.Id, Amount = 200 };

            await _transactionService.TransferAsync(dto, userId);

            _mockTransactionRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Exactly(2));
        }

        [Fact]
        public async Task TransferAsync_InsufficientFunds_ThrowsInvalidOperationException()
        {
            var userId = Guid.NewGuid();
            var fromAccount = CreateAccount(userId, balance: 100, accountNumber: "ACC001");
            var toAccount = CreateAccount(Guid.NewGuid(), accountNumber: "ACC002");
            _mockAccountRepo.Setup(r => r.GetByIdAsync(fromAccount.Id)).ReturnsAsync(fromAccount);
            _mockAccountRepo.Setup(r => r.GetByIdAsync(toAccount.Id)).ReturnsAsync(toAccount);
            var dto = new TransferDto { FromAccountId = fromAccount.Id, ToAccountId = toAccount.Id, Amount = 500 };

            var act = async () => await _transactionService.TransferAsync(dto, userId);

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task TransferAsync_ToAccountNotFound_ThrowsInvalidOperationException()
        {
            var userId = Guid.NewGuid();
            var fromAccount = CreateAccount(userId);
            _mockAccountRepo.Setup(r => r.GetByIdAsync(fromAccount.Id)).ReturnsAsync(fromAccount);
            _mockAccountRepo.Setup(r => r.GetByIdAsync(It.Is<Guid>(id => id != fromAccount.Id))).ReturnsAsync((Account?)null);
            var dto = new TransferDto { FromAccountId = fromAccount.Id, ToAccountId = Guid.NewGuid(), Amount = 500 };

            var act = async () => await _transactionService.TransferAsync(dto, userId);

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task TransferAsync_UnauthorizedUser_ThrowsUnauthorizedAccessException()
        {
            var fromAccount = CreateAccount(Guid.NewGuid(), accountNumber: "ACC001");
            var toAccount = CreateAccount(Guid.NewGuid(), accountNumber: "ACC002");
            _mockAccountRepo.Setup(r => r.GetByIdAsync(fromAccount.Id)).ReturnsAsync(fromAccount);
            _mockAccountRepo.Setup(r => r.GetByIdAsync(toAccount.Id)).ReturnsAsync(toAccount);
            var dto = new TransferDto { FromAccountId = fromAccount.Id, ToAccountId = toAccount.Id, Amount = 500 };

            var act = async () => await _transactionService.TransferAsync(dto, Guid.NewGuid());

            await act.Should().ThrowAsync<UnauthorizedAccessException>();
            _mockTransactionRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Never);
            _mockAccountRepo.Verify(r => r.UpdateAsync(It.IsAny<Account>()), Times.Never);
        }

        [Fact]
        public async Task TransferAsync_FromAccountNotFound_ThrowsUnauthorizedAccessException()
        {
            _mockAccountRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Account?)null);
            var dto = new TransferDto { FromAccountId = Guid.NewGuid(), ToAccountId = Guid.NewGuid(), Amount = 500 };

            var act = async () => await _transactionService.TransferAsync(dto, Guid.NewGuid());

            await act.Should().ThrowAsync<UnauthorizedAccessException>();
        }

        [Fact]
        public async Task TransferExternalAsync_ValidRequest_MovesMoneyBetweenAccounts()
        {
            var userId = Guid.NewGuid();
            var fromAccount = CreateAccount(userId, balance: 1000, accountNumber: "ACC001");
            var toAccount = CreateAccount(Guid.NewGuid(), balance: 500, accountNumber: "ACC002");
            _mockAccountRepo.Setup(r => r.GetByIdAsync(fromAccount.Id)).ReturnsAsync(fromAccount);
            _mockAccountRepo.Setup(r => r.GetByAccountNumberAsync("ACC002")).ReturnsAsync(toAccount);
            var dto = new ExternalTransferDto { FromAccountId = fromAccount.Id, ToAccountNumber = "ACC002", Amount = 300 };

            await _transactionService.TransferExternalAsync(dto, userId);

            fromAccount.Balance.Should().Be(700);
            toAccount.Balance.Should().Be(800);
        }

        [Fact]
        public async Task TransferExternalAsync_ValidRequest_CreatesTwoTransactions()
        {
            var userId = Guid.NewGuid();
            var fromAccount = CreateAccount(userId, accountNumber: "ACC001");
            var toAccount = CreateAccount(Guid.NewGuid(), accountNumber: "ACC002");
            _mockAccountRepo.Setup(r => r.GetByIdAsync(fromAccount.Id)).ReturnsAsync(fromAccount);
            _mockAccountRepo.Setup(r => r.GetByAccountNumberAsync("ACC002")).ReturnsAsync(toAccount);
            var dto = new ExternalTransferDto { FromAccountId = fromAccount.Id, ToAccountNumber = "ACC002", Amount = 200 };

            await _transactionService.TransferExternalAsync(dto, userId);

            _mockTransactionRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Exactly(2));
        }

        [Fact]
        public async Task TransferExternalAsync_DuplicateIdempotencyKey_ReturnsEarlyWithoutUpdating()
        {
            var userId = Guid.NewGuid();
            var fromAccount = CreateAccount(userId);
            _mockAccountRepo.Setup(r => r.GetByIdAsync(fromAccount.Id)).ReturnsAsync(fromAccount);
            _mockTransactionRepo.Setup(r => r.GetByIdempotencyKeyAsync("test-key")).ReturnsAsync(new Transaction());
            var dto = new ExternalTransferDto { FromAccountId = fromAccount.Id, ToAccountNumber = "ACC002", Amount = 300 };

            await _transactionService.TransferExternalAsync(dto, userId, "test-key");

            _mockAccountRepo.Verify(r => r.UpdateAsync(It.IsAny<Account>()), Times.Never);
        }

        [Fact]
        public async Task TransferExternalAsync_UnauthorizedUser_ThrowsUnauthorizedAccessException()
        {
            var fromAccount = CreateAccount(Guid.NewGuid());
            _mockAccountRepo.Setup(r => r.GetByIdAsync(fromAccount.Id)).ReturnsAsync(fromAccount);
            var dto = new ExternalTransferDto { FromAccountId = fromAccount.Id, ToAccountNumber = "ACC002", Amount = 300 };

            var act = async () => await _transactionService.TransferExternalAsync(dto, Guid.NewGuid());

            await act.Should().ThrowAsync<UnauthorizedAccessException>();
        }

        [Fact]
        public async Task TransferExternalAsync_FromAccountNotFound_ThrowsUnauthorizedAccessException()
        {
            _mockAccountRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Account?)null);
            var dto = new ExternalTransferDto { FromAccountId = Guid.NewGuid(), ToAccountNumber = "ACC002", Amount = 300 };

            var act = async () => await _transactionService.TransferExternalAsync(dto, Guid.NewGuid());

            await act.Should().ThrowAsync<UnauthorizedAccessException>();
        }

        [Fact]
        public async Task TransferExternalAsync_ToAccountNotFound_ThrowsInvalidOperationException()
        {
            var userId = Guid.NewGuid();
            var fromAccount = CreateAccount(userId);
            _mockAccountRepo.Setup(r => r.GetByIdAsync(fromAccount.Id)).ReturnsAsync(fromAccount);
            _mockAccountRepo.Setup(r => r.GetByAccountNumberAsync(It.IsAny<string>())).ReturnsAsync((Account?)null);
            var dto = new ExternalTransferDto { FromAccountId = fromAccount.Id, ToAccountNumber = "NOTEXIST", Amount = 300 };

            var act = async () => await _transactionService.TransferExternalAsync(dto, userId);

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task TransferExternalAsync_SameAccount_ThrowsInvalidOperationException()
        {
            var userId = Guid.NewGuid();
            var fromAccount = CreateAccount(userId, accountNumber: "ACC001");
            _mockAccountRepo.Setup(r => r.GetByIdAsync(fromAccount.Id)).ReturnsAsync(fromAccount);
            _mockAccountRepo.Setup(r => r.GetByAccountNumberAsync("ACC001")).ReturnsAsync(fromAccount);
            var dto = new ExternalTransferDto { FromAccountId = fromAccount.Id, ToAccountNumber = "ACC001", Amount = 300 };

            var act = async () => await _transactionService.TransferExternalAsync(dto, userId);

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task TransferExternalAsync_InsufficientFunds_ThrowsInvalidOperationException()
        {
            var userId = Guid.NewGuid();
            var fromAccount = CreateAccount(userId, balance: 100, accountNumber: "ACC001");
            var toAccount = CreateAccount(Guid.NewGuid(), accountNumber: "ACC002");
            _mockAccountRepo.Setup(r => r.GetByIdAsync(fromAccount.Id)).ReturnsAsync(fromAccount);
            _mockAccountRepo.Setup(r => r.GetByAccountNumberAsync("ACC002")).ReturnsAsync(toAccount);
            var dto = new ExternalTransferDto { FromAccountId = fromAccount.Id, ToAccountNumber = "ACC002", Amount = 500 };

            var act = async () => await _transactionService.TransferExternalAsync(dto, userId);

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task GetTransactionsByAccountIdAsync_ValidRequest_ReturnsPagedResult()
        {
            var userId = Guid.NewGuid();
            var account = CreateAccount(userId);
            _mockAccountRepo.Setup(r => r.GetByIdAsync(account.Id)).ReturnsAsync(account);
            _mockTransactionRepo.Setup(r => r.GetByAccountIdAsync(account.Id, 1, 10, null)).ReturnsAsync(new List<Transaction>());
            _mockTransactionRepo.Setup(r => r.GetTotalCountAsync(account.Id, null)).ReturnsAsync(0);

            var result = await _transactionService.GetTransactionsByAccountIdAsync(account.Id, userId, 1, 10);

            result.Should().NotBeNull();
            result.TotalCount.Should().Be(0);
        }

        [Fact]
        public async Task GetTransactionsByAccountIdAsync_ValidRequest_ReturnsCorrectPaginationMetadata()
        {
            var userId = Guid.NewGuid();
            var account = CreateAccount(userId);
            _mockAccountRepo.Setup(r => r.GetByIdAsync(account.Id)).ReturnsAsync(account);
            _mockTransactionRepo.Setup(r => r.GetByAccountIdAsync(account.Id, 1, 10, null)).ReturnsAsync(new List<Transaction>());
            _mockTransactionRepo.Setup(r => r.GetTotalCountAsync(account.Id, null)).ReturnsAsync(42);

            var result = await _transactionService.GetTransactionsByAccountIdAsync(account.Id, userId, 1, 10);

            result.TotalCount.Should().Be(42);
            result.Page.Should().Be(1);
            result.PageSize.Should().Be(10);
        }

        [Fact]
        public async Task GetTransactionsByAccountIdAsync_UnauthorizedUser_ThrowsUnauthorizedAccessException()
        {
            var account = CreateAccount(Guid.NewGuid());
            _mockAccountRepo.Setup(r => r.GetByIdAsync(account.Id)).ReturnsAsync(account);

            var act = async () => await _transactionService.GetTransactionsByAccountIdAsync(account.Id, Guid.NewGuid(), 1, 10);

            await act.Should().ThrowAsync<UnauthorizedAccessException>();
            _mockTransactionRepo.Verify(r => r.GetByAccountIdAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetTransactionsByDateRangeAsync_ValidRequest_ReturnsTransactions()
        {
            var userId = Guid.NewGuid();
            var account = CreateAccount(userId);
            _mockAccountRepo.Setup(r => r.GetByIdAsync(account.Id)).ReturnsAsync(account);
            _mockTransactionRepo.Setup(r => r.GetByAccountIdAndDateRangeAsync(account.Id, It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(new List<Transaction>());

            var result = await _transactionService.GetTransactionsByDateRangeAsync(account.Id, userId, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

            result.Should().NotBeNull();
        }

        [Fact]
        public async Task GetTransactionsByDateRangeAsync_UnauthorizedUser_ThrowsUnauthorizedAccessException()
        {
            var account = CreateAccount(Guid.NewGuid());
            _mockAccountRepo.Setup(r => r.GetByIdAsync(account.Id)).ReturnsAsync(account);

            var act = async () => await _transactionService.GetTransactionsByDateRangeAsync(account.Id, Guid.NewGuid(), DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

            await act.Should().ThrowAsync<UnauthorizedAccessException>();
        }
    }
}