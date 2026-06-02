namespace BankLite.Application.Options;

public sealed class AllowedOriginsSettings
{
    public const string SectionName = "AllowedOrigins";

    public string Frontend { get; init; } = string.Empty;
}