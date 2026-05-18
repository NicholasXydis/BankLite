using BankLite.Application.DTOs;
using BankLite.Application.Interfaces;
using BankLite.Application.Services;
using BankLite.Domain.Entities;
using BankLite.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BankLite.Tests.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<IUserRepository> _mockUserRepo;
        private readonly Mock<IAuditLogRepository> _mockAuditRepo;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly Mock<IRefreshTokenRepository> _mockRefreshTokenRepo;
        private readonly Mock<IPasswordResetRepository> _mockPasswordResetRepo;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly AuthService _authService;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;

        public AuthServiceTests()
        {
            _mockUserRepo = new Mock<IUserRepository>();
            _mockAuditRepo = new Mock<IAuditLogRepository>();
            _mockConfig = new Mock<IConfiguration>();

            var jwtSection = new Mock<IConfigurationSection>();
            jwtSection.Setup(x => x["Secret"]).Returns("supersecretkey12345678901234567890");
            jwtSection.Setup(x => x["Issuer"]).Returns("BankLiteAPI");
            jwtSection.Setup(x => x["Audience"]).Returns("BankLiteClient");
            jwtSection.Setup(x => x["ExpiryMinutes"]).Returns("60");
            _mockConfig.Setup(x => x.GetSection("JwtSettings")).Returns(jwtSection.Object);
            _mockConfig.Setup(x => x["JwtSettings:ExpiryMinutes"]).Returns("60");

            _mockRefreshTokenRepo = new Mock<IRefreshTokenRepository>();
            _mockPasswordResetRepo = new Mock<IPasswordResetRepository>();
            _mockEmailService = new Mock<IEmailService>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();

            _authService = new AuthService(_mockUserRepo.Object, _mockConfig.Object, _mockAuditRepo.Object, new NullLogger<AuthService>(), _mockRefreshTokenRepo.Object, _mockPasswordResetRepo.Object, _mockEmailService.Object, _mockUnitOfWork.Object);
        }

        [Fact]
        public async Task LoginAsync_ShouldReturnToken_OnSuccess()
        {
            var existingUser = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@banklite.com",
                FullName = "Test User",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123")
            };

            _mockUserRepo
                .Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync(existingUser);

            var dto = new LoginUserDto
            {
                Email = "test@banklite.com",
                Password = "Password123"
            };

            var (token, refreshToken, response) = await _authService.LoginAsync(dto);
            Assert.NotEmpty(token);
            Assert.Equal(existingUser.Id, response.UserId);
        }

        [Fact]
        public async Task LoginAsync_ShouldReturnRefreshToken_OnSuccess()
        {
            var existingUser = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@banklite.com",
                FullName = "Test User",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123")
            };

            _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(existingUser);

            var dto = new LoginUserDto { Email = "test@banklite.com", Password = "Password123" };

            var (token, refreshToken, response) = await _authService.LoginAsync(dto);

            Assert.NotEmpty(refreshToken);
        }

        [Fact]
        public async Task LoginAsync_ShouldReturnCorrectUserId_OnSuccess()
        {
            var existingUser = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@banklite.com",
                FullName = "Test User",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123")
            };

            _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(existingUser);

            var dto = new LoginUserDto { Email = "test@banklite.com", Password = "Password123" };

            var (token, refreshToken, response) = await _authService.LoginAsync(dto);

            Assert.Equal(existingUser.Id, response.UserId);
            Assert.Equal(existingUser.FullName, response.FullName);
        }

        [Fact]
        public async Task LoginAsync_ShouldReturnFutureExpiresAt_OnSuccess()
        {
            var existingUser = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@banklite.com",
                FullName = "Test User",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123")
            };

            _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(existingUser);

            var dto = new LoginUserDto { Email = "test@banklite.com", Password = "Password123" };

            var (token, refreshToken, response) = await _authService.LoginAsync(dto);

            Assert.True(response.ExpiresAt > DateTime.UtcNow);
        }

        [Fact]
        public async Task LoginAsync_ShouldUpdateLastLoginAt_OnSuccess()
        {
            var existingUser = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@banklite.com",
                FullName = "Test User",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123")
            };

            _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(existingUser);

            var dto = new LoginUserDto { Email = "test@banklite.com", Password = "Password123" };

            await _authService.LoginAsync(dto);

            Assert.NotNull(existingUser.LastLoginAt);
        }

        [Fact]
        public async Task LoginAsync_ShouldResetFailedAttempts_OnSuccess()
        {
            var existingUser = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@banklite.com",
                FullName = "Test User",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123"),
                FailedLoginAttempts = 3
            };

            _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(existingUser);

            var dto = new LoginUserDto { Email = "test@banklite.com", Password = "Password123" };

            await _authService.LoginAsync(dto);

            Assert.Equal(0, existingUser.FailedLoginAttempts);
            Assert.Null(existingUser.LockoutEnd);
        }

        [Fact]
        public async Task LoginAsync_ShouldCallUpdateAsync_OnSuccess()
        {
            var existingUser = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@banklite.com",
                FullName = "Test User",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123")
            };

            _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(existingUser);

            var dto = new LoginUserDto { Email = "test@banklite.com", Password = "Password123" };

            await _authService.LoginAsync(dto);

            _mockUserRepo.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_ShouldNotBeLocked_WhenLockoutExpired()
        {
            var existingUser = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@banklite.com",
                FullName = "Test User",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123"),
                LockoutEnd = DateTime.UtcNow.AddMinutes(-1)
            };

            _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(existingUser);

            var dto = new LoginUserDto { Email = "test@banklite.com", Password = "Password123" };

            var (token, refreshToken, response) = await _authService.LoginAsync(dto);

            Assert.NotEmpty(token);
        }

        [Fact]
        public async Task LoginAsync_ShouldThrow_WhenUserNotFound()
        {
            _mockUserRepo
                .Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((User?)null);

            var dto = new LoginUserDto
            {
                Email = "test@banklite.com",
                Password = "Password123"
            };

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _authService.LoginAsync(dto));
        }

        [Fact]
        public async Task LoginAsync_ShouldThrow_WhenPasswordIsWrong()
        {
            var existingUser = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@banklite.com",
                FullName = "Existing User",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123")
            };

            _mockUserRepo
                .Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync(existingUser);

            var dto = new LoginUserDto
            {
                Email = "test@banklite.com",
                Password = "WrongPassword"
            };

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _authService.LoginAsync(dto));
        }

        [Fact]
        public async Task LoginAsync_ShouldThrow_WhenAccountLocked()
        {
            var existingUser = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@banklite.com",
                FullName = "Test User",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123"),
                LockoutEnd = DateTime.UtcNow.AddMinutes(10)
            };

            _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(existingUser);

            var dto = new LoginUserDto { Email = "test@banklite.com", Password = "Password123" };

            await Assert.ThrowsAsync<InvalidOperationException>(() => _authService.LoginAsync(dto));
        }

        [Fact]
        public async Task LoginAsync_ShouldIncrementFailedAttempts_WhenPasswordIsWrong()
        {
            var existingUser = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@banklite.com",
                FullName = "Test User",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123"),
                FailedLoginAttempts = 0
            };

            _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(existingUser);

            var dto = new LoginUserDto { Email = "test@banklite.com", Password = "WrongPassword" };

            await Assert.ThrowsAsync<InvalidOperationException>(() => _authService.LoginAsync(dto));

            Assert.Equal(1, existingUser.FailedLoginAttempts);
        }

        [Fact]
        public async Task LoginAsync_ShouldLockAccount_AfterFiveFailedAttempts()
        {
            var existingUser = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@banklite.com",
                FullName = "Test User",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123"),
                FailedLoginAttempts = 4
            };

            _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(existingUser);

            var dto = new LoginUserDto { Email = "test@banklite.com", Password = "WrongPassword" };

            await Assert.ThrowsAsync<InvalidOperationException>(() => _authService.LoginAsync(dto));

            Assert.NotNull(existingUser.LockoutEnd);
            Assert.Equal(0, existingUser.FailedLoginAttempts);
        }

        [Fact]
        public async Task LoginAsync_ShouldCallUpdateAsync_WhenPasswordIsWrong()
        {
            var existingUser = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@banklite.com",
                FullName = "Test User",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123")
            };

            _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(existingUser);

            var dto = new LoginUserDto { Email = "test@banklite.com", Password = "WrongPassword" };

            await Assert.ThrowsAsync<InvalidOperationException>(() => _authService.LoginAsync(dto));

            _mockUserRepo.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_ShouldNotCallUpdateAsync_WhenUserNotFound()
        {
            _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

            var dto = new LoginUserDto { Email = "test@banklite.com", Password = "Password123" };

            await Assert.ThrowsAsync<InvalidOperationException>(() => _authService.LoginAsync(dto));

            _mockUserRepo.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_ShouldReturnToken_OnSuccess()
        {
            _mockUserRepo
                .Setup(r => r.ExistsAsync(It.IsAny<string>()))
                .ReturnsAsync(false);

            var dto = new RegisterUserDto
            {
                FullName = "New User",
                Email = "new@banklite.com",
                Password = "Password123"
            };

            var (token, refreshToken, response) = await _authService.RegisterAsync(dto);
            Assert.NotEmpty(token);
        }

        [Fact]
        public async Task RegisterAsync_ShouldReturnRefreshToken_OnSuccess()
        {
            _mockUserRepo.Setup(r => r.ExistsAsync(It.IsAny<string>())).ReturnsAsync(false);

            var dto = new RegisterUserDto { FullName = "New User", Email = "new@banklite.com", Password = "Password123" };

            var (token, refreshToken, response) = await _authService.RegisterAsync(dto);

            Assert.NotEmpty(refreshToken);
        }

        [Fact]
        public async Task RegisterAsync_ShouldReturnCorrectFullName_OnSuccess()
        {
            _mockUserRepo.Setup(r => r.ExistsAsync(It.IsAny<string>())).ReturnsAsync(false);

            var dto = new RegisterUserDto { FullName = "New User", Email = "new@banklite.com", Password = "Password123" };

            var (token, refreshToken, response) = await _authService.RegisterAsync(dto);

            Assert.Equal("New User", response.FullName);
        }

        [Fact]
        public async Task RegisterAsync_ShouldReturnFutureExpiresAt_OnSuccess()
        {
            _mockUserRepo.Setup(r => r.ExistsAsync(It.IsAny<string>())).ReturnsAsync(false);

            var dto = new RegisterUserDto { FullName = "New User", Email = "new@banklite.com", Password = "Password123" };

            var (token, refreshToken, response) = await _authService.RegisterAsync(dto);

            Assert.True(response.ExpiresAt > DateTime.UtcNow);
        }

        [Fact]
        public async Task RegisterAsync_ShouldLowercaseEmail()
        {
            _mockUserRepo.Setup(r => r.ExistsAsync(It.IsAny<string>())).ReturnsAsync(false);

            User? savedUser = null;
            _mockUserRepo.Setup(r => r.AddAsync(It.IsAny<User>()))
                .Callback<User>(u => savedUser = u);

            var dto = new RegisterUserDto { FullName = "New User", Email = "NEW@BANKLITE.COM", Password = "Password123" };

            await _authService.RegisterAsync(dto);

            Assert.NotNull(savedUser);
            Assert.Equal("new@banklite.com", savedUser!.Email);
        }

        [Fact]
        public async Task RegisterAsync_ShouldTrimFullName()
        {
            _mockUserRepo.Setup(r => r.ExistsAsync(It.IsAny<string>())).ReturnsAsync(false);

            User? savedUser = null;
            _mockUserRepo.Setup(r => r.AddAsync(It.IsAny<User>()))
                .Callback<User>(u => savedUser = u);

            var dto = new RegisterUserDto { FullName = "  New User  ", Email = "new@banklite.com", Password = "Password123" };

            await _authService.RegisterAsync(dto);

            Assert.NotNull(savedUser);
            Assert.Equal("New User", savedUser!.FullName);
        }

        [Fact]
        public async Task RegisterAsync_ShouldHashPassword()
        {
            _mockUserRepo.Setup(r => r.ExistsAsync(It.IsAny<string>())).ReturnsAsync(false);

            User? savedUser = null;
            _mockUserRepo.Setup(r => r.AddAsync(It.IsAny<User>()))
                .Callback<User>(u => savedUser = u);

            var dto = new RegisterUserDto { FullName = "New User", Email = "new@banklite.com", Password = "Password123" };

            await _authService.RegisterAsync(dto);

            Assert.NotNull(savedUser);
            Assert.NotEqual("Password123", savedUser!.PasswordHash);
            Assert.True(BCrypt.Net.BCrypt.Verify("Password123", savedUser.PasswordHash));
        }

        [Fact]
        public async Task RegisterAsync_ShouldCallAddAsync_OnSuccess()
        {
            _mockUserRepo.Setup(r => r.ExistsAsync(It.IsAny<string>())).ReturnsAsync(false);

            var dto = new RegisterUserDto { FullName = "New User", Email = "new@banklite.com", Password = "Password123" };

            await _authService.RegisterAsync(dto);

            _mockUserRepo.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_ShouldThrow_WhenEmailAlreadyExists()
        {
            _mockUserRepo
                .Setup(r => r.ExistsAsync(It.IsAny<string>()))
                .ReturnsAsync(true);

            var dto = new RegisterUserDto
            {
                FullName = "New User",
                Email = "test@banklite.com",
                Password = "Password123"
            };

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _authService.RegisterAsync(dto));
        }

        [Fact]
        public async Task RegisterAsync_ShouldNotCallAddAsync_WhenEmailExists()
        {
            _mockUserRepo.Setup(r => r.ExistsAsync(It.IsAny<string>())).ReturnsAsync(true);

            var dto = new RegisterUserDto { FullName = "New User", Email = "test@banklite.com", Password = "Password123" };

            await Assert.ThrowsAsync<InvalidOperationException>(() => _authService.RegisterAsync(dto));

            _mockUserRepo.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task ForgotPasswordAsync_ShouldSilentlyFail_WhenUserNotFound()
        {
            _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

            var exception = await Record.ExceptionAsync(
                () => _authService.ForgotPasswordAsync("notexist@banklite.com", "http://localhost/reset"));

            Assert.Null(exception);
        }

        [Fact]
        public async Task ForgotPasswordAsync_ShouldSendEmail_WhenUserExists()
        {
            var existingUser = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@banklite.com",
                FullName = "Test User",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123")
            };

            _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(existingUser);

            await _authService.ForgotPasswordAsync("test@banklite.com", "http://localhost/reset");

            _mockEmailService.Verify(e => e.SendPasswordResetEmailAsync(existingUser.Email, It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task ForgotPasswordAsync_ShouldSaveResetToken_WhenUserExists()
        {
            var existingUser = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@banklite.com",
                FullName = "Test User",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123")
            };

            _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(existingUser);

            await _authService.ForgotPasswordAsync("test@banklite.com", "http://localhost/reset");

            _mockPasswordResetRepo.Verify(r => r.AddAsync(It.IsAny<PasswordResetToken>()), Times.Once);
        }

        [Fact]
        public async Task ResetPasswordAsync_ShouldResetPassword_OnSuccess()
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@banklite.com",
                FullName = "Test User",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword123")
            };

            var resetToken = new PasswordResetToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = "valid-token",
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                IsUsed = false,
                User = user
            };

            var hashedToken = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes("valid-token")));
            _mockPasswordResetRepo.Setup(r => r.GetByTokenAsync(hashedToken)).ReturnsAsync(resetToken);

            await _authService.ResetPasswordAsync("valid-token", "NewPassword123");

            Assert.True(BCrypt.Net.BCrypt.Verify("NewPassword123", user.PasswordHash));
        }

        [Fact]
        public async Task ResetPasswordAsync_ShouldMarkTokenAsUsed_OnSuccess()
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@banklite.com",
                FullName = "Test User",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword123")
            };

            var resetToken = new PasswordResetToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = "valid-token",
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                IsUsed = false,
                User = user
            };

            var hashedToken = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes("valid-token")));
            _mockPasswordResetRepo.Setup(r => r.GetByTokenAsync(hashedToken)).ReturnsAsync(resetToken);

            await _authService.ResetPasswordAsync("valid-token", "NewPassword123");

            Assert.True(resetToken.IsUsed);
            _mockPasswordResetRepo.Verify(r => r.UpdateAsync(resetToken), Times.Once);
        }

        [Fact]
        public async Task ResetPasswordAsync_ShouldHashNewPassword()
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@banklite.com",
                FullName = "Test User",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword123")
            };

            var resetToken = new PasswordResetToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = "valid-token",
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                IsUsed = false,
                User = user
            };

            var hashedToken = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes("valid-token")));
            _mockPasswordResetRepo.Setup(r => r.GetByTokenAsync(hashedToken)).ReturnsAsync(resetToken);

            await _authService.ResetPasswordAsync("valid-token", "NewPassword123");

            Assert.NotEqual("NewPassword123", user.PasswordHash);
            Assert.True(BCrypt.Net.BCrypt.Verify("NewPassword123", user.PasswordHash));
        }

        [Fact]
        public async Task ResetPasswordAsync_ShouldThrow_WhenTokenNotFound()
        {
            _mockPasswordResetRepo.Setup(r => r.GetByTokenAsync(It.IsAny<string>())).ReturnsAsync((PasswordResetToken?)null);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _authService.ResetPasswordAsync("invalid-token", "NewPassword123"));
        }

        [Fact]
        public async Task ResetPasswordAsync_ShouldThrow_WhenTokenExpired()
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@banklite.com",
                FullName = "Test User",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword123")
            };

            var resetToken = new PasswordResetToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = "expired-token",
                ExpiresAt = DateTime.UtcNow.AddMinutes(-1),
                IsUsed = false,
                User = user
            };

            _mockPasswordResetRepo.Setup(r => r.GetByTokenAsync("expired-token")).ReturnsAsync(resetToken);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _authService.ResetPasswordAsync("expired-token", "NewPassword123"));
        }

        [Fact]
        public async Task ResetPasswordAsync_ShouldThrow_WhenTokenAlreadyUsed()
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@banklite.com",
                FullName = "Test User",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword123")
            };

            var resetToken = new PasswordResetToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = "used-token",
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                IsUsed = true,
                User = user
            };

            _mockPasswordResetRepo.Setup(r => r.GetByTokenAsync("used-token")).ReturnsAsync(resetToken);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _authService.ResetPasswordAsync("used-token", "NewPassword123"));
        }

        [Fact]
        public async Task RevokeRefreshTokenAsync_ShouldRevoke_WhenTokenExists()
        {
            var refreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = "valid-refresh-token",
                IsRevoked = false,
                ExpiresAt = DateTime.UtcNow.AddDays(1)
            };

            var hashedToken = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes("valid-refresh-token")));
            _mockRefreshTokenRepo.Setup(r => r.GetByTokenAsync(hashedToken)).ReturnsAsync(refreshToken);

            await _authService.RevokeRefreshTokenAsync("valid-refresh-token");

            _mockRefreshTokenRepo.Verify(r => r.RevokeAsync(refreshToken), Times.Once);
        }

        [Fact]
        public async Task RevokeRefreshTokenAsync_ShouldSilentlyFail_WhenTokenNotFound()
        {
            _mockRefreshTokenRepo.Setup(r => r.GetByTokenAsync(It.IsAny<string>())).ReturnsAsync((RefreshToken?)null);

            var exception = await Record.ExceptionAsync(
                () => _authService.RevokeRefreshTokenAsync("nonexistent-token"));

            Assert.Null(exception);
        }

        [Fact]
        public async Task RevokeRefreshTokenAsync_ShouldNotRevoke_WhenTokenAlreadyRevoked()
        {
            var refreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = "already-revoked-token",
                IsRevoked = true,
                ExpiresAt = DateTime.UtcNow.AddDays(1)
            };

            var hashedToken = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes("already-revoked-token")));
            _mockRefreshTokenRepo.Setup(r => r.GetByTokenAsync(hashedToken)).ReturnsAsync(refreshToken);

            await _authService.RevokeRefreshTokenAsync("already-revoked-token");

            _mockRefreshTokenRepo.Verify(r => r.RevokeAsync(It.IsAny<RefreshToken>()), Times.Never);
        }
    }
}