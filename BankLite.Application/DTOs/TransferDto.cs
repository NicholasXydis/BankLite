namespace BankLite.Application.DTOs
{
    public class TransferDto
    {
        /// <summary>The unique identifier of the account to transfer from.</summary>
        public Guid FromAccountId { get; set; }
        /// <summary>The unique identifier of the account to transfer to.</summary>
        public Guid ToAccountId { get; set; }
        /// <summary>The amount to transfer. Must be greater than 0.</summary>
        public decimal Amount { get; set; }
    }
}
