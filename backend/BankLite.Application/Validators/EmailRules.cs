namespace BankLite.Application.Validators;

public static class EmailRules
{
    public const string Pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";

    public const int MaxLength = 256;
}
