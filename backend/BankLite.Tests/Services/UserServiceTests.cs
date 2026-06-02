using BankLite.Application.DTOs;
using BankLite.Application.Exceptions;
using BankLite.Application.Services;
using BankLite.Domain.Entities;
using BankLite.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BankLite.Tests.Services;

public class UserServiceTests
{
    private readonly Mock<IRefreshTokenRepository> _mockRefreshTokenRepo;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IUserRepository> _mockUserRepo;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _mockUserRepo = new Mock<IUserRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockRefreshTokenRepo = new Mock<IRefreshTokenRepository>();
        _userService = new UserService(
            _mockUserRepo.Object,
            _mockUnitOfWork.Object,
            new NullLogger<UserService>(),
            _mockRefreshTokenRepo.Object);
    }

    private User CreateTestUser(Guid? userId = null, string? passwordHash = null)
    {
        return new User
        {
            Id = userId ?? Guid.NewGuid(),
            FullName = "Test User",
            Email = "test@banklite.com",
            PasswordHash = passwordHash ?? string.Empty,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            LastLoginAt = DateTime.UtcNow.AddDays(-1)
        };
    }

    [Fact]
    public async Task GetProfileAsync_ValidUser_ReturnsProfileWithCorrectData()
    {
        var userId = Guid.NewGuid();
        var user = CreateTestUser(userId);
        _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        var result = await _userService.GetProfileAsync(userId);

        result.Should().NotBeNull();
        result.FullName.Should().Be(user.FullName);
        result.Email.Should().Be(user.Email);
    }

    [Fact]
    public async Task GetProfileAsync_ValidUser_ReturnsCorrectCreatedAt()
    {
        var userId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow.AddDays(-30);
        var user = CreateTestUser(userId);
        user.CreatedAt = createdAt;
        _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        var result = await _userService.GetProfileAsync(userId);

        result.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public async Task GetProfileAsync_ValidUser_ReturnsCorrectLastLoginAt()
    {
        var userId = Guid.NewGuid();
        var lastLoginAt = DateTime.UtcNow.AddDays(-1);
        var user = CreateTestUser(userId);
        user.LastLoginAt = lastLoginAt;
        _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        var result = await _userService.GetProfileAsync(userId);

        result.LastLoginAt.Should().Be(lastLoginAt);
    }

    [Fact]
    public async Task GetProfileAsync_UserNeverLoggedIn_ReturnsNullLastLoginAt()
    {
        var userId = Guid.NewGuid();
        var user = CreateTestUser(userId);
        user.LastLoginAt = null;
        _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        var result = await _userService.GetProfileAsync(userId);

        result.LastLoginAt.Should().BeNull();
    }

    [Fact]
    public async Task GetProfileAsync_UserNotFound_ThrowsBadRequestException()
    {
        _mockUserRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

        var act = async () => await _userService.GetProfileAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task GetProfileAsync_UserNotFound_ThrowsWithCorrectMessage()
    {
        _mockUserRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

        var act = async () => await _userService.GetProfileAsync(Guid.NewGuid());

        var ex = await act.Should().ThrowAsync<BadRequestException>();
        ex.Which.Message.Should().Contain("User not found");
    }

    [Fact]
    public async Task GetProfileAsync_ValidUser_CallsRepositoryOnce()
    {
        var userId = Guid.NewGuid();
        var user = CreateTestUser(userId);
        _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        await _userService.GetProfileAsync(userId);

        _mockUserRepo.Verify(r => r.GetByIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_ValidRequest_ChangesPassword()
    {
        var userId = Guid.NewGuid();
        var user = CreateTestUser(userId, BCrypt.Net.BCrypt.HashPassword("OldPassword123"));
        _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        var dto = new ChangePasswordDto { CurrentPassword = "OldPassword123", NewPassword = "NewPassword123" };

        await _userService.ChangePasswordAsync(userId, dto);

        BCrypt.Net.BCrypt.Verify("NewPassword123", user.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task ChangePasswordAsync_ValidRequest_HashesNewPassword()
    {
        var userId = Guid.NewGuid();
        var user = CreateTestUser(userId, BCrypt.Net.BCrypt.HashPassword("OldPassword123"));
        _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        var dto = new ChangePasswordDto { CurrentPassword = "OldPassword123", NewPassword = "NewPassword123" };

        await _userService.ChangePasswordAsync(userId, dto);

        user.PasswordHash.Should().NotBe("NewPassword123");
        BCrypt.Net.BCrypt.Verify("NewPassword123", user.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task ChangePasswordAsync_ValidRequest_CallsUpdateAsyncOnce()
    {
        var userId = Guid.NewGuid();
        var user = CreateTestUser(userId, BCrypt.Net.BCrypt.HashPassword("OldPassword123"));
        _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        var dto = new ChangePasswordDto { CurrentPassword = "OldPassword123", NewPassword = "NewPassword123" };

        await _userService.ChangePasswordAsync(userId, dto);

        _mockUserRepo.Verify(r => r.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_ValidRequest_RevokesAllRefreshTokens()
    {
        var userId = Guid.NewGuid();
        var user = CreateTestUser(userId, BCrypt.Net.BCrypt.HashPassword("OldPassword123"));
        _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        var dto = new ChangePasswordDto { CurrentPassword = "OldPassword123", NewPassword = "NewPassword123" };

        await _userService.ChangePasswordAsync(userId, dto);

        _mockRefreshTokenRepo.Verify(r => r.RevokeAllForUserAsync(userId), Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_ValidRequest_CallsSaveAsyncOnce()
    {
        var userId = Guid.NewGuid();
        var user = CreateTestUser(userId, BCrypt.Net.BCrypt.HashPassword("OldPassword123"));
        _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        var dto = new ChangePasswordDto { CurrentPassword = "OldPassword123", NewPassword = "NewPassword123" };

        await _userService.ChangePasswordAsync(userId, dto);

        _mockUnitOfWork.Verify(r => r.SaveAsync(), Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_WrongCurrentPassword_PreservesOldHash()
    {
        var userId = Guid.NewGuid();
        var oldHash = BCrypt.Net.BCrypt.HashPassword("OldPassword123");
        var user = CreateTestUser(userId, oldHash);
        _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        var dto = new ChangePasswordDto { CurrentPassword = "WrongPassword", NewPassword = "NewPassword123" };

        await Assert.ThrowsAsync<BadRequestException>(() => _userService.ChangePasswordAsync(userId, dto));

        user.PasswordHash.Should().Be(oldHash);
    }

    [Fact]
    public async Task ChangePasswordAsync_WrongCurrentPassword_NeverCallsUpdateAsync()
    {
        var userId = Guid.NewGuid();
        var user = CreateTestUser(userId, BCrypt.Net.BCrypt.HashPassword("OldPassword123"));
        _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        var dto = new ChangePasswordDto { CurrentPassword = "WrongPassword", NewPassword = "NewPassword123" };

        await Assert.ThrowsAsync<BadRequestException>(() => _userService.ChangePasswordAsync(userId, dto));

        _mockUserRepo.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task ChangePasswordAsync_UserNotFound_NeverCallsUpdateAsync()
    {
        _mockUserRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);
        var dto = new ChangePasswordDto { CurrentPassword = "OldPassword123", NewPassword = "NewPassword123" };

        await Assert.ThrowsAsync<BadRequestException>(() =>
            _userService.ChangePasswordAsync(Guid.NewGuid(), dto));

        _mockUserRepo.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task ChangePasswordAsync_UserNotFound_ThrowsBadRequestException()
    {
        _mockUserRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);
        var dto = new ChangePasswordDto { CurrentPassword = "OldPassword123", NewPassword = "NewPassword123" };

        var act = async () => await _userService.ChangePasswordAsync(Guid.NewGuid(), dto);

        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task ChangePasswordAsync_WrongCurrentPassword_ThrowsBadRequestException()
    {
        var userId = Guid.NewGuid();
        var user = CreateTestUser(userId, BCrypt.Net.BCrypt.HashPassword("OldPassword123"));
        _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        var dto = new ChangePasswordDto { CurrentPassword = "WrongPassword", NewPassword = "NewPassword123" };

        var act = async () => await _userService.ChangePasswordAsync(userId, dto);

        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task ChangePasswordAsync_WrongCurrentPassword_ThrowsWithCorrectMessage()
    {
        var userId = Guid.NewGuid();
        var user = CreateTestUser(userId, BCrypt.Net.BCrypt.HashPassword("OldPassword123"));
        _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        var dto = new ChangePasswordDto { CurrentPassword = "WrongPassword", NewPassword = "NewPassword123" };

        var act = async () => await _userService.ChangePasswordAsync(userId, dto);

        var ex = await act.Should().ThrowAsync<BadRequestException>();
        ex.Which.Message.Should().Contain("incorrect");
    }

    [Fact]
    public async Task ChangePasswordAsync_UserNotFound_ThrowsWithCorrectMessage()
    {
        _mockUserRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);
        var dto = new ChangePasswordDto { CurrentPassword = "OldPassword123", NewPassword = "NewPassword123" };

        var act = async () => await _userService.ChangePasswordAsync(Guid.NewGuid(), dto);

        var ex = await act.Should().ThrowAsync<BadRequestException>();
        ex.Which.Message.Should().Contain("User not found");
    }

    [Fact]
    public async Task DeleteAccountAsync_ValidUser_CallsDeleteAsyncWithCorrectUser()
    {
        var userId = Guid.NewGuid();
        var user = CreateTestUser(userId);
        _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        await _userService.DeleteAccountAsync(userId);

        _mockUserRepo.Verify(r => r.DeleteAsync(user), Times.Once);
    }

    [Fact]
    public async Task DeleteAccountAsync_ValidUser_CallsGetByIdAsyncOnce()
    {
        var userId = Guid.NewGuid();
        var user = CreateTestUser(userId);
        _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        await _userService.DeleteAccountAsync(userId);

        _mockUserRepo.Verify(r => r.GetByIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task DeleteAccountAsync_ValidUser_CallsSaveAsyncOnce()
    {
        var userId = Guid.NewGuid();
        var user = CreateTestUser(userId);
        _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        await _userService.DeleteAccountAsync(userId);

        _mockUnitOfWork.Verify(r => r.SaveAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAccountAsync_UserNotFound_ThrowsBadRequestException()
    {
        _mockUserRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

        var act = async () => await _userService.DeleteAccountAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task DeleteAccountAsync_UserNotFound_ThrowsWithCorrectMessage()
    {
        _mockUserRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

        var act = async () => await _userService.DeleteAccountAsync(Guid.NewGuid());

        var ex = await act.Should().ThrowAsync<BadRequestException>();
        ex.Which.Message.Should().Contain("User not found");
    }

    [Fact]
    public async Task DeleteAccountAsync_UserNotFound_NeverCallsDeleteAsync()
    {
        _mockUserRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<BadRequestException>(() => _userService.DeleteAccountAsync(Guid.NewGuid()));

        _mockUserRepo.Verify(r => r.DeleteAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAccountAsync_ValidUser_NeverCallsUpdateAsync()
    {
        var userId = Guid.NewGuid();
        var user = CreateTestUser(userId);
        _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        await _userService.DeleteAccountAsync(userId);

        _mockUserRepo.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
    }
}
