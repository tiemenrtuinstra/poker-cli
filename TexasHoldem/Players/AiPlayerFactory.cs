using TexasHoldem.CLI;
using TexasHoldem.Game.Enums;

namespace TexasHoldem.Players;

/// <summary>
/// Factory for creating AI players based on configuration.
/// Supports multiple AI providers (Claude, Gemini, OpenAI) and distributes
/// players among configured providers.
/// </summary>
public class AiPlayerFactory
{
    private readonly GameConfig _config;
    private readonly Random _random;
    private int _providerIndex;

    public AiPlayerFactory(GameConfig config, Random? random = null)
    {
        _config = config;
        _random = random ?? new Random();
        _providerIndex = 0;
    }

    /// <summary>
    /// Create an AI player using the next available provider in rotation
    /// </summary>
    public IPlayer CreateAiPlayer(int startingChips)
    {
        var activeProviders = GetActiveProviders();

        if (!activeProviders.Any() || activeProviders.All(p => p == AiProvider.None))
        {
            // No LLM providers configured, use basic AI
            return BasicAiPlayer.CreateRandomAiPlayer(startingChips, _random);
        }

        // Get the next provider in rotation (round-robin distribution)
        var provider = activeProviders[_providerIndex % activeProviders.Count];
        _providerIndex++;

        return CreatePlayerForProvider(provider, startingChips);
    }

    /// <summary>
    /// Create multiple AI players, distributing them among enabled providers
    /// </summary>
    public List<IPlayer> CreateAiPlayers(int count, int startingChips)
    {
        var players = new List<IPlayer>();
        var activeProviders = GetActiveProviders();

        // Show which providers are being used
        if (activeProviders.Any(p => p != AiProvider.None))
        {
            var llmProviders = activeProviders.Where(p => p != AiProvider.None).ToList();
            Console.WriteLine($"  Using AI providers: {string.Join(", ", llmProviders)}");
        }

        for (int i = 0; i < count; i++)
        {
            players.Add(CreateAiPlayer(startingChips));
        }

        return players;
    }

    private List<AiProvider> GetActiveProviders()
    {
        var active = new List<AiProvider>();
        var warnings = new List<string>();

        foreach (var provider in _config.EnabledProviders)
        {
            switch (provider)
            {
                case AiProvider.Claude:
                    if (!string.IsNullOrEmpty(_config.ClaudeApiKey))
                        active.Add(AiProvider.Claude);
                    else
                        warnings.Add("Claude (no API key in .env)");
                    break;
                case AiProvider.Gemini:
                    if (!string.IsNullOrEmpty(_config.GeminiApiKey))
                        active.Add(AiProvider.Gemini);
                    else
                        warnings.Add("Gemini (no API key in .env)");
                    break;
                case AiProvider.OpenAI:
                    if (!string.IsNullOrEmpty(_config.OpenAiApiKey))
                        active.Add(AiProvider.OpenAI);
                    else
                        warnings.Add("OpenAI (no API key in .env)");
                    break;
                case AiProvider.None:
                    active.Add(AiProvider.None);
                    break;
            }
        }

        // Show warnings for providers without keys
        if (warnings.Any())
        {
            Console.WriteLine($"  Skipping providers without API keys: {string.Join(", ", warnings)}");
        }

        // If no LLM providers are active, default to basic AI
        if (!active.Any())
        {
            Console.WriteLine("  No LLM providers available - using basic AI");
            active.Add(AiProvider.None);
        }

        return active;
    }

    private IPlayer CreatePlayerForProvider(AiProvider provider, int startingChips)
    {
        return provider switch
        {
            AiProvider.Claude => ClaudeAiPlayer.CreateClaudeAiPlayer(
                startingChips,
                _config.ClaudeApiKey,
                _config.ClaudeModel,
                _random),

            AiProvider.Gemini => GeminiAiPlayer.CreateGeminiAiPlayer(
                startingChips,
                _config.GeminiApiKey,
                _config.GeminiModel,
                _random),

            AiProvider.OpenAI => OpenAiPlayer.CreateOpenAiPlayer(
                startingChips,
                _config.OpenAiApiKey,
                _config.OpenAiModel,
                _random),

            _ => BasicAiPlayer.CreateRandomAiPlayer(startingChips, _random)
        };
    }

    /// <summary>
    /// Get a summary of the AI configuration
    /// </summary>
    public string GetConfigurationSummary()
    {
        var lines = new List<string> { "AI Configuration:" };

        var activeProviders = GetActiveProviders();
        if (activeProviders.All(p => p == AiProvider.None))
        {
            lines.Add("  - Basic AI only (no LLM providers configured)");
        }
        else
        {
            foreach (var provider in activeProviders.Where(p => p != AiProvider.None).Distinct())
            {
                var model = provider switch
                {
                    AiProvider.Claude => _config.ClaudeModel,
                    AiProvider.Gemini => _config.GeminiModel,
                    AiProvider.OpenAI => _config.OpenAiModel,
                    _ => "N/A"
                };
                lines.Add($"  - {provider}: {model}");
            }
        }

        return string.Join("\n", lines);
    }
}
