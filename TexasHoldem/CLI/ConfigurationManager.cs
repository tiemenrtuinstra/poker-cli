using System.Text.Json;

namespace TexasHoldem.CLI;

public class ConfigurationManager
{
    private const string DefaultConfigFile = "config.json";
    private AppConfig _config;
    private readonly string _configFilePath;

    public ConfigurationManager(string? configFilePath = null)
    {
        _configFilePath = configFilePath ?? DefaultConfigFile;
        _config = LoadConfiguration();
    }

    public AppConfig GetConfiguration() => _config;

    public GameConfig CreateGameConfigFromDefaults()
    {
        // Load .env file for API keys (secure storage)
        EnvLoader.Load();

        return new GameConfig
        {
            HumanPlayerCount = _config.Game.DefaultHumanPlayers,
            AiPlayerCount = _config.Game.DefaultAiPlayers,
            StartingChips = _config.Game.DefaultStartingChips,
            SmallBlind = _config.Game.DefaultSmallBlind,
            BigBlind = _config.Game.DefaultBigBlind,
            Ante = _config.Game.DefaultAnte,
            MaxHands = _config.Tournament.MaxHands,
            IsBlindIncreaseEnabled = _config.Tournament.EnableBlindIncrease,
            BlindIncreaseInterval = _config.Tournament.BlindIncreaseInterval,
            BlindIncreaseMultiplier = _config.Tournament.BlindIncreaseMultiplier,
            HumanPlayerNames = _config.PlayerNames.DefaultHumanNames?.ToList(),
            UseColors = _config.Game.UseColors,
            EnableAsciiArt = _config.Game.EnableAsciiArt,
            EnableLogging = _config.Game.EnableLogging,
            // AI Provider settings - environment variables take priority over config.json
            ClaudeApiKey = EnvLoader.GetEnv("CLAUDE_API_KEY") ?? _config.AI.ClaudeApiKey,
            GeminiApiKey = EnvLoader.GetEnv("GEMINI_API_KEY") ?? _config.AI.GeminiApiKey,
            OpenAiApiKey = EnvLoader.GetEnv("OPENAI_API_KEY") ?? _config.AI.OpenAiApiKey,
            EnabledProviders = ParseEnabledProviders(_config.AI.EnabledProviders),
            // Model names - env vars can override config
            ClaudeModel = EnvLoader.GetEnv("CLAUDE_MODEL") ?? _config.AI.ClaudeModel,
            GeminiModel = EnvLoader.GetEnv("GEMINI_MODEL") ?? _config.AI.GeminiModel,
            OpenAiModel = EnvLoader.GetEnv("OPENAI_MODEL") ?? _config.AI.OpenAiModel
        };
    }

    private List<Domain.Enums.AiProvider> ParseEnabledProviders(List<string> providerNames)
    {
        var providers = new List<Domain.Enums.AiProvider>();
        foreach (var name in providerNames)
        {
            if (Enum.TryParse<Domain.Enums.AiProvider>(name, true, out var provider))
            {
                providers.Add(provider);
            }
        }
        return providers.Any() ? providers : new List<Domain.Enums.AiProvider> { Domain.Enums.AiProvider.None };
    }

    private AppConfig LoadConfiguration()
    {
        try
        {
            if (File.Exists(_configFilePath))
            {
                var jsonString = File.ReadAllText(_configFilePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                };

                var config = JsonSerializer.Deserialize<AppConfig>(jsonString, options);
                if (config != null)
                {
                    Console.WriteLine($"✅ Configuration loaded from {_configFilePath}");
                    return config;
                }
            }

            Console.WriteLine($"⚠️  Configuration file not found. Creating default configuration at {_configFilePath}");
            return CreateDefaultConfiguration();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error loading configuration: {ex.Message}");
            Console.WriteLine("Using default configuration.");
            return CreateDefaultConfiguration();
        }
    }

    private AppConfig CreateDefaultConfiguration()
    {
        var defaultConfig = new AppConfig();
        SaveConfiguration(defaultConfig);
        return defaultConfig;
    }

