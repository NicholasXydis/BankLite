using BankLite.Application.DTOs;
using BankLite.Application.Services;
using BankLite.Domain.Entities;
using BankLite.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BankLite.Tests.Services
{
    public class UserServiceTests
    {
        private readonly Mock<IUserRepository> _mockUserRepo;
        private readonly UserService _userService;

        public UserServiceTests()
        {
            _mockUserRepo = new Mock<IUserRepository>();
            _userService = new UserService(_mockUserRepo.Object, new NullLogger<UserService>());
        }

        [Fact]
        public async Task GetProfileAsync_ShouldReturnProfile_OnSuccess()
        {
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                FullName = "Test User",
                Email = "test@banklite.com",
                PasswordHash = string.Empty,
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                LastLoginAt = DateTime.UtcNow.AddDays(-1)
            };
            _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

            var result = await _userService.GetProfileAsync(userId);

            Assert.NotNull(result);
            Assert.Equal(user.FullName, result.FullName);
            Assert.Equal(user.Email, result.Email);
        }

        [Fact]
        public async Task GetProfileAsync_ShouldReturnCorrectCreatedAt()
        {
            var userId = Guid.NewGuid();
            var createdAt = DateTime.UtcNow.AddDays(-30);
            var user = new User
            {
                Id = userId,
                FullName = "Test User",
                Email = "test@banklite.com",
                PasswordHash = string.Empty,
                CreatedAt = createdAt
            };
            _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

            var result = await _userService.GetProfileAsync(userId);

            Assert.Equal(createdAt, result.CreatedAt);
        }

        [Fact]
        public async Task GetProfileAsync_ShouldReturnCorrectLastLoginAt()
        {
            var userId = Guid.NewGuid();
            var lastLoginAt = DateTime.UtcNow.AddDays(-1);
            var user = new User
            {
                Id = userId,
                FullName = "Test User",
                Email = "test@banklite.com",
                PasswordHash = string.Empty,
                LastLoginAt = lastLoginAt
            };
            _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

            var result = await _userService.GetProfileAsync(userId);

            Assert.Equal(lastLoginAt, result.LastLoginAt);
        }

        [Fact]
        public async Task GetProfileAsync_ShouldReturnNullLastLoginAt_WhenNeverLoggedIn()
        {
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                FullName = "Test User",
                Email = "test@banklite.com",
                PasswordHash = string.Empty,
                LastLoginAt = null
            };
            _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

            var result = await _userService.GetProfileAsync(userId);

            Assert.Null(result.LastLoginAt);
        }

        [Fact]
        public async Task GetProfileAsync_ShouldThrow_WhenUserNotFound()
        {
            _mockUserRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _userService.GetProfileAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task GetProfileAsync_ShouldThrow_WithCorrectMessage_WhenUserNotFound()
        {
            _mockUserRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _userService.GetProfileAsync(Guid.NewGuid()));

            Assert.Contains("User not found", ex.Message);
        }

        [Fact]
        public async Task GetProfileAsync_ShouldCallRepository_Once()
        {
            var userId = Guid.NewGuid();
            var user = new User { Id = userId, FullName = "Test User", Email = "test@banklite.com", PasswordHash = string.Empty };
            _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

            await _userService.GetProfileAsync(userId);

            _mockUserRepo.Verify(r => r.GetByIdAsync(userId), Times.Once);
        }

        [Fact]
        public async Task ChangePasswordAsync_ShouldChangePassword_OnSuccess()
        {
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                FullName = "Test User",
                Email = "test@banklite.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword123")
            };
            _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

            var dto = new ChangePasswordDto { CurrentPassword = "OldPassword123", NewPassword = "NewPassword123" };

            await _userService.ChangePasswordAsync(userId, dto);

            Assert.True(BCrypt.Net.BCrypt.Verify("NewPassword123", user.PasswordHash));
        }

        [Fact]
        public async Task ChangePasswordAsync_ShouldHashNewPassword()
        {
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                FullName = "Test User",
                Email = "test@banklite.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword123")
            };
            _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

            var dto = new ChangePasswordDto { CurrentPassword = "OldPassword123", NewPassword = "NewPassword123" };

            await _userService.ChangePasswordAsync(userId, dto);

            Assert.NotEqual("NewPassword123", user.PasswordHash);
            Assert.True(BCrypt.Net.BCrypt.Verify("NewPassword123", user.PasswordHash));
        }

        [Fact]
        public async Task ChangePasswordAsync_ShouldCallUpdateAsync_OnSuccess()
        {
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                FullName = "Test User",
                Email = "test@banklite.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword123")
            };
            _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

            var dto = new ChangePasswordDto { CurrentPassword = "OldPassword123", NewPassword = "NewPassword123" };

            await _userService.ChangePasswordAsync(userId, dto);

            _mockUserRepo.Verify(r => r.UpdateAsync(user), Times.Once);
        }

        [Fact]
        public async Task ChangePasswordAsync_ShouldPreserveOldHash_WhenPasswordIsWrong()
        {
            var userId = Guid.NewGuid();
            var oldHash = BCrypt.Net.BCrypt.HashPassword("OldPassword123");
            var user = new User
            {
                Id = userId,
                FullName = "Test User",
                Email = "test@banklite.com",
                PasswordHash = oldHash
            };
            _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

            var dto = new ChangePasswordDto { CurrentPassword = "WrongPassword", NewPassword = "NewPassword123" };

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _userService.ChangePasswordAsync(userId, dto));

            Assert.Equal(oldHash, user.PasswordHash);
        }

        [Fact]
        public async Task ChangePasswordAsync_ShouldNotCallUpdateAsync_WhenPasswordIsWrong()
        {
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                FullName = "Test User",
                Email = "test@banklite.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword123")
            };
            _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

            var dto = new ChangePasswordDto { CurrentPassword = "WrongPassword", NewPassword = "NewPassword123" };

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _userService.ChangePasswordAsync(userId, dto));

            _mockUserRepo.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task ChangePasswordAsync_ShouldNotCallUpdateAsync_WhenUserNotFound()
        {
            _mockUserRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

            var dto = new ChangePasswordDto { CurrentPassword = "OldPassword123", NewPassword = "NewPassword123" };

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _userService.ChangePasswordAsync(Guid.NewGuid(), dto));

            _mockUserRepo.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task ChangePasswordAsync_ShouldThrow_WhenUserNotFound()
        {
            _mockUserRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

            var dto = new ChangePasswordDto { CurrentPassword = "OldPassword123", NewPassword = "NewPassword123" };

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _userService.ChangePasswordAsync(Guid.NewGuid(), dto));
        }

        [Fact]
        public async Task ChangePasswordAsync_ShouldThrow_WhenCurrentPasswordIsWrong()
        {
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                FullName = "Test User",
                Email = "test@banklite.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword123")
            };
            _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

            var dto = new ChangePasswordDto { CurrentPassword = "WrongPassword", NewPassword = "NewPassword123" };

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _userService.ChangePasswordAsync(userId, dto));
        }

        [Fact]
        public async Task ChangePasswordAsync_ShouldThrow_WithCorrectMessage_WhenCurrentPasswordIsWrong()
        {
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                FullName = "Test User",
                Email = "test@banklite.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword123")
            };
            _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

            var dto = new ChangePasswordDto { CurrentPassword = "WrongPassword", NewPassword = "NewPassword123" };

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _userService.ChangePasswordAsync(userId, dto));

            Assert.Contains("incorrect", ex.Message);
        }

        [Fact]
        public async Task ChangePasswordAsync_ShouldThrow_WithCorrectMessage_WhenUserNotFound()
        {
            _mockUserRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

            var dto = new ChangePasswordDto { CurrentPassword = "OldPassword123", NewPassword = "NewPassword123" };

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _userService.ChangePasswordAsync(Guid.NewGuid(), dto));

            Assert.Contains("User not found", ex.Message);
        }

        [Fact]
        public async Task DeleteAccountAsync_ShouldCallDeleteAsync_WithCorrectUser()
        {
            var userId = Guid.NewGuid();
            var user = new User { Id = userId, FullName = "Test User", Email = "test@banklite.com", PasswordHash = string.Empty };
            _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

            await _userService.DeleteAccountAsync(userId);

            _mockUserRepo.Verify(r => r.DeleteAsync(user), Times.Once);
        }

        [Fact]
        public async Task DeleteAccountAsync_ShouldCallGetByIdAsync_Once()
        {
            var userId = Guid.NewGuid();
            var user = new User { Id = userId, FullName = "Test User", Email = "test@banklite.com", PasswordHash = string.Empty };
            _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

            await _userService.DeleteAccountAsync(userId);

            _mockUserRepo.Verify(r => r.GetByIdAsync(userId), Times.Once);
        }

        [Fact]
        public async Task DeleteAccountAsync_ShouldThrow_WhenUserNotFound()
        {
            _mockUserRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _userService.DeleteAccountAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task DeleteAccountAsync_ShouldThrow_WithCorrectMessage_WhenUserNotFound()
        {
            _mockUserRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _userService.DeleteAccountAsync(Guid.NewGuid()));

            Assert.Contains("User not found", ex.Message);
        }

        [Fact]
        public async Task DeleteAccountAsync_ShouldNotCallDeleteAsync_WhenUserNotFound()
        {
            _mockUserRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _userService.DeleteAccountAsync(Guid.NewGuid()));

            _mockUserRepo.Verify(r => r.DeleteAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAccountAsync_ShouldNotCallUpdateAsync_WhenDeleting()
        {
            var userId = Guid.NewGuid();
            var user = new User { Id = userId, FullName = "Test User", Email = "test@banklite.com", PasswordHash = string.Empty };
            _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

            await _userService.DeleteAccountAsync(userId);

            _mockUserRepo.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
        }
    }
}