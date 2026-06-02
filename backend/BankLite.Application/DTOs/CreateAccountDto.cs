using BankLite.Domain.Entities;

namespace BankLite.Application.DTOs;

public class CreateAccountDto
{
    /// <summary>The account type. 0 = Chequing, 1 = Savings.</summary>
    public AccountType Type { get; init; }
}