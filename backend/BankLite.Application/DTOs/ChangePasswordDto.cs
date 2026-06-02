namespace BankLite.Application.DTOs;

public class ChangePasswordDto
{
    /// <summary>The user's current password.</summary>
    public string CurrentPassword { get; init; } = string.Empty;

    /// <summary>The new password. Minimum 8 characters.</summary>
    public string NewPassword { get; init; } = string.Empty;
}