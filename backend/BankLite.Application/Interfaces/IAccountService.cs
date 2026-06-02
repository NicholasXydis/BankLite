using BankLite.Application.DTOs;

namespace BankLite.Application.Interfaces;

public interface IAccountService
{
    Task<AccountResponseDto> CreateAccountAsync(CreateAccountDto dto, Guid userId);
    Task<IEnumerable<AccountResponseDto>> GetAccountsByUserIdAsync(Guid userId);
}