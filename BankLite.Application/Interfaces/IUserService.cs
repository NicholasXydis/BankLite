using BankLite.Application.DTOs;

namespace BankLite.Application.Interfaces
{
    public interface IUserService
    {
        Task<UserProfileDto> GetProfileAsync(Guid userId);
        Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto);
        Task DeleteAccountAsync(Guid userId);
    }
}