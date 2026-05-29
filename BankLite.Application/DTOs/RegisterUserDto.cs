namespace BankLite.Application.DTOs;

public class RegisterUserDto
{
    /// <summary>The user's full name.</summary>
    public required string FullName { get; init; }

    /// <summary>The user's password. Minimum 8 characters.</summary>
    public required string Password { get; init; }

    /// <summary>The user's email address.</summary>
    public required string Email { get; init; }
}