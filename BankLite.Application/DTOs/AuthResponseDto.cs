namespace BankLite.Application.DTOs
{
    public class AuthResponseDto
    {
        public Guid UserId { get; set; }
        public required string FullName { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
