namespace Infrastructure.Anthropic;

public sealed class AnthropicOptions
{
    public const string SectionName = "Anthropic";

    // Never committed: set locally via `dotnet user-secrets set "Anthropic:ApiKey" ...` and in Azure via the Anthropic__ApiKey app setting.
    public string ApiKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = "https://api.anthropic.com";

    // Cheapest/fastest current model ($1/$5 per MTok vs Opus 4.8's $5/$25); override per
    // environment via the Anthropic:Model setting if a turn ever needs more capability.
    public string Model { get; set; } = "claude-haiku-4-5";

    // Hard cap on output tokens per API call (each agent-loop iteration), the main cost guardrail besides the loop cap in ChatService.
    public int MaxTokens { get; set; } = 2048;
}
