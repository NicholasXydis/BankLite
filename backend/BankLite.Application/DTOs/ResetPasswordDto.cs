namespace BankLite.Application.DTOs;

public class ResetPasswordDto
{
    /// <summary>The password reset token received via email. Maximum 256 characters.</summary>
    public string Token { get; init; } = string.Empty;

    /// <summary>The new password. Must be 8 to 100 characters.</summary>
    public string NewPassword { get; init; } = string.Empty;
}
