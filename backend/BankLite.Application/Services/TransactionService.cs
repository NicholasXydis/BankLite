using System.Security.Cryptography;
using System.Text;
using BankLite.Application.DTOs;
using BankLite.Application.Exceptions;
using BankLite.Application.Interfaces;
using BankLite.Domain.Entities;
using BankLite.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace BankLite.Application.Services;

public class TransactionService : ITransactionService
{
    private readonly IAccountRepository _accountRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IBalanceNotifier _balanceNotifier;
    private readonly ILogger<TransactionService> _logger;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public TransactionService(IAccountRepository accountRepository, ITransactionRepository transactionRepository,
        IUnitOfWork unitOfWork, IAuditLogRepository auditLogRepository, ILogger<TransactionService> logger,
        IBalanceNotifier balanceNotifier)
    {
        _accountRepository = accountRepository;
        _transactionRepository = transactionRepository;
        _unitOfWork = unitOfWork;
        _auditLogRepository = auditLogRepository;
        _logger = logger;
        _balanceNotifier = balanceNotifier;
    }

    public async Task<TransactionResponseDto> DepositAsync(DepositWithdrawDto dto, Guid userId,
        string? idempotencyKey = null)
    {
        Account? account = null;
        Transaction? transaction = null;
        TransactionResponseDto? existingResult = null;
        var scopedIdempotencyKey = CreateScopedIdempotencyKey(userId, nameof(DepositAsync), idempotencyKey);

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            account = await GetOwnedAccountOrThrowAsync(dto.AccountId, userId, "deposit");
            var description = $"Deposit of ${dto.Amount:F2}";
            existingResult = await TryGetIdempotentReplayAsync(
                scopedIdempotencyKey, account.Id, dto.Amount, TransactionType.Deposit, description);
            if (existingResult != null) return;

            account.Balance += dto.Amount;

            transaction = new Transaction
            {
                AccountId = dto.AccountId,
                Amount = dto.Amount,
                Type = TransactionType.Deposit,
                Description = description,
                IdempotencyKey = scopedIdempotencyKey
            };

            await _transactionRepository.AddAsync(transaction);
            await _accountRepository.UpdateAsync(account);
            await _auditLogRepository.LogAsync(CreateAuditLog(userId, "Deposit",
                $"User {userId} deposited {dto.Amount} to account {dto.AccountId}"));
        });

        if (existingResult != null) return existingResult;

        if (account == null || transaction == null) throw new BadRequestException("Deposit failed.");

        await _balanceNotifier.NotifyBalanceUpdatedAsync(userId, account.Id, account.Balance);
        _logger.LogInformation("Deposit completed for account {AccountId}", account.Id);

        return MapToDto(transaction);
    }

    public async Task<TransactionResponseDto> WithdrawAsync(DepositWithdrawDto dto, Guid userId,
        string? idempotencyKey = null)
    {
        Account? account = null;
        Transaction? transaction = null;
        TransactionResponseDto? existingResult = null;
        var scopedIdempotencyKey = CreateScopedIdempotencyKey(userId, nameof(WithdrawAsync), idempotencyKey);

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            account = await GetOwnedAccountOrThrowAsync(dto.AccountId, userId, "withdrawal");

            if (dto.Amount > account.Balance)
            {
                _logger.LogWarning("Withdrawal rejected due to insufficient funds");
                throw new BadRequestException("Insufficient Funds");
            }

            var description = $"Withdrawal of ${dto.Amount:F2}";
            existingResult = await TryGetIdempotentReplayAsync(
                scopedIdempotencyKey, account.Id, dto.Amount, TransactionType.Withdrawal, description);
            if (existingResult != null) return;

            account.Balance -= dto.Amount;

            transaction = new Transaction
            {
                AccountId = dto.AccountId,
                Amount = dto.Amount,
                Type = TransactionType.Withdrawal,
                Description = description,
                IdempotencyKey = scopedIdempotencyKey
            };

            await _transactionRepository.AddAsync(transaction);
            await _accountRepository.UpdateAsync(account);
            await _auditLogRepository.LogAsync(CreateAuditLog(userId, "Withdrawal",
                $"User {userId} withdrew {dto.Amount} from account {dto.AccountId}"));
        });

        if (existingResult != null) return existingResult;

        if (account == null || transaction == null) throw new BadRequestException("Withdrawal failed.");

        await _balanceNotifier.NotifyBalanceUpdatedAsync(userId, account.Id, account.Balance);
        _logger.LogInformation("Withdrawal completed for account {AccountId}", account.Id);

        return MapToDto(transaction);
    }

    public async Task TransferAsync(TransferDto dto, Guid userId, string? idempotencyKey = null)
    {
        Account? fromAccount = null;
        Account? toAccount = null;
        var idempotentReplay = false;
        var scopedIdempotencyKey = CreateScopedIdempotencyKey(userId, nameof(TransferAsync), idempotencyKey);

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            fromAccount = await GetOwnedAccountOrThrowAsync(dto.FromAccountId, userId, "transfer");
            toAccount = await _accountRepository.GetByIdAsync(dto.ToAccountId);

            if (toAccount == null) throw new BadRequestException("To Account Not Found");
            if (toAccount.UserId != userId)
                throw new UnauthorizedAppException("You do not have access to this account.");
            if (dto.Amount > fromAccount.Balance) throw new BadRequestException("Insufficient Funds");

            var debitDescription = $"Internal Transfer to account {toAccount.Id}";
            idempotentReplay = await IsIdempotentReplayAsync(
                scopedIdempotencyKey, fromAccount.Id, dto.Amount, TransactionType.Transfer, debitDescription);
            if (idempotentReplay) return;

            fromAccount.Balance -= dto.Amount;
            toAccount.Balance += dto.Amount;
            await _accountRepository.UpdateAsync(fromAccount);
            await _accountRepository.UpdateAsync(toAccount);

            var debitTransaction = new Transaction
            {
                AccountId = dto.FromAccountId,
                Amount = dto.Amount,
                Type = TransactionType.Transfer,
                Description = debitDescription,
                IdempotencyKey = scopedIdempotencyKey
            };

            var creditTransaction = new Transaction
            {
                AccountId = dto.ToAccountId,
                Amount = dto.Amount,
                Type = TransactionType.Transfer,
                Description = $"Internal Transfer from account {fromAccount.Id}"
            };

            await _transactionRepository.AddAsync(debitTransaction);
            await _transactionRepository.AddAsync(creditTransaction);

            await _auditLogRepository.LogAsync(CreateAuditLog(userId, "Transfer",
                $"User {userId} transferred {dto.Amount} from account {dto.FromAccountId} to account {dto.ToAccountId}"));
        });

        if (idempotentReplay) return;

        if (fromAccount == null || toAccount == null) throw new BadRequestException("Transfer failed.");

        await _balanceNotifier.NotifyBalanceUpdatedAsync(userId, fromAccount.Id, fromAccount.Balance);
        await _balanceNotifier.NotifyBalanceUpdatedAsync(userId, toAccount.Id, toAccount.Balance);

        _logger.LogInformation(
            "Internal transfer completed from account {FromAccountId} to account {ToAccountId}", fromAccount.Id,
            toAccount.Id);
    }

    public async Task TransferExternalAsync(ExternalTransferDto dto, Guid userId, string? idempotencyKey = null)
    {
        Account? fromAccount = null;
        Account? toAccount = null;
        var idempotentReplay = false;
        var scopedIdempotencyKey =
            CreateScopedIdempotencyKey(userId, nameof(TransferExternalAsync), idempotencyKey);

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            fromAccount = await GetOwnedAccountOrThrowAsync(dto.FromAccountId, userId, "external transfer");
            toAccount = await _accountRepository.GetByAccountNumberAsync(dto.ToAccountNumber);
            if (toAccount == null) throw new BadRequestException("Account number not found.");
            if (toAccount.Id == fromAccount.Id)
                throw new BadRequestException("Cannot transfer to the same account.");
            if (dto.Amount > fromAccount.Balance) throw new BadRequestException("Insufficient Funds");

            var debitDescription = $"External Transfer to account {toAccount.Id}";
            idempotentReplay = await IsIdempotentReplayAsync(
                scopedIdempotencyKey, fromAccount.Id, dto.Amount, TransactionType.Transfer, debitDescription);
            if (idempotentReplay) return;

            fromAccount.Balance -= dto.Amount;
            toAccount.Balance += dto.Amount;
            await _accountRepository.UpdateAsync(fromAccount);
            await _accountRepository.UpdateAsync(toAccount);

            var debitTransaction = new Transaction
            {
                AccountId = dto.FromAccountId,
                Amount = dto.Amount,
                Type = TransactionType.Transfer,
                Description = debitDescription,
                IdempotencyKey = scopedIdempotencyKey
            };

            var creditTransaction = new Transaction
            {
                AccountId = toAccount.Id,
                Amount = dto.Amount,
                Type = TransactionType.Transfer,
                Description = $"External Transfer from account {fromAccount.Id}"
            };

            await _transactionRepository.AddAsync(debitTransaction);
            await _transactionRepository.AddAsync(creditTransaction);

            await _auditLogRepository.LogAsync(CreateAuditLog(userId, "ExternalTransfer",
                $"User {userId} transferred {dto.Amount} from account {dto.FromAccountId} to account {toAccount.Id}"));
        });

        if (idempotentReplay) return;

        if (fromAccount == null || toAccount == null) throw new BadRequestException("External transfer failed.");

        await _balanceNotifier.NotifyBalanceUpdatedAsync(userId, fromAccount.Id, fromAccount.Balance);
        await _balanceNotifier.NotifyBalanceUpdatedAsync(toAccount.UserId, toAccount.Id, toAccount.Balance);

        _logger.LogInformation(
            "External transfer completed from account {FromAccountId} to account {ToAccountId}", fromAccount.Id,
            toAccount.Id);
    }

    public async Task<PagedResultDto<TransactionResponseDto>> GetTransactionsByAccountIdAsync(Guid accountId,
        Guid userId, int page, int pageSize, string? type = null)
    {
        await GetOwnedAccountOrThrowAsync(accountId, userId, "transaction history access");
        var transactions = await _transactionRepository.GetByAccountIdAsync(accountId, page, pageSize, type);
        var totalCount = await _transactionRepository.GetTotalCountAsync(accountId, type);
        return new PagedResultDto<TransactionResponseDto>
        {
            Items = transactions.Select(MapToDto),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<IEnumerable<TransactionResponseDto>> GetTransactionsByDateRangeAsync(Guid accountId, Guid userId,
        DateTime startDate, DateTime endDate)
    {
        await GetOwnedAccountOrThrowAsync(accountId, userId, "date range transaction access");
        var transactions = await _transactionRepository.GetByAccountIdAndDateRangeAsync(accountId, startDate, endDate);
        return transactions.Select(MapToDto);
    }

    private async Task<Account> GetOwnedAccountOrThrowAsync(Guid accountId, Guid userId, string operation)
    {
        var account = await _accountRepository.GetByIdAsync(accountId);
        if (account == null || account.UserId != userId)
        {
            _logger.LogWarning("Unauthorized {Operation} attempt rejected", operation);
            throw new UnauthorizedAppException("You do not have access to this account.");
        }

        return account;
    }

    private async Task<bool> IsIdempotentReplayAsync(
        string? scopedIdempotencyKey,
        Guid accountId,
        decimal amount,
        TransactionType type,
        string description)
    {
        return await TryGetIdempotentReplayAsync(scopedIdempotencyKey, accountId, amount, type, description) != null;
    }

    private async Task<TransactionResponseDto?> TryGetIdempotentReplayAsync(
        string? scopedIdempotencyKey,
        Guid accountId,
        decimal amount,
        TransactionType type,
        string description)
    {
        if (scopedIdempotencyKey == null) return null;

        var existing = await _transactionRepository.GetByIdempotencyKeyAsync(scopedIdempotencyKey);
        if (existing == null) return null;

        EnsureReplayMatches(existing, accountId, amount, type, description);
        return MapToDto(existing);
    }

    private static AuditLog CreateAuditLog(Guid userId, string action, string details)
    {
        return new AuditLog
        {
            UserId = userId,
            Action = action,
            Details = details,
            PerformedAt = DateTime.UtcNow
        };
    }

    private static TransactionResponseDto MapToDto(Transaction t)
    {
        return new TransactionResponseDto
        {
            Id = t.Id,
            AccountId = t.AccountId,
            Amount = t.Amount,
            Type = t.Type.ToString(),
            Description = t.Description,
            CreatedAt = t.CreatedAt
        };
    }

    private static string? CreateScopedIdempotencyKey(Guid userId, string operation, string? idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey)) return null;

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{userId:N}:{operation}:{idempotencyKey}"));
        return Convert.ToBase64String(bytes);
    }

    private static void EnsureReplayMatches(
        Transaction existing,
        Guid accountId,
        decimal amount,
        TransactionType type,
        string description)
    {
        if (existing.AccountId != accountId ||
            existing.Amount != amount ||
            existing.Type != type ||
            existing.Description != description)
            throw new BadRequestException("Idempotency key was already used for a different transaction.");
    }
}