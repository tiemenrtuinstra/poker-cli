using TexasHoldem.Data.Entities;
using TexasHoldem.Data.Services;
using TexasHoldem.Game.Enums;

namespace TexasHoldem.CLI;

public class ConfigurationManager
{
    private readonly ISettingsService _settingsService;
    private ProgramSettingsEntity? _programSettings;
    private GameDefaultsEntity? _gameDefaults;

    public ConfigurationManager(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    /// <summary>
    /// Initialize settings - must be called at startup
    /// </summary>
    public async Task InitializeAsync()
    {
        await _settingsService.EnsureSettingsExistAsync();
        _programSettings = await _settingsService.GetProgramSettingsAsync();
        _gameDefaults = await _settingsService.GetGameDefaultsAsync();
    }

    public ProgramSettingsEntity GetProgramSettings()
    {
        return _programSettings ?? new ProgramSettingsEntity();
    }

    public GameDefaultsEntity GetGameDefaults()
    {
        return _gameDefaults ?? new GameDefaultsEntity();
    }

    public GameConfig CreateGameConfigFromDefaults()
    {
        // Load .env file for API keys (secure storage)
        EnvLoader.Load();

        var programSettings = GetProgramSettings();
        var gameDefaults = GetGameDefaults();

        // Parse enabled providers, auto-enable if API key is present
        var enabledProviders = ParseEnabledProviders(programSettings.EnabledProviders);

        // Auto-enable providers if API keys are configured
        var claudeKey = EnvLoader.GetEnv("CLAUDE_API_KEY");
        var geminiKey = EnvLoader.GetEnv("GEMINI_API_KEY");
        var openaiKey = EnvLoader.GetEnv("OPENAI_API_KEY");

        if (enabledProviders.Contains(AiProvider.None) || !enabledProviders.Any())
        {
            // Auto-detect providers based on available API keys
            enabledProviders = new List<AiProvider>();
            if (!string.IsNullOrEmpty(claudeKey)) enabledProviders.Add(AiProvider.Claude);
            if (!string.IsNullOrEmpty(geminiKey)) enabledProviders.Add(AiProvider.Gemini);
            if (!string.IsNullOrEmpty(openaiKey)) enabledProviders.Add(AiProvider.OpenAI);

            // Fall back to None (BasicAI) if no API keys
            if (!enabledProviders.Any()) enabledProviders.Add(AiProvider.None);
        }

        return new GameConfig
        {
            // Game defaults
            HumanPlayerCount = gameDefaults.DefaultHumanPlayers,
            AiPlayerCount = gameDefaults.DefaultAiPlayers,
            StartingChips = gameDefaults.DefaultStartingChips,
            SmallBlind = gameDefaults.DefaultSmallBlind,
            BigBlind = gameDefaults.DefaultBigBlind,
            Ante = gameDefaults.DefaultAnte,
            MaxHands = gameDefaults.MaxHands,
            IsBlindIncreaseEnabled = gameDefaults.EnableBlindIncrease,
            BlindIncreaseInterval = gameDefaults.BlindIncreaseInterval,
            BlindIncreaseMultiplier = gameDefaults.BlindIncreaseMultiplier,
            AllowRebuys = gameDefaults.AllowRebuys,
            RebuyAmount = gameDefaults.RebuyAmount,

            // Program settings mapped to GameConfig
            HumanPlayerNames = programSettings.DefaultHumanNames?.Split(',').Select(n => n.Trim()).ToList(),
            UseColors = programSettings.UseColors,
            EnableAsciiArt = programSettings.EnableAsciiArt,
            UseUnicodeSymbols = programSettings.UseUnicodeSymbols,
            EnableLogging = true, // Always enabled

            // AI Provider settings - API keys from .env
            ClaudeApiKey = claudeKey,
            GeminiApiKey = geminiKey,
            OpenAiApiKey = openaiKey,
            EnabledProviders = enabledProviders,

            // Model names from settings
            ClaudeModel = programSettings.ClaudeModel,
            GeminiModel = programSettings.GeminiModel,
            OpenAiModel = programSettings.OpenAiModel
        };
    }

    private List<AiProvider> ParseEnabledProviders(string? providerString)
    {
        if (string.IsNullOrWhiteSpace(providerString))
            return new List<AiProvider> { AiProvider.None };

        var providers = new List<AiProvider>();
        foreach (var name in providerString.Split(',').Select(s => s.Trim()))
        {
            if (Enum.TryParse<AiProvider>(name, true, out var provider))
            {
                providers.Add(provider);
            }
        }
        return providers.Any() ? providers : new List<AiProvider> { AiProvider.None };
    }

    public async Task UpdateGameDefaultsAsync(GameConfig gameConfig)
    {
        var defaults = await _settingsService.GetGameDefaultsAsync();

        defaults.DefaultHumanPlayers = gameConfig.HumanPlayerCount;
        defaults.DefaultAiPlayers = gameConfig.AiPlayerCount;
        defaults.DefaultStartingChips = gameConfig.StartingChips;
        defaults.DefaultSmallBlind = gameConfig.SmallBlind;
        defaults.DefaultBigBlind = gameConfig.BigBlind;
        defaults.DefaultAnte = gameConfig.Ante;
        defaults.MaxHands = gameConfig.MaxHands;
        defaults.EnableBlindIncrease = gameConfig.IsBlindIncreaseEnabled;
        defaults.BlindIncreaseInterval = gameConfig.BlindIncreaseInterval;
        defaults.BlindIncreaseMultiplier = gameConfig.BlindIncreaseMultiplier;
        defaults.AllowRebuys = gameConfig.AllowRebuys;
        defaults.RebuyAmount = gameConfig.RebuyAmount;

        await _settingsService.SaveGameDefaultsAsync(defaults);
        _gameDefaults = defaults;

        Console.WriteLine("[green]Game defaults saved to database.[/]");
    }

    public async Task UpdateProgramSettingsAsync(ProgramSettingsEntity settings)
    {
        await _settingsService.SaveProgramSettingsAsync(settings);
        _programSettings = settings;
        Console.WriteLine("[green]Program settings saved to database.[/]");
    }

    public async Task ResetToDefaultsAsync()
    {
        await _settingsService.ResetToDefaultsAsync();
        _programSettings = await _settingsService.GetProgramSettingsAsync();
        _gameDefaults = await _settingsService.GetGameDefaultsAsync();
        Console.WriteLine("[green]All settings reset to defaults.[/]");
    }

    public void ShowCurrentConfiguration()
    {
        var programSettings = GetProgramSettings();
        var gameDefaults = GetGameDefaults();

        Console.WriteLine("\n[bold cyan]CURRENT CONFIGURATION[/]");
        Console.WriteLine("=".PadRight(50, '='));

        Console.WriteLine("\n[bold yellow]Game Defaults:[/]");
        Console.WriteLine($"   Human Players: {gameDefaults.DefaultHumanPlayers}");
        Console.WriteLine($"   AI Players: {gameDefaults.DefaultAiPlayers}");
        Console.WriteLine($"   Starting Chips: {gameDefaults.DefaultStartingChips:N0}");
        Console.WriteLine($"   Blinds: {gameDefaults.DefaultSmallBlind}/{gameDefaults.DefaultBigBlind}");
        Console.WriteLine($"   Ante: {gameDefaults.DefaultAnte}");
        Console.WriteLine($"   Blind Increase: {(gameDefaults.EnableBlindIncrease ? "Enabled" : "Disabled")}");
        if (gameDefaults.EnableBlindIncrease)
        {
            Console.WriteLine($"     - Interval: Every {gameDefaults.BlindIncreaseInterval} hands");
            Console.WriteLine($"     - Multiplier: {gameDefaults.BlindIncreaseMultiplier:F1}x");
        }
        Console.WriteLine($"   Max Hands: {(gameDefaults.MaxHands == 0 ? "Unlimited" : gameDefaults.MaxHands.ToString())}");
        Console.WriteLine($"   Allow Rebuys: {(gameDefaults.AllowRebuys ? "Yes" : "No")}");

        Console.WriteLine("\n[bold yellow]Program Settings:[/]");
        Console.WriteLine($"   Colors: {(programSettings.UseColors ? "Enabled" : "Disabled")}");
        Console.WriteLine($"   ASCII Art: {(programSettings.EnableAsciiArt ? "Enabled" : "Disabled")}");
        Console.WriteLine($"   Unicode Symbols: {(programSettings.UseUnicodeSymbols ? "Enabled" : "Disabled")}");
        Console.WriteLine($"   Clear Screen Between Hands: {(programSettings.ClearScreenBetweenHands ? "Yes" : "No")}");
        Console.WriteLine($"   Show Hand Rankings: {(programSettings.ShowHandRankings ? "Yes" : "No")}");
        Console.WriteLine($"   Animation Speed: {programSettings.AnimationSpeed:F1}x");
        Console.WriteLine($"   Check for Updates: {(programSettings.CheckForUpdatesOnStartup ? "Yes" : "No")}");

        Console.WriteLine("\n[bold yellow]AI Settings:[/]");
        Console.WriteLine($"   Thinking Delay: {programSettings.ThinkingDelayMin}-{programSettings.ThinkingDelayMax}ms");
        Console.WriteLine($"   Poker Talk: {(programSettings.EnablePokerTalk ? $"Enabled ({programSettings.PokerTalkFrequency:P0})" : "Disabled")}");
        Console.WriteLine($"   Funny AI Names: {(programSettings.UseFunnyAiNames ? "Yes" : "No")}");
        Console.WriteLine($"   Enabled Providers: {programSettings.EnabledProviders}");

        // Check for API keys
        EnvLoader.Load();
        var claudeKey = EnvLoader.GetEnv("CLAUDE_API_KEY");
        var geminiKey = EnvLoader.GetEnv("GEMINI_API_KEY");
        var openaiKey = EnvLoader.GetEnv("OPENAI_API_KEY");

        Console.WriteLine($"   Claude: {(!string.IsNullOrEmpty(claudeKey) ? $"Configured ({programSettings.ClaudeModel})" : "Not configured")}");
        Console.WriteLine($"   Gemini: {(!string.IsNullOrEmpty(geminiKey) ? $"Configured ({programSettings.GeminiModel})" : "Not configured")}");
        Console.WriteLine($"   OpenAI: {(!string.IsNullOrEmpty(openaiKey) ? $"Configured ({programSettings.OpenAiModel})" : "Not configured")}");
        Console.WriteLine($"   [dim](API keys loaded from .env file)[/]");

        Console.WriteLine($"\n[bold yellow]Logging:[/]");
        Console.WriteLine($"   Log Level: {programSettings.LogLevel}");
        Console.WriteLine($"   [dim](Game history stored in SQLite database)[/]");
    }
}
