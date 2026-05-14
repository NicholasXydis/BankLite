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
        private readonly ILogger<AuthService> _logger;

        public AuthService(IUserRepository userRepository, IConfiguration configuration, IAuditLogRepository auditLogRepository, ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _configuration = configuration;
            _auditLogRepository = auditLogRepository;
            _logger = logger;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterUserDto dto)
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
            return new AuthResponseDto
            {
                Token = token,
                UserId = user.Id,
                FullName = user.FullName
            };
        }
        public async Task<AuthResponseDto> LoginAsync(LoginUserDto dto)
        {
            var user = await _userRepository.GetByEmailAsync(dto.Email.ToLower());
            if (user == null)
            {
                _logger.LogWarning("Login failed - user not found: {Email}", dto.Email.ToLower());
                throw new InvalidOperationException("Invalid Credentials");
            }

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            {
                _logger.LogWarning("Login failed - wrong password: {Email}", dto.Email.ToLower());
                throw new InvalidOperationException("Invalid Credentials");
            }

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
            return new AuthResponseDto
            {
                Token = token,
                UserId = user.Id,
                FullName = user.FullName
            };
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

    }
}
