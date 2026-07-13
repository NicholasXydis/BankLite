namespace BankLite.Application.Options;

public sealed class GroqSettings
{
    public const string SectionName = "Groq";

    public string ApiKey { get; init; } = string.Empty;

    public string Model { get; init; } = "openai/gpt-oss-120b";
}
