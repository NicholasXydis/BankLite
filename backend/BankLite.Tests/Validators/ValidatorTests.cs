using BankLite.Application.DTOs;
using BankLite.Application.Validators;
using BankLite.Domain.Entities;
using FluentAssertions;

namespace BankLite.Tests.Validators;

public class ValidatorTests
{
    private readonly ChangePasswordValidator _changePasswordValidator = new();
    private readonly ChatMessageValidator _chatMessageValidator = new();
    private readonly CreateAccountValidator _createAccountValidator = new();
    private readonly DepositWithdrawValidator _depositWithdrawValidator = new();
    private readonly ExternalTransferValidator _externalTransferValidator = new();
    private readonly ForgotPasswordValidator _forgotPasswordValidator = new();
    private readonly LoginUserValidator _loginUserValidator = new();
    private readonly RegisterUserValidator _registerUserValidator = new();
    private readonly ResetPasswordValidator _resetPasswordValidator = new();
    private readonly TransferValidator _transferValidator = new();

    [Theory]
    [InlineData(100)]
    [InlineData(0.01)]
    [InlineData(1000000)]
    public async Task DepositWithdrawValidator_ValidAmount_PassesValidation(double amount)
    {
        var dto = new DepositWithdrawDto { AccountId = Guid.NewGuid(), Amount = (decimal)amount };

        var result = await _depositWithdrawValidator.ValidateAsync(dto);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    [InlineData(1000001)]
    public async Task DepositWithdrawValidator_InvalidAmount_FailsValidation(double amount)
    {
        var dto = new DepositWithdrawDto { AccountId = Guid.NewGuid(), Amount = (decimal)amount };

        var result = await _depositWithdrawValidator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task DepositWithdrawValidator_EmptyAccountId_FailsValidation()
    {
        var dto = new DepositWithdrawDto { AccountId = Guid.Empty, Amount = 100 };

        var result = await _depositWithdrawValidator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task TransferValidator_ValidRequest_PassesValidation()
    {
        var dto = new TransferDto { FromAccountId = Guid.NewGuid(), ToAccountId = Guid.NewGuid(), Amount = 100 };

        var result = await _transferValidator.ValidateAsync(dto);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    [InlineData(1000001)]
    public async Task TransferValidator_InvalidAmount_FailsValidation(double amount)
    {
        var dto = new TransferDto
        { FromAccountId = Guid.NewGuid(), ToAccountId = Guid.NewGuid(), Amount = (decimal)amount };

        var result = await _transferValidator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task TransferValidator_SameAccount_FailsValidation()
    {
        var id = Guid.NewGuid();
        var dto = new TransferDto { FromAccountId = id, ToAccountId = id, Amount = 100 };

        var result = await _transferValidator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("same account"));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task TransferValidator_EmptyAccountIds_FailsValidation(bool fromEmpty, bool toEmpty)
    {
        var dto = new TransferDto
        {
            FromAccountId = fromEmpty ? Guid.Empty : Guid.NewGuid(),
            ToAccountId = toEmpty ? Guid.Empty : Guid.NewGuid(),
            Amount = 100
        };

        var result = await _transferValidator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ExternalTransferValidator_ValidRequest_PassesValidation()
    {
        var dto = new ExternalTransferDto
        { FromAccountId = Guid.NewGuid(), ToAccountNumber = "ACC123456789", Amount = 100 };

        var result = await _externalTransferValidator.ValidateAsync(dto);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    [InlineData(1000001)]
    public async Task ExternalTransferValidator_InvalidAmount_FailsValidation(double amount)
    {
        var dto = new ExternalTransferDto
        { FromAccountId = Guid.NewGuid(), ToAccountNumber = "ACC123456789", Amount = (decimal)amount };

        var result = await _externalTransferValidator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("SHORT")]
    public async Task ExternalTransferValidator_InvalidAccountNumber_FailsValidation(string accountNumber)
    {
        var dto = new ExternalTransferDto
        { FromAccountId = Guid.NewGuid(), ToAccountNumber = accountNumber, Amount = 100 };

        var result = await _externalTransferValidator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ExternalTransferValidator_EmptyAccountNumber_FailsWithCorrectMessage()
    {
        var dto = new ExternalTransferDto { FromAccountId = Guid.NewGuid(), ToAccountNumber = "", Amount = 100 };

        var result = await _externalTransferValidator.ValidateAsync(dto);

        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("account number"));
    }

    [Fact]
    public async Task ExternalTransferValidator_EmptyFromAccountId_FailsValidation()
    {
        var dto = new ExternalTransferDto
        { FromAccountId = Guid.Empty, ToAccountNumber = "ACC123456789", Amount = 100 };

        var result = await _externalTransferValidator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("Test User", "test@banklite.com", "Password123")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA", "test@banklite.com", "Password123")]
    [InlineData("Test User", "test@banklite.com", "Abcde123")]
    [InlineData("Test User", "test@banklite.com", "Abcdefgh1")]
    public async Task RegisterUserValidator_ValidData_PassesValidation(string fullName, string email, string password)
    {
        var dto = new RegisterUserDto { FullName = fullName, Email = email, Password = password };

        var result = await _registerUserValidator.ValidateAsync(dto);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "test@banklite.com", "Password123")]
    [InlineData("   ", "test@banklite.com", "Password123")]
    [InlineData(" Test User", "test@banklite.com", "Password123")]
    [InlineData("Test User ", "test@banklite.com", "Password123")]
    public async Task RegisterUserValidator_InvalidFullName_FailsValidation(string fullName, string email,
        string password)
    {
        var dto = new RegisterUserDto { FullName = fullName, Email = email, Password = password };

        var result = await _registerUserValidator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task RegisterUserValidator_FullNameTooLong_FailsValidation()
    {
        var dto = new RegisterUserDto
        { FullName = new string('A', 51), Email = "test@banklite.com", Password = "Password123" };

        var result = await _registerUserValidator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("notanemail")]
    [InlineData("testbanklite.com")]
    [InlineData("test@")]
    public async Task RegisterUserValidator_InvalidEmail_FailsValidation(string email)
    {
        var dto = new RegisterUserDto { FullName = "Test User", Email = email, Password = "Password123" };

        var result = await _registerUserValidator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task RegisterUserValidator_EmailTooLong_FailsValidation()
    {
        var dto = new RegisterUserDto
        { FullName = "Test User", Email = new string('a', 252) + "@b.com", Password = "Password123" };

        var result = await _registerUserValidator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("Abcde12")]
    public async Task RegisterUserValidator_PasswordTooShort_FailsValidation(string password)
    {
        var dto = new RegisterUserDto { FullName = "Test User", Email = "test@banklite.com", Password = password };

        var result = await _registerUserValidator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task RegisterUserValidator_PasswordTooLong_FailsValidation()
    {
        var dto = new RegisterUserDto
        { FullName = "Test User", Email = "test@banklite.com", Password = new string('a', 101) };

        var result = await _registerUserValidator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("test@banklite.com", "Password123")]
    [InlineData("test@banklite.com", "Abcde123")]
    [InlineData("test@mail.banklite.com", "Password123")]
    public async Task LoginUserValidator_ValidData_PassesValidation(string email, string password)
    {
        var dto = new LoginUserDto { Email = email, Password = password };

        var result = await _loginUserValidator.ValidateAsync(dto);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "Password123")]
    [InlineData("notanemail", "Password123")]
    [InlineData("testbanklite.com", "Password123")]
    public async Task LoginUserValidator_InvalidEmail_FailsValidation(string email, string password)
    {
        var dto = new LoginUserDto { Email = email, Password = password };

        var result = await _loginUserValidator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task LoginUserValidator_EmailTooLong_FailsValidation()
    {
        var dto = new LoginUserDto { Email = new string('a', 252) + "@b.com", Password = "Password123" };

        var result = await _loginUserValidator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("Abcde12")]
    public async Task LoginUserValidator_PasswordTooShort_FailsValidation(string password)
    {
        var dto = new LoginUserDto { Email = "test@banklite.com", Password = password };

        var result = await _loginUserValidator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task LoginUserValidator_PasswordTooLong_FailsValidation()
    {
        var dto = new LoginUserDto { Email = "test@banklite.com", Password = new string('a', 101) };

        var result = await _loginUserValidator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("OldPassword123", "NewPassword123")]
    [InlineData("OldPass1", "NewPass1")]
    public async Task ChangePasswordValidator_ValidData_PassesValidation(string current, string newPass)
    {
        var dto = new ChangePasswordDto { CurrentPassword = current, NewPassword = newPass };

        var result = await _changePasswordValidator.ValidateAsync(dto);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "NewPassword123")]
    [InlineData("OldPassword123", "")]
    [InlineData("OldPassword123", "abc")]
    [InlineData("OldPassword123", "abcde12")]
    public async Task ChangePasswordValidator_InvalidPasswords_FailsValidation(string current, string newPass)
    {
        var dto = new ChangePasswordDto { CurrentPassword = current, NewPassword = newPass };

        var result = await _changePasswordValidator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ChangePasswordValidator_PasswordTooLong_FailsValidation()
    {
        var dto = new ChangePasswordDto { CurrentPassword = new string('a', 101), NewPassword = "NewPassword123" };

        var result = await _changePasswordValidator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ChangePasswordValidator_SamePassword_FailsValidationWithCorrectMessage()
    {
        var dto = new ChangePasswordDto { CurrentPassword = "Password123", NewPassword = "Password123" };

        var result = await _changePasswordValidator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("different"));
    }

    [Theory]
    [InlineData(AccountType.Chequing)]
    [InlineData(AccountType.Savings)]
    public async Task CreateAccountValidator_ValidType_PassesValidation(AccountType type)
    {
        var dto = new CreateAccountDto { Type = type };

        var result = await _createAccountValidator.ValidateAsync(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAccountValidator_InvalidType_FailsValidation()
    {
        var dto = new CreateAccountDto { Type = (AccountType)99 };

        var result = await _createAccountValidator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ForgotPasswordValidator_ValidEmail_PassesValidation()
    {
        var dto = new ForgotPasswordDto { Email = "test@banklite.com" };

        var result = await _forgotPasswordValidator.ValidateAsync(dto);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("notanemail")]
    public async Task ForgotPasswordValidator_InvalidEmail_FailsValidation(string email)
    {
        var dto = new ForgotPasswordDto { Email = email };

        var result = await _forgotPasswordValidator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ResetPasswordValidator_ValidRequest_PassesValidation()
    {
        var dto = new ResetPasswordDto { Token = "valid-token", NewPassword = "Password123" };

        var result = await _resetPasswordValidator.ValidateAsync(dto);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "Password123")]
    [InlineData("valid-token", "short")]
    public async Task ResetPasswordValidator_InvalidData_FailsValidation(string token, string password)
    {
        var dto = new ResetPasswordDto { Token = token, NewPassword = password };

        var result = await _resetPasswordValidator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ResetPasswordValidator_TokenTooLong_FailsValidation()
    {
        var dto = new ResetPasswordDto { Token = new string('a', 257), NewPassword = "Password123" };

        var result = await _resetPasswordValidator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ChatMessageValidator_ValidContent_PassesValidation()
    {
        var dto = new ChatMessageDto("Hello Alfred");

        var result = await _chatMessageValidator.ValidateAsync(dto);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    public async Task ChatMessageValidator_EmptyContent_FailsValidation(string content)
    {
        var dto = new ChatMessageDto(content);

        var result = await _chatMessageValidator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ChatMessageValidator_ContentTooLong_FailsValidation()
    {
        var dto = new ChatMessageDto(new string('a', 201));

        var result = await _chatMessageValidator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
    }
}