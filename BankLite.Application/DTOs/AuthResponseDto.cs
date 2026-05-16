namespace BankLite.Application.DTOs
{
    public class AuthResponseDto
    {
        /// <summary>The authenticated user's unique identifier.</summary>
        public Guid UserId { get; set; }
        /// <summary>The user's full name.</summary>
        public required string FullName { get; set; }
        /// <summary>The JWT access token expiry time.</summary>
        public DateTime ExpiresAt { get; set; }
    }
}
