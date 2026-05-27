using BankLite.Application.DTOs;
using BankLite.Application.Interfaces;
using BankLite.Application.Services;
using BankLite.Domain.Entities;
using BankLite.Domain.Interfaces;
using FluentAssertions;
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
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            _mockUserRepo = new Mock<IUserRepository>();
            _mockAuditRepo = new Mock<IAuditLogRepository>();
            _mockConfig = new Mock<IConfiguration>();
            _mockRefreshTokenRepo = new Mock<IRefreshTokenRepository>();
            _mockPasswordResetRepo = new Mock<IPasswordResetRepository>();
            _mockEmailService = new Mock<IEmailService>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();

            var jwtSection = new Mock<IConfigurationSection>();
            jwtSection.Setup(x => x["Secret"]).Returns("supersecretkey12345678901234567890");
            jwtSection.Setup(x => x["Issuer"]).Returns("BankLiteAPI");
            jwtSection.Setup(x => x["Audience"]).Returns("BankLiteClient");
            jwtSection.Setup(x => x["ExpiryMinutes"]).Returns("60");
            _mockConfig.Setup(x => x.GetSection("JwtSettings")).Returns(jwtSection.Object);
            _mockConfig.Setup(x => x["JwtSettings:ExpiryMinutes"]).Returns("60");

            _mockUnitOfWork.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
                .Returns((Func<Task> operation) => operation());

            _authService = new AuthService(
                _mockUserRepo.Object,
                _mockConfig.Object,
                _mockAuditRepo.Object,
                new NullLogger<AuthService>(),
                _mockRefreshTokenRepo.Object,
                _mockPasswordResetRepo.Object,
                _mockEmailService.Object,
                _mockUnitOfWork.Object);
        }

        private User CreateTestUser(string email = "test@banklite.com", string password = "Password123", int failedAttempts = 0, DateTime? lockoutEnd = null)
        {
            return new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                FullName = "Test User",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                FailedLoginAttempts = failedAttempts,
                LockoutEnd = lockoutEnd
            };
        }

        private static string HashToken(string token)
        {
            var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(bytes);
        }

        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsToken()
        {
            var user = CreateTestUser();
            _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
            var dto = new LoginUserDto { Email = "test@banklite.com", Password = "Password123" };

            var (token, _, _) = await _authService.LoginAsync(dto);

            token.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsRefreshToken()
        {
            var user = CreateTestUser();
            _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
            var dto = new LoginUserDto { Email = "test@banklite.com", Password = "Password123" };

            var (_, refreshToken, _) = await _authService.LoginAsync(dto);

            refreshToken.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsCorrectUserIdAndFullName()
        {
            var user = CreateTestUser();
            _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
            var dto = new LoginUserDto { Email = "test@banklite.com", Password = "Password123" };

            var (_, _, response) = await _authService.LoginAsync(dto);

            response.UserId.Should().Be(user.Id);
            response.FullName.Should().Be(user.FullName);
        }

        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsFutureExpiresAt()
        {
            var user = CreateTestUser();
            _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
            var dto = new LoginUserDto { Email = "test@banklite.com", Password = "Password123" };

            var (_, _, response) = await _authService.LoginAsync(dto);

            response.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
        }

        [Fact]
        public async Task LoginAsync_ValidCredentials_UpdatesLastLoginAt()
        {
            var user = CreateTestUser();
            _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
            var dto = new LoginUserDto { Email = "test@banklite.com", Password = "Password123" };

            await _authService.LoginAsync(dto);

            user.LastLoginAt.Should().NotBeNull();
        }

        [Fact]
        public async Task LoginAsync_ValidCredentials_ResetsFailedAttemptsAndLockout()
        {
            var user = CreateTestUser(failedAttempts: 3);
            _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
            var dto = new LoginUserDto { Email = "test@banklite.com", Password = "Password123" };

            await _authService.LoginAsync(dto);

            user.FailedLoginAttempts.Should().Be(0);
            user.LockoutEnd.Should().BeNull();
        }

        [Fact]
        public async Task LoginAsync_ValidCredentials_CallsUpdateAsyncOnce()
        {
            var user = CreateTestUser();
            _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
            var dto = new LoginUserDto { Email = "test@banklite.com", Password = "Password123" };

            await _authService.LoginAsync(dto);

            _mockUserRepo.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_ExpiredLockout_AllowsLogin()
        {
            var user = CreateTestUser(lockoutEnd: DateTime.UtcNow.AddMinutes(-1));
            _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
            var dto = new LoginUserDto { Email = "test@banklite.com", Password = "Password123" };

            var (token, _, _) = await _authService.LoginAsync(dto);

            token.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task LoginAsync_UserNotFound_ThrowsInvalidOperationException()
        {
            _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
            var dto = new LoginUserDto { Email = "test@banklite.com", Password = "Password123" };

            var act = async () => await _authService.LoginAsync(dto);

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task LoginAsync_WrongPassword_ThrowsInvalidOperationException()
        {
            var user = CreateTestUser();
            _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
            var dto = new LoginUserDto { Email = "test@banklite.com", Password = "WrongPassword" };

            var act = async () => await _authService.LoginAsync(dto);

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task LoginAsync_AccountLocked_ThrowsInvalidOperationException()
        {
            var user = CreateTestUser(lockoutEnd: DateTime.UtcNow.AddMinutes(10));
            _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
            var dto = new LoginUserDto { Email = "test@banklite.com", Password = "Password123" };

            var act = async () => await _authService.LoginAsync(dto);

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task LoginAsync_WrongPassword_IncrementsFailedAttempts()
        {
            var user = CreateTestUser(failedAttempts: 0);
            _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
            var dto = new LoginUserDto { Email = "test@banklite.com", Password = "WrongPassword" };

            await Assert.ThrowsAsync<InvalidOperationException>(() => _authService.LoginAsync(dto));

            user.FailedLoginAttempts.Should().Be(1);
        }

        [Fact]
        public async Task LoginAsync_FiveFailedAttempts_LocksAccountAndResetsCounter()
        {
            var user = CreateTestUser(failedAttempts: 4);
            _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
            var dto = new LoginUserDto { Email = "test@banklite.com", Password = "WrongPassword" };

            await Assert.ThrowsAsync<InvalidOperationException>(() => _authService.LoginAsync(dto));

            user.LockoutEnd.Should().NotBeNull();
            user.FailedLoginAttempts.Should().Be(0);
        }

        [Fact]
        public async Task LoginAsync_WrongPassword_CallsUpdateAsync()
        {
            var user = CreateTestUser();
            _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
            var dto = new LoginUserDto { Email = "test@banklite.com", Password = "WrongPassword" };

            await Assert.ThrowsAsync<InvalidOperationException>(() => _authService.LoginAsync(dto));

            _mockUserRepo.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_UserNotFound_NeverCallsUpdateAsync()
        {
            _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
            var dto = new LoginUserDto { Email = "test@banklite.com", Password = "Password123" };

            await Assert.ThrowsAsync<InvalidOperationException>(() => _authService.LoginAsync(dto));

            _mockUserRepo.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_ValidData_ReturnsToken()
        {
            _mockUserRepo.Setup(r => r.ExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            var dto = new RegisterUserDto { FullName = "New User", Email = "new@banklite.com", Password = "Password123" };

            var (token, _, _) = await _authService.RegisterAsync(dto);

            token.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task RegisterAsync_ValidData_ReturnsRefreshToken()
        {
            _mockUserRepo.Setup(r => r.ExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            var dto = new RegisterUserDto { FullName = "New User", Email = "new@banklite.com", Password = "Password123" };

            var (_, refreshToken, _) = await _authService.RegisterAsync(dto);

            refreshToken.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task RegisterAsync_ValidData_ReturnsCorrectFullName()
        {
            _mockUserRepo.Setup(r => r.ExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            var dto = new RegisterUserDto { FullName = "New User", Email = "new@banklite.com", Password = "Password123" };

            var (_, _, response) = await _authService.RegisterAsync(dto);

            response.FullName.Should().Be("New User");
        }

        [Fact]
        public async Task RegisterAsync_ValidData_ReturnsFutureExpiresAt()
        {
            _mockUserRepo.Setup(r => r.ExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            var dto = new RegisterUserDto { FullName = "New User", Email = "new@banklite.com", Password = "Password123" };

            var (_, _, response) = await _authService.RegisterAsync(dto);

            response.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
        }

        [Fact]
        public async Task RegisterAsync_UpperCaseEmail_StoresAsLowerCase()
        {
            _mockUserRepo.Setup(r => r.ExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            User? savedUser = null;
            _mockUserRepo.Setup(r => r.AddAsync(It.IsAny<User>())).Callback<User>(u => savedUser = u);
            var dto = new RegisterUserDto { FullName = "New User", Email = "NEW@BANKLITE.COM", Password = "Password123" };

            await _authService.RegisterAsync(dto);

            savedUser.Should().NotBeNull();
            savedUser!.Email.Should().Be("new@banklite.com");
        }

        [Fact]
        public async Task RegisterAsync_ValidData_HashesPassword()
        {
            _mockUserRepo.Setup(r => r.ExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            User? savedUser = null;
            _mockUserRepo.Setup(r => r.AddAsync(It.IsAny<User>())).Callback<User>(u => savedUser = u);
            var dto = new RegisterUserDto { FullName = "New User", Email = "new@banklite.com", Password = "Password123" };

            await _authService.RegisterAsync(dto);

            savedUser.Should().NotBeNull();
            savedUser!.PasswordHash.Should().NotBe("Password123");
            BCrypt.Net.BCrypt.Verify("Password123", savedUser.PasswordHash).Should().BeTrue();
        }

        [Fact]
        public async Task RegisterAsync_ValidData_CallsAddAsyncOnce()
        {
            _mockUserRepo.Setup(r => r.ExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            var dto = new RegisterUserDto { FullName = "New User", Email = "new@banklite.com", Password = "Password123" };

            await _authService.RegisterAsync(dto);

            _mockUserRepo.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_DuplicateEmail_ThrowsInvalidOperationException()
        {
            _mockUserRepo.Setup(r => r.ExistsAsync(It.IsAny<string>())).ReturnsAsync(true);
            var dto = new RegisterUserDto { FullName = "New User", Email = "test@banklite.com", Password = "Password123" };

            var act = async () => await _authService.RegisterAsync(dto);

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task RegisterAsync_DuplicateEmail_NeverCallsAddAsync()
        {
            _mockUserRepo.Setup(r => r.ExistsAsync(It.IsAny<string>())).ReturnsAsync(true);
            var dto = new RegisterUserDto { FullName = "New User", Email = "test@banklite.com", Password = "Password123" };

            await Assert.ThrowsAsync<InvalidOperationException>(() => _authService.RegisterAsync(dto));

            _mockUserRepo.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task ForgotPasswordAsync_UserNotFound_DoesNotThrow()
        {
            _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

            var act = async () => await _authService.ForgotPasswordAsync("notexist@banklite.com", "http://localhost/reset");

            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task ForgotPasswordAsync_UserExists_SendsEmail()
        {
            var user = CreateTestUser();
            _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);

            await _authService.ForgotPasswordAsync("test@banklite.com", "http://localhost/reset", "en");

            _mockEmailService.Verify(e => e.SendPasswordResetEmailAsync(user.Email, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task ForgotPasswordAsync_UserExists_SavesResetToken()
        {
            var user = CreateTestUser();
            _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);

            await _authService.ForgotPasswordAsync("test@banklite.com", "http://localhost/reset");

            _mockPasswordResetRepo.Verify(r => r.AddAsync(It.IsAny<PasswordResetToken>()), Times.Once);
        }

        [Fact]
        public async Task ResetPasswordAsync_ValidToken_ResetsPassword()
        {
            var user = CreateTestUser(password: "OldPassword123");
            var resetToken = new PasswordResetToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = "valid-token",
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                IsUsed = false,
                User = user
            };
            _mockPasswordResetRepo.Setup(r => r.GetByTokenAsync(HashToken("valid-token"))).ReturnsAsync(resetToken);

            await _authService.ResetPasswordAsync("valid-token", "NewPassword123");

            BCrypt.Net.BCrypt.Verify("NewPassword123", user.PasswordHash).Should().BeTrue();
        }

        [Fact]
        public async Task ResetPasswordAsync_ValidToken_MarksTokenAsUsed()
        {
            var user = CreateTestUser(password: "OldPassword123");
            var resetToken = new PasswordResetToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = "valid-token",
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                IsUsed = false,
                User = user
            };
            _mockPasswordResetRepo.Setup(r => r.GetByTokenAsync(HashToken("valid-token"))).ReturnsAsync(resetToken);

            await _authService.ResetPasswordAsync("valid-token", "NewPassword123");

            resetToken.IsUsed.Should().BeTrue();
            _mockPasswordResetRepo.Verify(r => r.UpdateAsync(resetToken), Times.Once);
        }

        [Fact]
        public async Task ResetPasswordAsync_ValidToken_HashesNewPassword()
        {
            var user = CreateTestUser(password: "OldPassword123");
            var resetToken = new PasswordResetToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = "valid-token",
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                IsUsed = false,
                User = user
            };
            _mockPasswordResetRepo.Setup(r => r.GetByTokenAsync(HashToken("valid-token"))).ReturnsAsync(resetToken);

            await _authService.ResetPasswordAsync("valid-token", "NewPassword123");

            user.PasswordHash.Should().NotBe("NewPassword123");
            BCrypt.Net.BCrypt.Verify("NewPassword123", user.PasswordHash).Should().BeTrue();
        }

        [Theory]
        [InlineData(null)]
        public async Task ResetPasswordAsync_TokenNotFound_ThrowsInvalidOperationException(PasswordResetToken? token)
        {
            _mockPasswordResetRepo.Setup(r => r.GetByTokenAsync(It.IsAny<string>())).ReturnsAsync(token);

            var act = async () => await _authService.ResetPasswordAsync("invalid-token", "NewPassword123");

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task ResetPasswordAsync_ExpiredToken_ThrowsInvalidOperationException()
        {
            var user = CreateTestUser();
            var resetToken = new PasswordResetToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = "expired-token",
                ExpiresAt = DateTime.UtcNow.AddMinutes(-1),
                IsUsed = false,
                User = user
            };
            _mockPasswordResetRepo.Setup(r => r.GetByTokenAsync(HashToken("expired-token"))).ReturnsAsync(resetToken);

            var act = async () => await _authService.ResetPasswordAsync("expired-token", "NewPassword123");

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task ResetPasswordAsync_AlreadyUsedToken_ThrowsInvalidOperationException()
        {
            var user = CreateTestUser();
            var resetToken = new PasswordResetToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = "used-token",
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                IsUsed = true,
                User = user
            };
            _mockPasswordResetRepo.Setup(r => r.GetByTokenAsync(HashToken("used-token"))).ReturnsAsync(resetToken);

            var act = async () => await _authService.ResetPasswordAsync("used-token", "NewPassword123");

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task RevokeRefreshTokenAsync_ValidToken_RevokesToken()
        {
            var refreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = "valid-refresh-token",
                IsRevoked = false,
                ExpiresAt = DateTime.UtcNow.AddDays(1)
            };
            _mockRefreshTokenRepo.Setup(r => r.GetByTokenAsync(HashToken("valid-refresh-token"))).ReturnsAsync(refreshToken);

            await _authService.RevokeRefreshTokenAsync("valid-refresh-token");

            _mockRefreshTokenRepo.Verify(r => r.RevokeAsync(refreshToken), Times.Once);
        }

        [Fact]
        public async Task RevokeRefreshTokenAsync_TokenNotFound_DoesNotThrow()
        {
            _mockRefreshTokenRepo.Setup(r => r.GetByTokenAsync(It.IsAny<string>())).ReturnsAsync((RefreshToken?)null);

            var act = async () => await _authService.RevokeRefreshTokenAsync("nonexistent-token");

            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task RevokeRefreshTokenAsync_AlreadyRevokedToken_NeverCallsRevokeAsync()
        {
            var refreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = "already-revoked-token",
                IsRevoked = true,
                ExpiresAt = DateTime.UtcNow.AddDays(1)
            };
            _mockRefreshTokenRepo.Setup(r => r.GetByTokenAsync(HashToken("already-revoked-token"))).ReturnsAsync(refreshToken);

            await _authService.RevokeRefreshTokenAsync("already-revoked-token");

            _mockRefreshTokenRepo.Verify(r => r.RevokeAsync(It.IsAny<RefreshToken>()), Times.Never);
        }

        [Fact]
        public async Task RefreshAsync_RevokedToken_ThrowsUnauthorizedAccessException()
        {
            var rawToken = "test-token";
            var refreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = HashToken(rawToken),
                IsRevoked = true,
                ExpiresAt = DateTime.UtcNow.AddDays(1)
            };
            _mockRefreshTokenRepo.Setup(r => r.GetByTokenAsync(HashToken(rawToken))).ReturnsAsync(refreshToken);

            var act = async () => await _authService.RefreshAsync(rawToken);

            await act.Should().ThrowAsync<UnauthorizedAccessException>();
        }

        [Fact]
        public async Task RefreshAsync_ExpiredToken_ThrowsUnauthorizedAccessException()
        {
            var rawToken = "test-token";
            var refreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = HashToken(rawToken),
                IsRevoked = false,
                ExpiresAt = DateTime.UtcNow.AddDays(-1)
            };
            _mockRefreshTokenRepo.Setup(r => r.GetByTokenAsync(HashToken(rawToken))).ReturnsAsync(refreshToken);

            var act = async () => await _authService.RefreshAsync(rawToken);

            await act.Should().ThrowAsync<UnauthorizedAccessException>();
        }
    }
}