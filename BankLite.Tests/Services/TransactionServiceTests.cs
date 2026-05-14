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
        private readonly TransactionService _transactionService;

        public TransactionServiceTests()
        {
            _mockAccountRepo = new Mock<IAccountRepository>();
            _mockTransactionRepo = new Mock<ITransactionRepository>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockAuditLogRepo = new Mock<IAuditLogRepository>();

            _transactionService = new TransactionService(_mockAccountRepo.Object, _mockTransactionRepo.Object, _mockUnitOfWork.Object, _mockAuditLogRepo.Object, new NullLogger<TransactionService>());

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
    }
}
