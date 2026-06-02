namespace BankLite.Application.Options;

public sealed class SendGridSettings
{
    public const string SectionName = "SendGrid";

    public string ApiKey { get; init; } = string.Empty;
    public string FromEmail { get; init; } = string.Empty;
    public string FromName { get; init; } = "BankLite";
}