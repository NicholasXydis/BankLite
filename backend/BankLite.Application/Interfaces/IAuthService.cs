using BankLite.Application.DTOs;

namespace BankLite.Application.Interfaces;

public interface IAuthService
{
    Task<(string Token, string RefreshToken, AuthResponseDto Response)> RegisterAsync(RegisterUserDto dto);
    Task<(string Token, string RefreshToken, AuthResponseDto Response)> LoginAsync(LoginUserDto dto);
    Task<(string Token, string RefreshToken, AuthResponseDto Response)> RefreshAsync(string refreshToken);
    Task RevokeRefreshTokenAsync(string refreshToken);
    Task ForgotPasswordAsync(string email, string resetBaseUrl, string lang = "en");
    Task ResetPasswordAsync(string token, string newPassword);
}