using BankLite.Application.DTOs;
using BankLite.Application.Validators;
using BankLite.Domain.Entities;
using Xunit;

namespace BankLite.Tests.Validators
{
    public class ValidatorTests
    {
        private readonly DepositWithdrawValidator _depositWithdrawValidator = new();
        private readonly TransferValidator _transferValidator = new();
        private readonly ExternalTransferValidator _externalTransferValidator = new();
        private readonly RegisterUserValidator _registerUserValidator = new();
        private readonly LoginUserValidator _loginUserValidator = new();
        private readonly ChangePasswordValidator _changePasswordValidator = new();
        private readonly CreateAccountValidator _createAccountValidator = new();
        private readonly ForgotPasswordValidator _forgotPasswordValidator = new();
        private readonly ResetPasswordValidator _resetPasswordValidator = new();
        private readonly ChatMessageValidator _chatMessageValidator = new();

        [Fact]
        public async Task DepositWithdrawValidator_ShouldPass_WhenValid()
        {
            var dto = new DepositWithdrawDto { AccountId = Guid.NewGuid(), Amount = 100 };
            var result = await _depositWithdrawValidator.ValidateAsync(dto);
            Assert.True(result.IsValid);
        }

        [Fact]
        public async Task DepositWithdrawValidator_ShouldPass_WhenAmountIsMinimum()
        {
            var dto = new DepositWithdrawDto { AccountId = Guid.NewGuid(), Amount = 0.01m };
            var result = await _depositWithdrawValidator.ValidateAsync(dto);
            Assert.True(result.IsValid);
        }

        [Fact]
        public async Task DepositWithdrawValidator_ShouldPass_WhenAmountIsAtLimit()
        {
            var dto = new DepositWithdrawDto { AccountId = Guid.NewGuid(), Amount = 1000000 };
            var result = await _depositWithdrawValidator.ValidateAsync(dto);
            Assert.True(result.IsValid);
        }

