using BankLite.Application.DTOs;
using BankLite.Application.Interfaces;
using BankLite.Domain.Entities;
using BankLite.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace BankLite.Application.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly ILogger<TransactionService> _logger;

        public TransactionService(IAccountRepository accountRepository, ITransactionRepository transactionRepository, IUnitOfWork unitOfWork, IAuditLogRepository auditLogRepository, ILogger<TransactionService> logger)
        {
            _accountRepository = accountRepository;
            _transactionRepository = transactionRepository;
            _unitOfWork = unitOfWork;
            _auditLogRepository = auditLogRepository;
            _logger = logger;
        }

        public async Task<Transaction> DepositAsync(DepositWithdrawDto dto, Guid userId)
        {
            var account = await _accountRepository.GetByIdAsync(dto.AccountId);
            if (account == null || account.UserId != userId)
            {
                _logger.LogWarning("Unauthorized deposit attempt by user {UserId} on account {AccountId}", userId, dto.AccountId);
                throw new UnauthorizedAccessException("You do not have access to this account.");
            }

            account.Balance += dto.Amount;

            var transaction = new Transaction
            {
                AccountId = dto.AccountId,
                Amount = dto.Amount,
                Type = TransactionType.Deposit,
                Description = $"Deposit of ${dto.Amount:F2}"
            };

            await _transactionRepository.AddAsync(transaction);
            await _accountRepository.UpdateAsync(account);
            await _unitOfWork.SaveAsync();
            _logger.LogInformation("Deposit of {Amount} to account {AccountId} by user {UserId}", dto.Amount, dto.AccountId, userId);

            await _auditLogRepository.LogAsync(new AuditLog
            {
                Action = "Deposit",
                Details = $"User {userId} deposited {dto.Amount} to account {dto.AccountId}",
                PerformedAt = DateTime.UtcNow,
            });

            return transaction;
        }

        public async Task<Transaction> WithdrawAsync(DepositWithdrawDto dto, Guid userId)
        {
            var account = await _accountRepository.GetByIdAsync(dto.AccountId);
            if (account == null || account.UserId != userId)
            {
                _logger.LogWarning("Unauthorized withdrawal attempt by user {UserId} on account {AccountId}", userId, dto.AccountId);
                throw new UnauthorizedAccessException("You do not have access to this account.");
            }

            if (dto.Amount > account.Balance)
            {
                _logger.LogWarning("Insufficient funds for withdrawal by user {UserId} on account {AccountId}", userId, dto.AccountId);
                throw new InvalidOperationException("Insufficient Funds");
            }

            account.Balance -= dto.Amount;

            var transaction = new Transaction
            {
                AccountId = dto.AccountId,
                Amount = dto.Amount,
                Type = TransactionType.Withdrawal,
                Description = $"Withdrawal of ${dto.Amount:F2}"
            };

            await _transactionRepository.AddAsync(transaction);
            await _accountRepository.UpdateAsync(account);
            await _unitOfWork.SaveAsync();
            _logger.LogInformation("Withdrawal of {Amount} from account {AccountId} by user {UserId}", dto.Amount, dto.AccountId, userId);

            await _auditLogRepository.LogAsync(new AuditLog
            {
                Action = "Withdrawal",
                Details = $"User {userId} withdrew {dto.Amount} from account {dto.AccountId}",
                PerformedAt = DateTime.UtcNow,
            });

            return transaction;
        }

        public async Task TransferAsync(TransferDto dto, Guid userId)
        {
            var fromAccount = await _accountRepository.GetByIdAsync(dto.FromAccountId);
            var toAccount = await _accountRepository.GetByIdAsync(dto.ToAccountId);
            if (fromAccount == null || fromAccount.UserId != userId)
            {
                _logger.LogWarning("Unauthorized transfer attempt by user {UserId} from account {AccountId}", userId, dto.FromAccountId);
                throw new UnauthorizedAccessException("You do not have access to this account.");
            }
            if (toAccount == null) throw new InvalidOperationException("To Account Not Found");
            if (dto.Amount > fromAccount.Balance) throw new InvalidOperationException("Insufficient Funds");

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                fromAccount.Balance -= dto.Amount;
                toAccount.Balance += dto.Amount;
                await _accountRepository.UpdateAsync(fromAccount);
                await _accountRepository.UpdateAsync(toAccount);

                var debitTransaction = new Transaction
                {
                    AccountId = dto.FromAccountId,
                    Amount = dto.Amount,
                    Type = TransactionType.Withdrawal,
                    Description = $"Transfer to account {toAccount.AccountNumber}"
                };

                var creditTransaction = new Transaction
                {
                    AccountId = dto.ToAccountId,
                    Amount = dto.Amount,
                    Type = TransactionType.Deposit,
                    Description = $"Transfer from account {fromAccount.AccountNumber}"
                };

                await _transactionRepository.AddAsync(debitTransaction);
                await _transactionRepository.AddAsync(creditTransaction);
            });

            _logger.LogInformation("Transfer of {Amount} from account {FromAccountId} to account {ToAccountId} by user {UserId}", dto.Amount, dto.FromAccountId, dto.ToAccountId, userId);

            await _auditLogRepository.LogAsync(new AuditLog
            {
                Action = "Transfer",
                Details = $"User {userId} transferred {dto.Amount} from account {dto.FromAccountId} to account {dto.ToAccountId}",
                PerformedAt = DateTime.UtcNow,
            });
        }

        public async Task TransferExternalAsync(ExternalTransferDto dto, Guid userId)
        {
            var fromAccount = await _accountRepository.GetByIdAsync(dto.FromAccountId);
            if (fromAccount == null || fromAccount.UserId != userId)
            {
                _logger.LogWarning("Unauthorized external transfer attempt by user {UserId} from account {AccountId}", userId, dto.FromAccountId);
                throw new UnauthorizedAccessException("You do not have access to this account.");
            }

            var toAccount = await _accountRepository.GetByAccountNumberAsync(dto.ToAccountNumber);
            if (toAccount == null) throw new InvalidOperationException("Account number not found.");
            if (toAccount.Id == fromAccount.Id) throw new InvalidOperationException("Cannot transfer to the same account.");
            if (dto.Amount > fromAccount.Balance) throw new InvalidOperationException("Insufficient Funds");

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                fromAccount.Balance -= dto.Amount;
                toAccount.Balance += dto.Amount;
                await _accountRepository.UpdateAsync(fromAccount);
                await _accountRepository.UpdateAsync(toAccount);

                var debitTransaction = new Transaction
                {
                    AccountId = dto.FromAccountId,
                    Amount = dto.Amount,
                    Type = TransactionType.Withdrawal,
                    Description = $"Transfer to account {toAccount.AccountNumber}"
                };

                var creditTransaction = new Transaction
                {
                    AccountId = toAccount.Id,
                    Amount = dto.Amount,
                    Type = TransactionType.Deposit,
                    Description = $"Transfer from account {fromAccount.AccountNumber}"
                };

                await _transactionRepository.AddAsync(debitTransaction);
                await _transactionRepository.AddAsync(creditTransaction);
            });

            _logger.LogInformation("External transfer of {Amount} from account {FromAccountId} to account number {ToAccountNumber} by user {UserId}", dto.Amount, dto.FromAccountId, dto.ToAccountNumber, userId);

            await _auditLogRepository.LogAsync(new AuditLog
            {
                Action = "ExternalTransfer",
                Details = $"User {userId} transferred {dto.Amount} from account {dto.FromAccountId} to account number {dto.ToAccountNumber}",
                PerformedAt = DateTime.UtcNow,
            });
        }

        public async Task<PagedResultDto<Transaction>> GetTransactionsByAccountIdAsync(Guid accountId, Guid userId, int page, int pageSize)
        {
            var account = await _accountRepository.GetByIdAsync(accountId);
            if (account == null || account.UserId != userId)
            {
                _logger.LogWarning("Unauthorized transaction history access by user {UserId} on account {AccountId}", userId, accountId);
                throw new UnauthorizedAccessException("You do not have access to this account.");
            }

            var transactions = await _transactionRepository.GetByAccountIdAsync(accountId, page, pageSize);
            var totalCount = await _transactionRepository.GetTotalCountAsync(accountId);
            return new PagedResultDto<Transaction>
            {
                Items = transactions,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
            };
        }
    }
}