    public void SaveConfiguration(AppConfig? config = null)
    {
        try
        {
            var configToSave = config ?? _config;
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var jsonString = JsonSerializer.Serialize(configToSave, options);
            File.WriteAllText(_configFilePath, jsonString);
            
            if (config != null)
            {
                _config = config;
            }
            
            Console.WriteLine($"✅ Configuration saved to {_configFilePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error saving configuration: {ex.Message}");
        }
    }

    public void UpdateGameDefaults(GameConfig gameConfig)
    {
        _config.Game.DefaultHumanPlayers = gameConfig.HumanPlayerCount;
        _config.Game.DefaultAiPlayers = gameConfig.AiPlayerCount;
        _config.Game.DefaultStartingChips = gameConfig.StartingChips;
        _config.Game.DefaultSmallBlind = gameConfig.SmallBlind;
        _config.Game.DefaultBigBlind = gameConfig.BigBlind;
        _config.Game.DefaultAnte = gameConfig.Ante;
        _config.Game.UseColors = gameConfig.UseColors;
        _config.Game.EnableAsciiArt = gameConfig.EnableAsciiArt;
        _config.Game.EnableLogging = gameConfig.EnableLogging;
        
        SaveConfiguration();
    }

    public void ResetToDefaults()
    {
        _config = new AppConfig();
        SaveConfiguration();
        Console.WriteLine("✅ Configuration reset to defaults");
    }

    public void ShowCurrentConfiguration()
    {
        Console.WriteLine("\n📋 CURRENT CONFIGURATION:");
        Console.WriteLine("=========================");
        
        Console.WriteLine($"🎮 Game Defaults:");
        Console.WriteLine($"   Human Players: {_config.Game.DefaultHumanPlayers}");
        Console.WriteLine($"   AI Players: {_config.Game.DefaultAiPlayers}");
        Console.WriteLine($"   Starting Chips: €{_config.Game.DefaultStartingChips:N0}");
        Console.WriteLine($"   Blinds: €{_config.Game.DefaultSmallBlind}/€{_config.Game.DefaultBigBlind}");
        Console.WriteLine($"   Ante: €{_config.Game.DefaultAnte}");
        Console.WriteLine($"   Colors: {_config.Game.UseColors}");
        Console.WriteLine($"   ASCII Art: {_config.Game.EnableAsciiArt}");
        Console.WriteLine($"   Logging: {_config.Game.EnableLogging}");
        
        Console.WriteLine($"\n🏆 Tournament Settings:");
        Console.WriteLine($"   Blind Increase: {_config.Tournament.EnableBlindIncrease}");
        Console.WriteLine($"   Increase Interval: {_config.Tournament.BlindIncreaseInterval} hands");
        Console.WriteLine($"   Increase Multiplier: {_config.Tournament.BlindIncreaseMultiplier:F1}x");
        Console.WriteLine($"   Max Hands: {(_config.Tournament.MaxHands == 0 ? "Unlimited" : _config.Tournament.MaxHands.ToString())}");
        
        Console.WriteLine($"\n🤖 AI Settings:");
        Console.WriteLine($"   Thinking Delay: {_config.AI.ThinkingDelayMin}-{_config.AI.ThinkingDelayMax}ms");
        Console.WriteLine($"   Poker Talk: {_config.AI.EnablePokerTalk} ({_config.AI.PokerTalkFrequency:P0} frequency)");
        Console.WriteLine($"   Enabled Providers: {string.Join(", ", _config.AI.EnabledProviders)}");

        // Check both .env and config.json for API keys
        EnvLoader.Load();
        var claudeKey = EnvLoader.GetEnv("CLAUDE_API_KEY") ?? _config.AI.ClaudeApiKey;
        var geminiKey = EnvLoader.GetEnv("GEMINI_API_KEY") ?? _config.AI.GeminiApiKey;
        var openaiKey = EnvLoader.GetEnv("OPENAI_API_KEY") ?? _config.AI.OpenAiApiKey;

        Console.WriteLine($"   Claude API: {(!string.IsNullOrEmpty(claudeKey) ? $"Configured ({_config.AI.ClaudeModel})" : "Not configured")}");
        Console.WriteLine($"   Gemini API: {(!string.IsNullOrEmpty(geminiKey) ? $"Configured ({_config.AI.GeminiModel})" : "Not configured")}");
        Console.WriteLine($"   OpenAI API: {(!string.IsNullOrEmpty(openaiKey) ? $"Configured ({_config.AI.OpenAiModel})" : "Not configured")}");
        Console.WriteLine($"   (API keys loaded from .env file or config.json)");
        
        Console.WriteLine($"\n📁 Logging:");
        Console.WriteLine($"   Directory: {_config.Logging.LogDirectory}");
        Console.WriteLine($"   Hand History: {_config.Logging.EnableHandHistory}");
        Console.WriteLine($"   Action Logging: {_config.Logging.EnableActionLogging}");
        Console.WriteLine($"   Log Level: {_config.Logging.LogLevel}");
    }
}

