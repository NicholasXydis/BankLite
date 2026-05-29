namespace BankLite.Application.Options;

public sealed class FrontendSettings
{
    public const string SectionName = "Frontend";

    public string ResetPasswordUrl { get; init; } = string.Empty;
}
