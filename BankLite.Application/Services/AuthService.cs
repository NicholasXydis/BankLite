using BankLite.Application.DTOs;
using BankLite.Application.Interfaces;
using BankLite.Domain.Entities;
using BankLite.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging;

namespace BankLite.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly ILogger<AuthService> _logger;

        public AuthService(IUserRepository userRepository, IConfiguration configuration, IAuditLogRepository auditLogRepository, ILogger<AuthService> logger, IRefreshTokenRepository refreshTokenRepository)
        {
            _userRepository = userRepository;
            _configuration = configuration;
            _auditLogRepository = auditLogRepository;
            _logger = logger;
            _refreshTokenRepository = refreshTokenRepository;
        }

        public async Task<(string Token, string RefreshToken, AuthResponseDto Response)> RegisterAsync(RegisterUserDto dto)
        {
            if (await _userRepository.ExistsAsync(dto.Email.ToLower()))
            {
                _logger.LogWarning("Registration failed - email already exists: {Email}", dto.Email);
                throw new InvalidOperationException("Email already registered");
            }

            dto.FullName = dto.FullName.Trim();

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email.ToLower(),
                PasswordHash = passwordHash
            };
            await _userRepository.AddAsync(user);
            _logger.LogInformation("User Registered Successfully: {Email}", dto.Email);

            await _auditLogRepository.LogAsync(new AuditLog
            {
                UserId = user.Id,
                Action = "Register",
                Details = $"User {user.Email} registered",
                PerformedAt = DateTime.UtcNow,
            });

            var token = GenerateToken(user);
            var refreshToken = await GenerateRefreshTokenAsync(user.Id);
            return (token, refreshToken, new AuthResponseDto
            {
                UserId = user.Id,
                FullName = user.FullName,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60),
            });
        }
        public async Task<(string Token, string RefreshToken, AuthResponseDto Response)> LoginAsync(LoginUserDto dto)
        {
            var user = await _userRepository.GetByEmailAsync(dto.Email.ToLower());
            if (user == null)
            {
                _logger.LogWarning("Login failed - user not found: {Email}", dto.Email.ToLower());
                throw new InvalidOperationException("Invalid Credentials");
            }

            if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
            {
                _logger.LogWarning("Login attempt on locked account: {Email}", dto.Email.ToLower());
                throw new InvalidOperationException("Account is locked. Please try again later.");
            }

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            {
                user.FailedLoginAttempts++;
                if (user.FailedLoginAttempts >= 5)
                {
                    user.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
                    user.FailedLoginAttempts = 0;
                    _logger.LogWarning("Account locked due to failed attempts: {Email}", dto.Email.ToLower());
                }
                await _userRepository.UpdateAsync(user);
                throw new InvalidOperationException("Invalid Credentials");
            }

            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;

            user.LastLoginAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            await _auditLogRepository.LogAsync(new AuditLog
            {
                UserId = user.Id,
                Action = "Login",
                Details = $"User {user.Email} logged in",
                PerformedAt = DateTime.UtcNow,
            });

            _logger.LogInformation("User logged in successfully: {Email}", user.Email);

            var token = GenerateToken(user);
            var refreshToken = await GenerateRefreshTokenAsync(user.Id);
            return (token, refreshToken, new AuthResponseDto
            {
                UserId = user.Id,
                FullName = user.FullName,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60)
            });
        }

        private string GenerateToken(User user)
        {
            var jwtsettings = _configuration.GetSection("JwtSettings");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtsettings["Secret"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim (ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email)
            };
            var token = new JwtSecurityToken(
                issuer: jwtsettings["Issuer"],
                audience: jwtsettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(double.Parse(jwtsettings["ExpiryMinutes"]!)),
                signingCredentials: creds
                );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<(string Token, string RefreshToken, AuthResponseDto Response)> RefreshAsync(string refreshToken)
        {
            var existing = await _refreshTokenRepository.GetByTokenAsync(refreshToken);
            if (existing == null || existing.IsRevoked || existing.ExpiresAt < DateTime.UtcNow)
                throw new UnauthorizedAccessException("Invalid or expired refresh token.");

            await _refreshTokenRepository.RevokeAsync(existing);

            var token = GenerateToken(existing.User);
            var newRefreshToken = await GenerateRefreshTokenAsync(existing.UserId);
            return (token, newRefreshToken, new AuthResponseDto
            {
                UserId = existing.User.Id,
                FullName = existing.User.FullName,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60)
            });
        }

        public async Task RevokeRefreshTokenAsync(string refreshToken)
        {
            var existing = await _refreshTokenRepository.GetByTokenAsync(refreshToken);
            if (existing != null && !existing.IsRevoked)
                await _refreshTokenRepository.RevokeAsync(existing);
        }

        private async Task<string> GenerateRefreshTokenAsync(Guid userId)
        {
            var token = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(64));
            var refreshToken = new BankLite.Domain.Entities.RefreshToken
            {
                UserId = userId,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddDays(1)
            };
            await _refreshTokenRepository.AddAsync(refreshToken);
            return token;
        }

    }
}
