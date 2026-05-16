namespace BankLite.Application.DTOs
{
    public class ExternalTransferDto
    {
        /// <summary>The unique identifier of the account to transfer from.</summary>
        public Guid FromAccountId { get; set; }
        /// <summary>The 12-character account number to transfer to.</summary>
        public string ToAccountNumber { get; set; } = string.Empty;
        /// <summary>The amount to transfer. Must be greater than 0.</summary>
        public decimal Amount { get; set; }
    }
}