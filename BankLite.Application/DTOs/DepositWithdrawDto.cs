namespace BankLite.Application.DTOs
{
    public class DepositWithdrawDto
    {
        /// <summary>The unique identifier of the account to deposit into or withdraw from.</summary>
        public Guid AccountId { get; set; }
        /// <summary>The amount to deposit or withdraw. Must be greater than 0.</summary>
        public decimal Amount { get; set; }
    }
}
