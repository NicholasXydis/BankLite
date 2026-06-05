namespace BankLite.Application.DTOs;

public class RegisterUserDto
{
    /// <summary>The user's full name. Letters and spaces only, no leading or trailing spaces, maximum 50 characters.</summary>
    public required string FullName { get; init; }

    /// <summary>The user's password. Must be 8 to 100 characters.</summary>
    public required string Password { get; init; }

    /// <summary>The user's email address. Maximum 256 characters.</summary>
    public required string Email { get; init; }
}
