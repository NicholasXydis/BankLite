namespace BankLite.Application.DTOs
{
    public class ForgotPasswordDto
    {
        /// <summary>The email address associated with the account.</summary>
        public required string Email { get; set; }
    }
}