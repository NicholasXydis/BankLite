namespace BankLite.Application.DTOs;

public class ChangePasswordDto
{
    /// <summary>The user's current password. Maximum 100 characters.</summary>
    public string CurrentPassword { get; init; } = string.Empty;

    /// <summary>The new password. Must be 8 to 100 characters and different from the current password.</summary>
    public string NewPassword { get; init; } = string.Empty;
}
