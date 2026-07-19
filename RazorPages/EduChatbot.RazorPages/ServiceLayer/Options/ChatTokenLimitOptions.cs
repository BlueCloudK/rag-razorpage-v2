using Microsoft.Extensions.Configuration;

namespace ServiceLayer.Options;

public sealed class ChatTokenLimitOptions
{
    public int MaxPromptTokens { get; init; } = 700;
    public int MaxRetrievedContextTokens { get; init; } = 1800;
    public int MaxAnswerTokens { get; init; } = 500;
    public int MaxTotalTokensPerQuestion { get; init; } = 3000;

    public static ChatTokenLimitOptions FromConfiguration(IConfiguration configuration)
    {
        var configuredPrompt = ReadPositiveInt(configuration["ChatTokenLimits:MaxPromptTokens"], 700);
        var configuredContext = ReadPositiveInt(configuration["ChatTokenLimits:MaxRetrievedContextTokens"], 1800);
        var configuredAnswer = ReadPositiveInt(configuration["ChatTokenLimits:MaxAnswerTokens"], 500);
        var configuredTotal = ReadPositiveInt(configuration["ChatTokenLimits:MaxTotalTokensPerQuestion"], 3000);

        var boundedTotal = Math.Max(configuredTotal, configuredPrompt + configuredContext + configuredAnswer);
        return new ChatTokenLimitOptions
        {
            MaxPromptTokens = configuredPrompt,
            MaxRetrievedContextTokens = configuredContext,
            MaxAnswerTokens = configuredAnswer,
            MaxTotalTokensPerQuestion = boundedTotal
        };
    }

    private static int ReadPositiveInt(string? value, int fallback) =>
        int.TryParse(value, out var parsed) && parsed > 0 ? parsed : fallback;
}
