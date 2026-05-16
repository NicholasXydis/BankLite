namespace BankLite.Application.DTOs
{
    public class TransactionResponseDto
    {
        /// <summary>The unique transaction identifier.</summary>
        public Guid Id { get; set; }
        /// <summary>The account this transaction belongs to.</summary>
        public Guid AccountId { get; set; }
        /// <summary>The transaction amount.</summary>
        public decimal Amount { get; set; }
        /// <summary>The transaction type: Deposit, Withdrawal, or Transfer.</summary>
        public string Type { get; set; } = string.Empty;
        /// <summary>A description of the transaction.</summary>
        public string Description { get; set; } = string.Empty;
        /// <summary>The date and time the transaction was created.</summary>
        public DateTime CreatedAt { get; set; }
    }
}