// Configuration classes
public class AppConfig
{
    public GameDefaults Game { get; set; } = new();
    public TournamentSettings Tournament { get; set; } = new();
    public AISettings AI { get; set; } = new();
    public DisplaySettings Display { get; set; } = new();
    public LoggingSettings Logging { get; set; } = new();
    public PlayerNameSettings PlayerNames { get; set; } = new();
}

public class GameDefaults
{
    public int DefaultHumanPlayers { get; set; } = 1;
    public int DefaultAiPlayers { get; set; } = 5;
    public int DefaultStartingChips { get; set; } = 10000;
    public int DefaultSmallBlind { get; set; } = 50;
    public int DefaultBigBlind { get; set; } = 100;
    public int DefaultAnte { get; set; } = 0;
    public bool EnableLogging { get; set; } = true;
    public bool UseColors { get; set; } = true;
    public bool EnableAsciiArt { get; set; } = true;
}

public class TournamentSettings
{
    public bool EnableBlindIncrease { get; set; } = false;
    public int BlindIncreaseInterval { get; set; } = 10;
    public double BlindIncreaseMultiplier { get; set; } = 1.5;
    public int MaxHands { get; set; } = 0;
}

public class AISettings
{
    public int ThinkingDelayMin { get; set; } = 500;
    public int ThinkingDelayMax { get; set; } = 2000;
    public bool EnablePokerTalk { get; set; } = true;
    public double PokerTalkFrequency { get; set; } = 0.2;

    // API Keys for different providers - configure in config.json
    public string ClaudeApiKey { get; set; } = string.Empty;
    public string GeminiApiKey { get; set; } = string.Empty;
    public string OpenAiApiKey { get; set; } = string.Empty;

    // Which AI providers are enabled (players will be distributed among enabled providers)
    public List<string> EnabledProviders { get; set; } = new() { "None" };

    // Model names for each provider (optional, uses defaults if empty)
    public string ClaudeModel { get; set; } = "claude-sonnet-4-20250514";
    public string GeminiModel { get; set; } = "gemini-2.0-flash";
    public string OpenAiModel { get; set; } = "gpt-4o-mini";
}

public class DisplaySettings
{
    public double AnimationSpeed { get; set; } = 1.0;
    public bool ShowHandRankings { get; set; } = true;
    public bool ClearScreenBetweenHands { get; set; } = false;
}

public class LoggingSettings
{
    public string LogDirectory { get; set; } = "logs";
    public bool EnableHandHistory { get; set; } = true;
    public bool EnableActionLogging { get; set; } = true;
    public string LogLevel { get; set; } = "Info";
}

public class PlayerNameSettings
{
    public List<string> DefaultHumanNames { get; set; } = new() { "Player 1", "Player 2", "Player 3" };
    public bool UseFunnyAiNames { get; set; } = true;
    public List<string> CustomAiNames { get; set; } = new();
}