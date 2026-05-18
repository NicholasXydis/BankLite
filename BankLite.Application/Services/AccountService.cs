using BankLite.Application.DTOs;
using BankLite.Application.Interfaces;
using BankLite.Domain.Entities;
using BankLite.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace BankLite.Application.Services
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AccountService> _logger;

        public AccountService(IAccountRepository accountRepository, IUnitOfWork unitOfWork, ILogger<AccountService> logger)
        {
            _accountRepository = accountRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Account> CreateAccountAsync(CreateAccountDto dto, Guid userId)
        {

            var existingAccounts = await _accountRepository.GetByUserIdAsync(userId);
            if (existingAccounts.Any(a => a.Type == dto.Type))
                throw new InvalidOperationException($"You already have a {dto.Type} account.");

            var account = new Account
            {
                UserId = userId,
                Type = dto.Type,
                AccountNumber = Guid.NewGuid().ToString("N")[..12].ToUpper()
            };

            await _accountRepository.AddAsync(account);
            await _unitOfWork.SaveAsync();
            _logger.LogInformation("Account created for user {UserId}: {AccountNumber}", userId, account.AccountNumber);
            return account;
        }

        public async Task<IEnumerable<Account>> GetAccountsByUserIdAsync(Guid userId)
        {
            _logger.LogInformation("Fetching accounts for user {UserId}", userId);
            return await _accountRepository.GetByUserIdAsync(userId);
        }
    }
}
