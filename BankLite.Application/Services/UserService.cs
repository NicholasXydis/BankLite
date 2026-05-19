using BankLite.Application.DTOs;
using BankLite.Application.Interfaces;
using BankLite.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace BankLite.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UserService> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRefreshTokenRepository _refreshTokenRepository;

        public UserService(IUserRepository userRepository, IUnitOfWork unitOfWork, ILogger<UserService> logger, IRefreshTokenRepository refreshTokenRepository)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _refreshTokenRepository = refreshTokenRepository;
        }

        public async Task<UserProfileDto> GetProfileAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new InvalidOperationException("User not found");

            _logger.LogInformation("Profile fetched for user {UserId}", userId);
            return new UserProfileDto
            {
                FullName = user.FullName,
                Email = user.Email,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };
        }

        public async Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new InvalidOperationException("User not found");

            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
                throw new InvalidOperationException("Current password is incorrect");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            await _userRepository.UpdateAsync(user);
            await _refreshTokenRepository.RevokeAllForUserAsync(userId);
            await _unitOfWork.SaveAsync();
            _logger.LogInformation("Password changed for user: {UserId}", userId);
        }

        public async Task DeleteAccountAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new InvalidOperationException("User not found");

            await _userRepository.DeleteAsync(user);
            await _unitOfWork.SaveAsync();
            _logger.LogInformation("Account deleted for user: {UserId}", userId);
        }
    }
}