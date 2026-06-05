namespace BankLite.Application.DTOs;

public class TransferResponseDto
{
    public string Message { get; init; } = string.Empty;

    public decimal Amount { get; init; }
}
