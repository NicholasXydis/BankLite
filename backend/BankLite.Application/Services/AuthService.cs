using System.Security.Cryptography;
using System.Text;
using BankLite.Application.DTOs;
using BankLite.Application.Exceptions;
using BankLite.Application.Interfaces;
using BankLite.Domain.Entities;
using BankLite.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace BankLite.Application.Services;

public class AuthService : IAuthService
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthService> _logger;
    private readonly IPasswordResetRepository _passwordResetRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;

    public AuthService(IUserRepository userRepository, ITokenService tokenService,
        IAuditLogRepository auditLogRepository, ILogger<AuthService> logger,
        IRefreshTokenRepository refreshTokenRepository, IPasswordResetRepository passwordResetRepository,
        IEmailService emailService, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _auditLogRepository = auditLogRepository;
        _logger = logger;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordResetRepository = passwordResetRepository;
        _emailService = emailService;
        _unitOfWork = unitOfWork;
    }

    public async Task<(string Token, string RefreshToken, AuthResponseDto Response)> RegisterAsync(RegisterUserDto dto)
    {
        var normalizedEmail = dto.Email.ToLower();
        if (await _userRepository.ExistsAsync(normalizedEmail))
        {
            _logger.LogWarning("Registration failed because the email is already registered");
            throw new BadRequestException("Email already registered");
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = dto.FullName,
            Email = normalizedEmail,
            PasswordHash = passwordHash
        };

        var refreshToken = string.Empty;
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _userRepository.AddAsync(user);
            await _auditLogRepository.LogAsync(new AuditLog
            {
                UserId = user.Id,
                Action = "Register",
                Details = $"User {user.Id} registered",
                PerformedAt = DateTime.UtcNow
            });
            refreshToken = await GenerateRefreshTokenAsync(user.Id, false);
        });

        _logger.LogInformation("User registered successfully: {UserId}", user.Id);

        var token = _tokenService.GenerateAccessToken(user);
        return (token, refreshToken, new AuthResponseDto
        {
            UserId = user.Id,
            FullName = user.FullName,
            ExpiresAt = _tokenService.GetAccessTokenExpiry()
        });
    }

    public async Task<(string Token, string RefreshToken, AuthResponseDto Response)> LoginAsync(LoginUserDto dto)
    {
        User? user = null;
        var refreshToken = string.Empty;
        string? rejectionMessage = null;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            user = await _userRepository.GetByEmailAsync(dto.Email.ToLower());
            if (user == null)
            {
                _logger.LogWarning("Login failed because the user was not found");
                rejectionMessage = "Invalid Credentials";
                return;
            }

            if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
            {
                _logger.LogWarning("Login attempt rejected for locked user {UserId}", user.Id);
                rejectionMessage = "Account is locked. Please try again later.";
                return;
            }

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            {
                user.FailedLoginAttempts++;
                if (user.FailedLoginAttempts >= 5)
                {
                    user.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
                    user.FailedLoginAttempts = 0;
                    _logger.LogWarning("User {UserId} locked due to failed login attempts", user.Id);
                }

                await _userRepository.UpdateAsync(user);
                rejectionMessage = "Invalid Credentials";
                return;
            }

            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
            user.LastLoginAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            await _auditLogRepository.LogAsync(new AuditLog
            {
                UserId = user.Id,
                Action = "Login",
                Details = $"User {user.Id} logged in",
                PerformedAt = DateTime.UtcNow
            });
            refreshToken = await GenerateRefreshTokenAsync(user.Id, false);
        });

        if (rejectionMessage != null || user == null)
            throw new BadRequestException(rejectionMessage ?? "Invalid Credentials");

        _logger.LogInformation("User logged in successfully: {UserId}", user.Id);

        var token = _tokenService.GenerateAccessToken(user);
        return (token, refreshToken, new AuthResponseDto
        {
            UserId = user.Id,
            FullName = user.FullName,
            ExpiresAt = _tokenService.GetAccessTokenExpiry()
        });
    }

    public async Task<(string Token, string RefreshToken, AuthResponseDto Response)> RefreshAsync(string refreshToken)
    {
        User? refreshedUser = null;
        var newRefreshToken = string.Empty;
        var isInvalid = false;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var existing = await _refreshTokenRepository.GetByTokenAsync(HashToken(refreshToken));
            if (existing == null || existing.IsRevoked || existing.ExpiresAt < DateTime.UtcNow)
            {
                isInvalid = true;
                return;
            }

            await _refreshTokenRepository.RevokeAsync(existing);
            newRefreshToken = await GenerateRefreshTokenAsync(existing.UserId, false);
            refreshedUser = existing.User;
        });

        if (isInvalid || refreshedUser == null) throw new UnauthorizedAppException("Invalid or expired refresh token.");

        var token = _tokenService.GenerateAccessToken(refreshedUser);
        return (token, newRefreshToken, new AuthResponseDto
        {
            UserId = refreshedUser.Id,
            FullName = refreshedUser.FullName,
            ExpiresAt = _tokenService.GetAccessTokenExpiry()
        });
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken)
    {
        var existing = await _refreshTokenRepository.GetByTokenAsync(HashToken(refreshToken));
        if (existing is { IsRevoked: false })
        {
            await _refreshTokenRepository.RevokeAsync(existing);
            await _unitOfWork.SaveAsync();
        }
    }

    public async Task ForgotPasswordAsync(string email, string resetBaseUrl, string lang = "en")
    {
        var user = await _userRepository.GetByEmailAsync(email.ToLower());
        if (user == null) return;

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var resetToken = new PasswordResetToken
        {
            UserId = user.Id,
            Token = HashToken(token),
            ExpiresAt = DateTime.UtcNow.AddMinutes(15)
        };

        await _passwordResetRepository.AddAsync(resetToken);
        await _unitOfWork.SaveAsync();

        var resetLink = $"{resetBaseUrl}?token={Uri.EscapeDataString(token)}";
        await _emailService.SendPasswordResetEmailAsync(user.Email, resetLink, lang);

        _logger.LogInformation("Password reset email queued for user {UserId}", user.Id);
    }

    public async Task ResetPasswordAsync(string token, string newPassword)
    {
        var hashedToken = HashToken(token);
        var userId = Guid.Empty;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var resetToken = await _passwordResetRepository.GetByTokenAsync(hashedToken);
            if (resetToken == null || resetToken.IsUsed || resetToken.ExpiresAt < DateTime.UtcNow)
                throw new BadRequestException("Invalid or expired reset token.");

            resetToken.IsUsed = true;
            await _passwordResetRepository.UpdateAsync(resetToken);
            resetToken.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _userRepository.UpdateAsync(resetToken.User);
            userId = resetToken.UserId;
        });

        _logger.LogInformation("Password reset successful for user {UserId}", userId);
    }

    private async Task<string> GenerateRefreshTokenAsync(Guid userId, bool saveImmediately = true)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var refreshToken = new RefreshToken
        {
            UserId = userId,
            Token = HashToken(token),
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };
        await _refreshTokenRepository.AddAsync(refreshToken);
        if (saveImmediately) await _unitOfWork.SaveAsync();

        return token;
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}