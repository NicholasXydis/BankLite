namespace BankLite.Application.DTOs
{
    public class UserProfileDto
    {
        /// <summary>The user's full name.</summary>
        public string FullName { get; set; } = string.Empty;
        /// <summary>The user's email address.</summary>
        public string Email { get; set; } = string.Empty;
        /// <summary>The date the account was created.</summary>
        public DateTime CreatedAt { get; set; }
        /// <summary>The date and time of the user's last login.</summary>
        public DateTime? LastLoginAt { get; set; }
    }
}