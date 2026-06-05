namespace BankLite.Application.DTOs;

public class LoginUserDto
{
    /// <summary>The user's email address. Maximum 256 characters.</summary>
    public required string Email { get; init; }

    /// <summary>The user's password. Must be 8 to 100 characters.</summary>
    public required string Password { get; init; }
}
