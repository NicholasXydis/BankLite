namespace BankLite.Application.DTOs;

public class LoginUserDto
{
    /// <summary>The user's email address.</summary>
    public required string Email { get; init; }

    /// <summary>The user's password.</summary>
    public required string Password { get; init; }
}