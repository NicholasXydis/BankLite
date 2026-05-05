namespace BankLite.Application.DTOs
{
    public class ExternalTransferDto
    {
        public Guid FromAccountId { get; set; }
        public string ToAccountNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}