namespace BankLite.Application.DTOs
{
    public class ResetPasswordDto
    {
        /// <summary>The password reset token received via email.</summary>
        public string Token { get; set; } = string.Empty;
        /// <summary>The new password. Minimum 8 characters.</summary>
        public string NewPassword { get; set; } = string.Empty;
    }
}