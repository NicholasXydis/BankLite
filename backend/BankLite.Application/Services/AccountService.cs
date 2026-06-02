using System.Security.Cryptography;
using BankLite.Application.DTOs;
using BankLite.Application.Exceptions;
using BankLite.Application.Interfaces;
using BankLite.Domain.Entities;
using BankLite.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace BankLite.Application.Services;

public class AccountService : IAccountService
{
    private readonly IAccountRepository _accountRepository;
    private readonly ILogger<AccountService> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public AccountService(IAccountRepository accountRepository, IUnitOfWork unitOfWork, ILogger<AccountService> logger)
    {
        _accountRepository = accountRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AccountResponseDto> CreateAccountAsync(CreateAccountDto dto, Guid userId)
    {
        if (await _accountRepository.ExistsByUserIdAndTypeAsync(userId, dto.Type))
            throw new BadRequestException($"You already have a {dto.Type} account.");

        var account = new Account
        {
            UserId = userId,
            Type = dto.Type,
            AccountNumber = await GenerateUniqueAccountNumberAsync()
        };

        await _accountRepository.AddAsync(account);
        await _unitOfWork.SaveAsync();
        _logger.LogInformation("Account created for user {UserId}: {AccountId}", userId, account.Id);
        return new AccountResponseDto
        {
            Id = account.Id,
            AccountNumber = account.AccountNumber,
            Type = account.Type.ToString(),
            Balance = account.Balance,
            CreatedAt = account.CreatedAt
        };
    }

    public async Task<IEnumerable<AccountResponseDto>> GetAccountsByUserIdAsync(Guid userId)
    {
        var accounts = await _accountRepository.GetByUserIdAsync(userId);
        return accounts.Select(a => new AccountResponseDto
        {
            Id = a.Id,
            AccountNumber = a.AccountNumber,
            Type = a.Type.ToString(),
            Balance = a.Balance,
            CreatedAt = a.CreatedAt
        });
    }

    private async Task<string> GenerateUniqueAccountNumberAsync()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        using var random = RandomNumberGenerator.Create();
        string accountNumber;
        var bytes = new byte[12];
        do
        {
            random.GetBytes(bytes);
            accountNumber = new string(bytes.Select(b => chars[b % chars.Length]).ToArray());
        } while (await _accountRepository.ExistsByAccountNumberAsync(accountNumber));

        return accountNumber;
    }
}