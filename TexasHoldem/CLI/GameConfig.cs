using TexasHoldem.Domain.Enums;

namespace TexasHoldem.CLI;

public class GameConfig
{
    public int HumanPlayerCount { get; set; } = 1;
    public int AiPlayerCount { get; set; } = 5;
    public int StartingChips { get; set; } = 10000;
    public int SmallBlind { get; set; } = 50;
    public int BigBlind { get; set; } = 100;
    public int Ante { get; set; } = 0;
    public int MaxHands { get; set; } = 0; // 0 = unlimited
    public bool IsBlindIncreaseEnabled { get; set; } = false;
    public int BlindIncreaseInterval { get; set; } = 10; // Every X hands
    public double BlindIncreaseMultiplier { get; set; } = 1.5;
    public List<string>? HumanPlayerNames { get; set; }
    public bool UseColors { get; set; } = true;
    public bool EnableAsciiArt { get; set; } = true;
    public bool EnableLogging { get; set; } = true;

    // AI Provider settings
    public string? ClaudeApiKey { get; set; }
    public string? GeminiApiKey { get; set; }
    public string? OpenAiApiKey { get; set; }
    public List<AiProvider> EnabledProviders { get; set; } = new() { AiProvider.None };
    public string ClaudeModel { get; set; } = "claude-sonnet-4-20250514";
    public string GeminiModel { get; set; } = "gemini-2.0-flash";
    public string OpenAiModel { get; set; } = "gpt-4o-mini";

    public int TotalPlayers => HumanPlayerCount + AiPlayerCount;

    public bool IsValid()
    {
        return TotalPlayers >= 2 && TotalPlayers <= 8 &&
               HumanPlayerCount >= 0 && HumanPlayerCount <= TotalPlayers &&
               StartingChips > 0 &&
               SmallBlind > 0 && BigBlind > SmallBlind &&
               Ante >= 0;
    }

    public GameConfig Clone()
    {
        return new GameConfig
        {
            HumanPlayerCount = HumanPlayerCount,
            AiPlayerCount = AiPlayerCount,
            StartingChips = StartingChips,
            SmallBlind = SmallBlind,
            BigBlind = BigBlind,
            Ante = Ante,
            MaxHands = MaxHands,
            IsBlindIncreaseEnabled = IsBlindIncreaseEnabled,
            BlindIncreaseInterval = BlindIncreaseInterval,
            BlindIncreaseMultiplier = BlindIncreaseMultiplier,
            HumanPlayerNames = HumanPlayerNames?.ToList(),
            UseColors = UseColors,
            EnableAsciiArt = EnableAsciiArt,
            EnableLogging = EnableLogging,
            ClaudeApiKey = ClaudeApiKey,
            GeminiApiKey = GeminiApiKey,
            OpenAiApiKey = OpenAiApiKey,
            EnabledProviders = EnabledProviders.ToList(),
            ClaudeModel = ClaudeModel,
            GeminiModel = GeminiModel,
            OpenAiModel = OpenAiModel
        };
    }

    /// <summary>
    /// Get the list of providers that have valid API keys configured
    /// </summary>
    public List<AiProvider> GetConfiguredProviders()
    {
        var configured = new List<AiProvider>();

        if (!string.IsNullOrEmpty(ClaudeApiKey))
            configured.Add(AiProvider.Claude);
        if (!string.IsNullOrEmpty(GeminiApiKey))
            configured.Add(AiProvider.Gemini);
        if (!string.IsNullOrEmpty(OpenAiApiKey))
            configured.Add(AiProvider.OpenAI);

        return configured;
    }

    /// <summary>
    /// Get the providers that are both enabled and have valid API keys
    /// </summary>
    public List<AiProvider> GetActiveProviders()
    {
        var configured = GetConfiguredProviders();
        return EnabledProviders
            .Where(p => p == AiProvider.None || configured.Contains(p))
            .ToList();
    }
}