using BankLite.Application.DTOs;

namespace BankLite.Application.Interfaces
{
    public interface IAuthService
    {
        Task<(string Token, AuthResponseDto Response)> RegisterAsync(RegisterUserDto dto);
        Task<(string Token, AuthResponseDto Response)> LoginAsync(LoginUserDto dto);
    }
}