        [Fact]
        public async Task DepositWithdrawValidator_ShouldFail_WhenAmountIsZero()
        {
            var dto = new DepositWithdrawDto { AccountId = Guid.NewGuid(), Amount = 0 };
            var result = await _depositWithdrawValidator.ValidateAsync(dto);
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task DepositWithdrawValidator_ShouldFail_WhenAmountIsNegative()
        {
            var dto = new DepositWithdrawDto { AccountId = Guid.NewGuid(), Amount = -50 };
            var result = await _depositWithdrawValidator.ValidateAsync(dto);
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task DepositWithdrawValidator_ShouldFail_WhenAmountExceedsLimit()
        {
            var dto = new DepositWithdrawDto { AccountId = Guid.NewGuid(), Amount = 1000001 };
            var result = await _depositWithdrawValidator.ValidateAsync(dto);
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task DepositWithdrawValidator_ShouldFail_WhenAccountIdIsEmpty()
        {
            var dto = new DepositWithdrawDto { AccountId = Guid.Empty, Amount = 100 };
            var result = await _depositWithdrawValidator.ValidateAsync(dto);
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task TransferValidator_ShouldPass_WhenValid()
        {
            var fromId = Guid.NewGuid();
            var dto = new TransferDto { FromAccountId = fromId, ToAccountId = Guid.NewGuid(), Amount = 100 };
            var result = await _transferValidator.ValidateAsync(dto);
            Assert.True(result.IsValid);
        }

        [Fact]
        public async Task TransferValidator_ShouldPass_WhenAmountIsLarge()
        {
            var fromId = Guid.NewGuid();
            var dto = new TransferDto { FromAccountId = fromId, ToAccountId = Guid.NewGuid(), Amount = 999999 };
            var result = await _transferValidator.ValidateAsync(dto);
            Assert.True(result.IsValid);
        }

        [Fact]
        public async Task TransferValidator_ShouldFail_WhenAmountIsZero()
        {
            var fromId = Guid.NewGuid();
            var dto = new TransferDto { FromAccountId = fromId, ToAccountId = Guid.NewGuid(), Amount = 0 };
            var result = await _transferValidator.ValidateAsync(dto);
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task TransferValidator_ShouldFail_WhenAmountIsNegative()
        {
            var fromId = Guid.NewGuid();
            var dto = new TransferDto { FromAccountId = fromId, ToAccountId = Guid.NewGuid(), Amount = -100 };
            var result = await _transferValidator.ValidateAsync(dto);
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task TransferValidator_ShouldFail_WhenSameAccount()
        {
            var id = Guid.NewGuid();
            var dto = new TransferDto { FromAccountId = id, ToAccountId = id, Amount = 100 };
            var result = await _transferValidator.ValidateAsync(dto);
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task TransferValidator_ShouldContainCorrectMessage_WhenSameAccount()
        {
            var id = Guid.NewGuid();
            var dto = new TransferDto { FromAccountId = id, ToAccountId = id, Amount = 100 };
            var result = await _transferValidator.ValidateAsync(dto);
            Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("same account"));
        }

        [Fact]
        public async Task TransferValidator_ShouldFail_WhenFromAccountIdIsEmpty()
        {
            var dto = new TransferDto { FromAccountId = Guid.Empty, ToAccountId = Guid.NewGuid(), Amount = 100 };
            var result = await _transferValidator.ValidateAsync(dto);
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task TransferValidator_ShouldFail_WhenToAccountIdIsEmpty()
        {
            var dto = new TransferDto { FromAccountId = Guid.NewGuid(), ToAccountId = Guid.Empty, Amount = 100 };
            var result = await _transferValidator.ValidateAsync(dto);
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task TransferValidator_ShouldFail_WhenBothAccountIdsAreEmpty()
        {
            var dto = new TransferDto { FromAccountId = Guid.Empty, ToAccountId = Guid.Empty, Amount = 100 };
            var result = await _transferValidator.ValidateAsync(dto);
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task TransferValidator_ShouldFail_WhenAmountExceedsLimit()
        {
            var fromId = Guid.NewGuid();
            var dto = new TransferDto { FromAccountId = fromId, ToAccountId = Guid.NewGuid(), Amount = 1000001 };
            var result = await _transferValidator.ValidateAsync(dto);
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task ExternalTransferValidator_ShouldPass_WhenValid()
        {
            var dto = new ExternalTransferDto { FromAccountId = Guid.NewGuid(), ToAccountNumber = "ACC123456789", Amount = 100 };
            var result = await _externalTransferValidator.ValidateAsync(dto);
            Assert.True(result.IsValid);
        }

        [Fact]
        public async Task ExternalTransferValidator_ShouldPass_WhenAmountIsAtLimit()
        {
            var dto = new ExternalTransferDto { FromAccountId = Guid.NewGuid(), ToAccountNumber = "ACC123456789", Amount = 1000000 };
            var result = await _externalTransferValidator.ValidateAsync(dto);
            Assert.True(result.IsValid);
        }

        [Fact]
        public async Task ExternalTransferValidator_ShouldFail_WhenAmountIsZero()
        {
            var dto = new ExternalTransferDto { FromAccountId = Guid.NewGuid(), ToAccountNumber = "ACC123456789", Amount = 0 };
            var result = await _externalTransferValidator.ValidateAsync(dto);
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task ExternalTransferValidator_ShouldFail_WhenAmountIsNegative()
        {
            var dto = new ExternalTransferDto { FromAccountId = Guid.NewGuid(), ToAccountNumber = "ACC123456789", Amount = -100 };
            var result = await _externalTransferValidator.ValidateAsync(dto);
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task ExternalTransferValidator_ShouldFail_WhenAmountExceedsLimit()
        {
            var dto = new ExternalTransferDto { FromAccountId = Guid.NewGuid(), ToAccountNumber = "ACC123456789", Amount = 1000001 };
            var result = await _externalTransferValidator.ValidateAsync(dto);
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task ExternalTransferValidator_ShouldFail_WhenToAccountNumberIsEmpty()
        {
            var dto = new ExternalTransferDto { FromAccountId = Guid.NewGuid(), ToAccountNumber = "", Amount = 100 };
            var result = await _externalTransferValidator.ValidateAsync(dto);
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task ExternalTransferValidator_ShouldFail_WhenToAccountNumberIsWhitespace()
        {
            var dto = new ExternalTransferDto { FromAccountId = Guid.NewGuid(), ToAccountNumber = "   ", Amount = 100 };
            var result = await _externalTransferValidator.ValidateAsync(dto);
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task ExternalTransferValidator_ShouldFail_WhenFromAccountIdIsEmpty()
        {
            var dto = new ExternalTransferDto { FromAccountId = Guid.Empty, ToAccountNumber = "ACC123456789", Amount = 100 };
            var result = await _externalTransferValidator.ValidateAsync(dto);
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task ExternalTransferValidator_ShouldContainCorrectMessage_WhenToAccountNumberIsEmpty()
        {
            var dto = new ExternalTransferDto { FromAccountId = Guid.NewGuid(), ToAccountNumber = "", Amount = 100 };
            var result = await _externalTransferValidator.ValidateAsync(dto);
            Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("account number"));
        }

        [Fact]
        public async Task ExternalTransferValidator_ShouldFail_WhenAccountNumberWrongLength()
        {
            var dto = new ExternalTransferDto { FromAccountId = Guid.NewGuid(), ToAccountNumber = "SHORT", Amount = 100 };
            var result = await _externalTransferValidator.ValidateAsync(dto);
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task RegisterUserValidator_ShouldPass_WhenValid()
        {
            var dto = new RegisterUserDto { FullName = "Test User", Email = "test@banklite.com", Password = "Password123" };
            var result = await _registerUserValidator.ValidateAsync(dto);
            Assert.True(result.IsValid);
        }

        [Fact]
        public async Task RegisterUserValidator_ShouldPass_WhenNameIsExactly50Chars()
        {
            var dto = new RegisterUserDto { FullName = new string('A', 50), Email = "test@banklite.com", Password = "Password123" };
            var result = await _registerUserValidator.ValidateAsync(dto);
            Assert.True(result.IsValid);
        }

        [Fact]
        public async Task RegisterUserValidator_ShouldPass_WhenPasswordIsExactly8Chars()
        {
            var dto = new RegisterUserDto { FullName = "Test User", Email = "test@banklite.com", Password = "Abcde123" };
            var result = await _registerUserValidator.ValidateAsync(dto);
            Assert.True(result.IsValid);
        }

        [Fact]
        public async Task RegisterUserValidator_ShouldPass_WhenPasswordIsExactly100Chars()
        {
            var dto = new RegisterUserDto { FullName = "Test User", Email = "test@banklite.com", Password = new string('a', 100) };
            var result = await _registerUserValidator.ValidateAsync(dto);
            Assert.True(result.IsValid);
        }

        [Fact]
        public async Task RegisterUserValidator_ShouldFail_WhenFullNameIsEmpty()
        {
            var dto = new RegisterUserDto { FullName = "", Email = "test@banklite.com", Password = "Password123" };
            var result = await _registerUserValidator.ValidateAsync(dto);
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task RegisterUserValidator_ShouldFail_WhenFullNameIsWhitespaceOnly()
        {
            var dto = new RegisterUserDto { FullName = "   ", Email = "test@banklite.com", Password = "Password123" };
            var result = await _registerUserValidator.ValidateAsync(dto);
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task RegisterUserValidator_ShouldFail_WhenFullNameTooLong()
        {
            var dto = new RegisterUserDto { FullName = new string('A', 51), Email = "test@banklite.com", Password = "Password123" };
            var result = await _registerUserValidator.ValidateAsync(dto);
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task RegisterUserValidator_ShouldFail_WhenFullNameHasLeadingSpaces()
        {
            var dto = new RegisterUserDto { FullName = " Test User", Email = "test@banklite.com", Password = "Password123" };
            var result = await _registerUserValidator.ValidateAsync(dto);
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task RegisterUserValidator_ShouldFail_WhenFullNameHasTrailingSpaces()
        {
            var dto = new RegisterUserDto { FullName = "Test User ", Email = "test@banklite.com", Password = "Password123" };
            var result = await _registerUserValidator.ValidateAsync(dto);
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task RegisterUserValidator_ShouldFail_WhenEmailIsEmpty()
        {
            var dto = new RegisterUserDto { FullName = "Test User", Email = "", Password = "Password123" };
            var result = await _registerUserValidator.ValidateAsync(dto);
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task RegisterUserValidator_ShouldFail_WhenEmailIsInvalid()
        {
            var dto = new RegisterUserDto { FullName = "Test User", Email = "notanemail", Password = "Password123" };
            var result = await _registerUserValidator.ValidateAsync(dto);
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task RegisterUserValidator_ShouldFail_WhenEmailMissingAtSign()
        {
            var dto = new RegisterUserDto { FullName = "Test User", Email = "testbanklite.com", Password = "Password123" };
            var result = await _registerUserValidator.ValidateAsync(dto);
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task RegisterUserValidator_ShouldFail_WhenEmailMissingDomain()
        {
            var dto = new RegisterUserDto { FullName = "Test User", Email = "test@", Password = "Password123" };
            var result = await _registerUserValidator.ValidateAsync(dto);
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task RegisterUserValidator_ShouldFail_WhenEmailTooLong()
        {
            var dto = new RegisterUserDto { FullName = "Test User", Email = new string('a', 252) + "@b.com", Password = "Password123" };
            var result = await _registerUserValidator.ValidateAsync(dto);
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task RegisterUserValidator_ShouldFail_WhenPasswordIsEmpty()
        {
            var dto = new RegisterUserDto { FullName = "Test User", Email = "test@banklite.com", Password = "" };
            var result = await _registerUserValidator.ValidateAsync(dto);
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task RegisterUserValidator_ShouldFail_WhenPasswordTooShort()
        {
            var dto = new RegisterUserDto { FullName = "Test User", Email = "test@banklite.com", Password = "abc" };
            var result = await _registerUserValidator.ValidateAsync(dto);
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task RegisterUserValidator_ShouldFail_WhenPasswordIs7Chars()
        {
            var dto = new RegisterUserDto { FullName = "Test User", Email = "test@banklite.com", Password = "Abcde12" };
            var result = await _registerUserValidator.ValidateAsync(dto);
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task RegisterUserValidator_ShouldFail_WhenPasswordTooLong()
        {
            var dto = new RegisterUserDto { FullName = "Test User", Email = "test@banklite.com", Password = new string('a', 101) };
            var result = await _registerUserValidator.ValidateAsync(dto);
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task ForgotPasswordValidator_ShouldPass_WhenEmailValid()
        {
            var dto = new ForgotPasswordDto { Email = "test@banklite.com" };
            var result = await _forgotPasswordValidator.ValidateAsync(dto);
            Assert.True(result.IsValid);
        }

        [Fact]
        public async Task ForgotPasswordValidator_ShouldFail_WhenEmailEmpty()
        {
            var dto = new ForgotPasswordDto { Email = "" };
            var result = await _forgotPasswordValidator.ValidateAsync(dto);
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task ForgotPasswordValidator_ShouldFail_WhenEmailFormatInvalid()
        {
            var dto = new ForgotPasswordDto { Email = "notanemail" };
            var result = await _forgotPasswordValidator.ValidateAsync(dto);
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task ResetPasswordValidator_ShouldPass_WhenValid()
        {
            var dto = new ResetPasswordDto { Token = "valid-token", NewPassword = "Password123" };
            var result = await _resetPasswordValidator.ValidateAsync(dto);
            Assert.True(result.IsValid);
        }

        [Fact]
        public async Task ResetPasswordValidator_ShouldFail_WhenTokenEmpty()
        {
            var dto = new ResetPasswordDto { Token = "", NewPassword = "Password123" };
            var result = await _resetPasswordValidator.ValidateAsync(dto);
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task ResetPasswordValidator_ShouldFail_WhenPasswordTooShort()
        {
            var dto = new ResetPasswordDto { Token = "valid-token", NewPassword = "short" };
            var result = await _resetPasswordValidator.ValidateAsync(dto);
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task ResetPasswordValidator_ShouldFail_WhenTokenTooLong()
        {
            var dto = new ResetPasswordDto { Token = new string('a', 257), NewPassword = "Password123" };
            var result = await _resetPasswordValidator.ValidateAsync(dto);
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task ChatMessageValidator_ShouldPass_WhenValid()
        {
            var dto = new ChatMessageDto("Hello Alfred");
            var result = await _chatMessageValidator.ValidateAsync(dto);
            Assert.True(result.IsValid);
        }

        [Fact]
        public async Task ChatMessageValidator_ShouldFail_WhenContentEmpty()
        {
            var dto = new ChatMessageDto("");
            var result = await _chatMessageValidator.ValidateAsync(dto);
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task ChatMessageValidator_ShouldFail_WhenContentTooLong()
        {
            var dto = new ChatMessageDto(new string('a', 201));
            var result = await _chatMessageValidator.ValidateAsync(dto);
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task LoginUserValidator_ShouldPass_WhenValid()
        {
            var dto = new LoginUserDto { Email = "test@banklite.com", Password = "Password123" };
            var result = await _loginUserValidator.ValidateAsync(dto);
            Assert.True(result.IsValid);
        }

        [Fact]
        public async Task LoginUserValidator_ShouldPass_WhenPasswordIsExactly8Chars()
        {
            var dto = new LoginUserDto { Email = "test@banklite.com", Password = "Abcde123" };
            var result = await _loginUserValidator.ValidateAsync(dto);
            Assert.True(result.IsValid);
        }

        [Fact]
        public async Task LoginUserValidator_ShouldPass_WhenEmailHasSubdomain()
        {
            var dto = new LoginUserDto { Email = "test@mail.banklite.com", Password = "Password123" };
            var result = await _loginUserValidator.ValidateAsync(dto);
            Assert.True(result.IsValid);
        }

        [Fact]
        public async Task LoginUserValidator_ShouldFail_WhenEmailIsEmpty()
        {
            var dto = new LoginUserDto { Email = "", Password = "Password123" };
            var result = await _loginUserValidator.ValidateAsync(dto);
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task LoginUserValidator_ShouldFail_WhenEmailIsInvalid()
        {
            var dto = new LoginUserDto { Email = "notanemail", Password = "Password123" };
            var result = await _loginUserValidator.ValidateAsync(dto);
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task LoginUserValidator_ShouldFail_WhenEmailMissingAtSign()
        {
            var dto = new LoginUserDto { Email = "testbanklite.com", Password = "Password123" };
            var result = await _loginUserValidator.ValidateAsync(dto);
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task LoginUserValidator_ShouldFail_WhenEmailTooLong()
        {
            var dto = new LoginUserDto { Email = new string('a', 252) + "@b.com", Password = "Password123" };
            var result = await _loginUserValidator.ValidateAsync(dto);
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task LoginUserValidator_ShouldFail_WhenPasswordIsEmpty()
        {
            var dto = new LoginUserDto { Email = "test@banklite.com", Password = "" };
            var result = await _loginUserValidator.ValidateAsync(dto);
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task LoginUserValidator_ShouldFail_WhenPasswordTooShort()
        {
            var dto = new LoginUserDto { Email = "test@banklite.com", Password = "abc" };
            var result = await _loginUserValidator.ValidateAsync(dto);
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task LoginUserValidator_ShouldFail_WhenPasswordIs7Chars()
        {
            var dto = new LoginUserDto { Email = "test@banklite.com", Password = "Abcde12" };
            var result = await _loginUserValidator.ValidateAsync(dto);
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task LoginUserValidator_ShouldFail_WhenPasswordTooLong()
        {
            var dto = new LoginUserDto { Email = "test@banklite.com", Password = new string('a', 101) };
            var result = await _loginUserValidator.ValidateAsync(dto);
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task ChangePasswordValidator_ShouldPass_WhenValid()
        {
            var dto = new ChangePasswordDto { CurrentPassword = "OldPassword123", NewPassword = "NewPassword123" };
            var result = await _changePasswordValidator.ValidateAsync(dto);
            Assert.True(result.IsValid);
        }

        [Fact]
        public async Task ChangePasswordValidator_ShouldPass_WhenPasswordIsExactly8Chars()
        {
            var dto = new ChangePasswordDto { CurrentPassword = "OldPass1", NewPassword = "NewPass1" };
            var result = await _changePasswordValidator.ValidateAsync(dto);
            Assert.True(result.IsValid);
        }

        [Fact]
        public async Task ChangePasswordValidator_ShouldPass_WhenPasswordIsExactly100Chars()
        {
            var dto = new ChangePasswordDto { CurrentPassword = new string('a', 100), NewPassword = new string('b', 100) };
            var result = await _changePasswordValidator.ValidateAsync(dto);
            Assert.True(result.IsValid);
        }

        [Fact]
        public async Task ChangePasswordValidator_ShouldFail_WhenCurrentPasswordIsEmpty()
        {
            var dto = new ChangePasswordDto { CurrentPassword = "", NewPassword = "NewPassword123" };
            var result = await _changePasswordValidator.ValidateAsync(dto);
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task ChangePasswordValidator_ShouldFail_WhenNewPasswordIsEmpty()
        {
            var dto = new ChangePasswordDto { CurrentPassword = "OldPassword123", NewPassword = "" };
            var result = await _changePasswordValidator.ValidateAsync(dto);
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task ChangePasswordValidator_ShouldFail_WhenNewPasswordTooShort()
        {
            var dto = new ChangePasswordDto { CurrentPassword = "OldPassword123", NewPassword = "abc" };
            var result = await _changePasswordValidator.ValidateAsync(dto);
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task ChangePasswordValidator_ShouldFail_WhenNewPasswordIs7Chars()
        {
            var dto = new ChangePasswordDto { CurrentPassword = "OldPassword123", NewPassword = "abcde12" };
            var result = await _changePasswordValidator.ValidateAsync(dto);
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task ChangePasswordValidator_ShouldFail_WhenNewPasswordTooLong()
        {
            var dto = new ChangePasswordDto { CurrentPassword = "OldPassword123", NewPassword = new string('a', 101) };
            var result = await _changePasswordValidator.ValidateAsync(dto);
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task ChangePasswordValidator_ShouldFail_WhenCurrentPasswordTooLong()
        {
            var dto = new ChangePasswordDto { CurrentPassword = new string('a', 101), NewPassword = "NewPassword123" };
            var result = await _changePasswordValidator.ValidateAsync(dto);
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task ChangePasswordValidator_ShouldFail_WhenSamePassword()
        {
            var dto = new ChangePasswordDto { CurrentPassword = "Password123", NewPassword = "Password123" };
            var result = await _changePasswordValidator.ValidateAsync(dto);
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task ChangePasswordValidator_ShouldContainCorrectMessage_WhenSamePassword()
        {
            var dto = new ChangePasswordDto { CurrentPassword = "Password123", NewPassword = "Password123" };
            var result = await _changePasswordValidator.ValidateAsync(dto);
            Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("different"));
        }

        [Fact]
        public async Task CreateAccountValidator_ShouldPass_WhenChequing()
        {
            var dto = new CreateAccountDto { Type = AccountType.Chequing };
            var result = await _createAccountValidator.ValidateAsync(dto);
            Assert.True(result.IsValid);
        }

        [Fact]
        public async Task CreateAccountValidator_ShouldPass_WhenSavings()
        {
            var dto = new CreateAccountDto { Type = AccountType.Savings };
            var result = await _createAccountValidator.ValidateAsync(dto);
            Assert.True(result.IsValid);
        }

        [Fact]
        public async Task CreateAccountValidator_ShouldFail_WhenInvalidType()
        {
            var dto = new CreateAccountDto { Type = (AccountType)99 };
            var result = await _createAccountValidator.ValidateAsync(dto);
            Assert.False(result.IsValid);
        }
    }
}