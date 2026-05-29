namespace BankLite.Application.DTOs;

public class ForgotPasswordDto
{
    /// <summary>The email address associated with the account.</summary>
    public required string Email { get; init; }

    /// <summary>Preferred language for the email. Accepts "en" or "fr". Defaults to "en".</summary>
    public string Lang { get; init; } = "en";
}