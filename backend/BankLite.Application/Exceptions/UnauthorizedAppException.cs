namespace BankLite.Application.Exceptions;

public class UnauthorizedAppException : UnauthorizedAccessException
{
    public UnauthorizedAppException(string message)
        : base(message)
    {
    }
}