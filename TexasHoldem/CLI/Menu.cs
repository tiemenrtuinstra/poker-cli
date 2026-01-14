using TexasHoldem.Domain;
using TexasHoldem.Domain.Enums;

namespace TexasHoldem.CLI;

public class Menu
{
    private readonly InputHelper _inputHelper;
    private readonly ConfigurationManager _configManager;

    public Menu()
    {
        _inputHelper = new InputHelper();
        _configManager = new ConfigurationManager();
    }

    public GameConfig? SetupGame()
    {
        ShowWelcomeMessage();

        var mainChoice = _inputHelper.GetChoiceInput("What would you like to do?", new Dictionary<string, string>
        {
            {"Start New Game", "new"},
            {"Load Preset Configuration", "preset"},
            {"Load From Configuration", "config"},
            {"Manage Settings", "settings"},
            {"View Rules", "rules"},
            {"Exit", "exit"}
        }, "new");

        switch (mainChoice)
        {
            case "new":
                return ConfigureNewGame();
            case "preset":
                return LoadPresetConfiguration();
            case "config":
                return LoadFromConfiguration();
            case "settings":
                ManageSettings();
                return SetupGame();
            case "rules":
                ShowRules();
                return SetupGame();
            case "exit":
                return null;
            default:
                return null;
        }
    }

    private void ShowWelcomeMessage()
    {
        _inputHelper.ClearScreen();
        Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                                                          ║");
        Console.WriteLine("║            🃏 TEXAS HOLD'EM POKER CLI 🃏               ║");
        Console.WriteLine("║                                                          ║");
        Console.WriteLine("║          Welcome to the ultimate poker experience!       ║");
        Console.WriteLine("║                                                          ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
        Console.WriteLine();
    }

    private GameConfig ConfigureNewGame()
    {
        _inputHelper.ClearScreen();
        Console.WriteLine("🎯 GAME SETUP");
        Console.WriteLine("=============");
        Console.WriteLine();

        var config = new GameConfig();

        // Player setup
        Console.WriteLine("👥 PLAYER CONFIGURATION:");
        config.HumanPlayerCount = _inputHelper.GetIntegerInput("Number of human players", 0, 8, 1);
        
        var maxAiPlayers = 8 - config.HumanPlayerCount;
        if (maxAiPlayers > 0)
        {
            var defaultAi = Math.Min(5, maxAiPlayers);
            config.AiPlayerCount = _inputHelper.GetIntegerInput($"Number of AI players", 0, maxAiPlayers, defaultAi);
        }
        else
        {
            config.AiPlayerCount = 0;
        }

        if (config.TotalPlayers < 2)
        {
            _inputHelper.ShowError("Need at least 2 total players!");
            return ConfigureNewGame();
        }

        // Get human player names
        if (config.HumanPlayerCount > 0)
        {
            Console.WriteLine("\n📝 PLAYER NAMES:");
            config.HumanPlayerNames = _inputHelper.GetPlayerNames(config.HumanPlayerCount);
        }

        // Chip and blind configuration
        Console.WriteLine("\n💰 CHIP & BLIND CONFIGURATION:");
        config.StartingChips = _inputHelper.GetIntegerInput("Starting chips per player", 1000, 1000000, 10000);
        config.SmallBlind = _inputHelper.GetIntegerInput("Small blind amount", 1, config.StartingChips / 10, 50);
        config.BigBlind = _inputHelper.GetIntegerInput("Big blind amount", config.SmallBlind + 1, config.StartingChips / 5, config.SmallBlind * 2);
        config.Ante = _inputHelper.GetIntegerInput("Ante amount (0 for no ante)", 0, config.SmallBlind, 0);

        // Tournament settings
        Console.WriteLine("\n🏆 TOURNAMENT SETTINGS:");
        config.MaxHands = _inputHelper.GetIntegerInput("Maximum hands (0 for unlimited)", 0, 1000, 0);
        config.IsBlindIncreaseEnabled = _inputHelper.GetBooleanInput("Enable blind increases?", false);
        
        if (config.IsBlindIncreaseEnabled)
        {
            config.BlindIncreaseInterval = _inputHelper.GetIntegerInput("Increase blinds every X hands", 1, 100, 10);
            config.BlindIncreaseMultiplier = _inputHelper.GetDoubleInput("Blind increase multiplier", 1.1, 3.0, 1.5);
        }

        // Display settings
        Console.WriteLine("\n🎨 DISPLAY SETTINGS:");
        config.UseColors = _inputHelper.GetBooleanInput("Use colors for cards and text?", true);
        config.EnableAsciiArt = _inputHelper.GetBooleanInput("Enable ASCII art table?", true);
        config.EnableLogging = _inputHelper.GetBooleanInput("Enable game logging?", true);

        // AI settings are loaded from .env and config.json
        var defaultConfig = _configManager.CreateGameConfigFromDefaults();
        config.ClaudeApiKey = defaultConfig.ClaudeApiKey;
        config.GeminiApiKey = defaultConfig.GeminiApiKey;
        config.OpenAiApiKey = defaultConfig.OpenAiApiKey;
        config.EnabledProviders = defaultConfig.EnabledProviders;
        config.ClaudeModel = defaultConfig.ClaudeModel;
        config.GeminiModel = defaultConfig.GeminiModel;
        config.OpenAiModel = defaultConfig.OpenAiModel;

        // Show AI configuration info
        var configuredProviders = config.GetConfiguredProviders();
        Console.WriteLine("\n🤖 AI CONFIGURATION:");
        if (configuredProviders.Any())
        {
            Console.WriteLine($"   API keys found for: {string.Join(", ", configuredProviders)}");

            // Show which of the enabled providers actually have keys
            var activeProviders = config.EnabledProviders
                .Where(p => p == Domain.Enums.AiProvider.None || configuredProviders.Contains(p))
                .ToList();

            if (activeProviders.Any(p => p != Domain.Enums.AiProvider.None))
            {
                Console.WriteLine($"   Will use: {string.Join(", ", activeProviders.Where(p => p != Domain.Enums.AiProvider.None))}");
            }
            else
            {
                Console.WriteLine("   Using: Basic AI (enabled providers have no API keys)");
            }
        }
        else
        {
            Console.WriteLine("   No API keys found - using basic AI");
            Console.WriteLine("   (Add keys to .env file to enable LLM players)");
        }

        // Confirm configuration
        ShowConfigurationSummary(config);
        
        var confirmed = _inputHelper.GetBooleanInput("Start game with this configuration?", true);
        if (!confirmed)
        {
            return ConfigureNewGame();
        }

        return config;
    }

    private GameConfig? LoadPresetConfiguration()
    {
        _inputHelper.ClearScreen();
        Console.WriteLine("⚙️  PRESET CONFIGURATIONS");
        Console.WriteLine("========================");
        Console.WriteLine();

        var presets = new Dictionary<string, GameConfig>
        {
            {"Quick Game (1v1)", CreateQuickGameConfig()},
            {"Classic Tournament (6 players)", CreateClassicTournamentConfig()},
            {"High Stakes (4 players)", CreateHighStakesConfig()},
            {"Beginner Friendly", CreateBeginnerFriendlyConfig()},
            {"AI Showcase (8 AI players)", CreateAiShowcaseConfig()},
            {"Back to Main Menu", null!}
        };

        var choice = _inputHelper.GetChoiceInput("Select a preset configuration:", 
            presets.ToDictionary(p => p.Key, p => p.Key), "Quick Game (1v1)");

        if (choice == "Back to Main Menu")
        {
            return SetupGame();
        }

        var config = presets[choice];
        if (config == null) return SetupGame();

        ShowConfigurationSummary(config);
        
        var confirmed = _inputHelper.GetBooleanInput("Start game with this preset?", true);
        if (!confirmed)
        {
            return LoadPresetConfiguration();
        }

        // Allow customization of player names for human players
        if (config.HumanPlayerCount > 0)
        {
            var customizeNames = _inputHelper.GetBooleanInput("Customize player names?", false);
            if (customizeNames)
            {
                Console.WriteLine("\n📝 CUSTOMIZE PLAYER NAMES:");
                config.HumanPlayerNames = _inputHelper.GetPlayerNames(config.HumanPlayerCount);
            }
        }

        return config;
    }

    private void ShowConfigurationSummary(GameConfig config)
    {
        Console.WriteLine("\n📊 CONFIGURATION SUMMARY:");
        Console.WriteLine(new string('-', 40));
        Console.WriteLine($"Players: {config.HumanPlayerCount} human, {config.AiPlayerCount} AI ({config.TotalPlayers} total)");
        
        if (config.HumanPlayerNames?.Any() == true)
        {
            Console.WriteLine($"Human players: {string.Join(", ", config.HumanPlayerNames)}");
        }
        
        Console.WriteLine($"Starting chips: ${config.StartingChips:N0} per player");
        Console.WriteLine($"Blinds: ${config.SmallBlind}/{config.BigBlind}");
        
        if (config.Ante > 0)
        {
            Console.WriteLine($"Ante: ${config.Ante}");
        }
        
        if (config.MaxHands > 0)
        {
            Console.WriteLine($"Maximum hands: {config.MaxHands}");
        }
        else
        {
            Console.WriteLine("Hands: Unlimited (play until one winner)");
        }
        
        if (config.IsBlindIncreaseEnabled)
        {
            Console.WriteLine($"Blinds increase: Every {config.BlindIncreaseInterval} hands by {config.BlindIncreaseMultiplier:F1}x");
        }
        
        Console.WriteLine($"Colors: {(config.UseColors ? "Enabled" : "Disabled")}");
        Console.WriteLine($"ASCII Art: {(config.EnableAsciiArt ? "Enabled" : "Disabled")}");
        Console.WriteLine($"Logging: {(config.EnableLogging ? "Enabled" : "Disabled")}");

        // Show AI provider info
        var configuredProviders = config.GetConfiguredProviders();
        if (configuredProviders.Any())
        {
            Console.WriteLine($"AI Providers: {string.Join(", ", config.EnabledProviders.Where(p => configuredProviders.Contains(p) || p == Domain.Enums.AiProvider.None))}");
        }
        else
        {
            Console.WriteLine("AI: Basic (no LLM configured)");
        }

        Console.WriteLine();
    }

    private void ShowRules()
    {
        _inputHelper.ClearScreen();
        Console.WriteLine("📖 TEXAS HOLD'EM POKER RULES");
        Console.WriteLine("=============================");
        Console.WriteLine();
        
        Console.WriteLine("🎯 OBJECTIVE:");
        Console.WriteLine("Win chips by making the best 5-card poker hand using your 2 hole cards");
        Console.WriteLine("and the 5 community cards, or by making all other players fold.");
        Console.WriteLine();
        
        Console.WriteLine("🃏 HAND RANKINGS (highest to lowest):");
        Console.WriteLine("1. Royal Flush    - A, K, Q, J, 10 all same suit");
        Console.WriteLine("2. Straight Flush - Five cards in sequence, same suit");
        Console.WriteLine("3. Four of a Kind - Four cards of same rank");
        Console.WriteLine("4. Full House     - Three of a kind + pair");
        Console.WriteLine("5. Flush          - Five cards same suit");
        Console.WriteLine("6. Straight       - Five cards in sequence");
        Console.WriteLine("7. Three of Kind  - Three cards of same rank");
        Console.WriteLine("8. Two Pair       - Two pairs of different ranks");
        Console.WriteLine("9. One Pair       - Two cards of same rank");
        Console.WriteLine("10. High Card     - Highest single card");
        Console.WriteLine();
        
        Console.WriteLine("🎲 BETTING ROUNDS:");
        Console.WriteLine("1. Pre-flop: After hole cards are dealt");
        Console.WriteLine("2. Flop:     After first 3 community cards");
        Console.WriteLine("3. Turn:     After 4th community card");
        Console.WriteLine("4. River:    After 5th community card");
        Console.WriteLine("5. Showdown: Compare hands if multiple players remain");
        Console.WriteLine();
        
        Console.WriteLine("💰 BETTING ACTIONS:");
        Console.WriteLine("• Fold:  Give up your hand");
        Console.WriteLine("• Check: Pass action (no bet required)");
        Console.WriteLine("• Call:  Match current bet");
        Console.WriteLine("• Bet:   Make first bet in a round");
        Console.WriteLine("• Raise: Increase current bet");
        Console.WriteLine("• All-in: Bet all remaining chips");
        Console.WriteLine();
        
        Console.WriteLine("🤖 AI PERSONALITIES:");
        Console.WriteLine("Each AI player has a unique personality affecting their play style:");
        Console.WriteLine("• Tight:    Plays only premium hands");
        Console.WriteLine("• Loose:    Plays many hands");
        Console.WriteLine("• Aggressive: Bets and raises frequently");
        Console.WriteLine("• Passive:   Calls more than betting");
        Console.WriteLine("• Bluffer:   Bluffs with weak hands");
        Console.WriteLine("• Fish:     Makes poor decisions (beginner)");
        Console.WriteLine("• Shark:    Skilled, optimal play");
        Console.WriteLine("• Maniac:   Very aggressive, unpredictable");
        Console.WriteLine("• Nit:      Extremely tight play");
        Console.WriteLine("• Calling Station: Calls almost everything");
        Console.WriteLine();
        
        _inputHelper.PressAnyKeyToContinue();
    }

    // Preset configuration methods
    private GameConfig CreateQuickGameConfig()
    {
        return new GameConfig
        {
            HumanPlayerCount = 1,
            AiPlayerCount = 1,
            StartingChips = 5000,
            SmallBlind = 25,
            BigBlind = 50,
            Ante = 0,
            MaxHands = 0,
            IsBlindIncreaseEnabled = false,
            UseColors = true,
            EnableAsciiArt = true,
            EnableLogging = true
        };
    }

    private GameConfig CreateClassicTournamentConfig()
    {
        return new GameConfig
        {
            HumanPlayerCount = 1,
            AiPlayerCount = 5,
            StartingChips = 15000,
            SmallBlind = 50,
            BigBlind = 100,
            Ante = 10,
            MaxHands = 0,
            IsBlindIncreaseEnabled = true,
            BlindIncreaseInterval = 15,
            BlindIncreaseMultiplier = 1.5,
            UseColors = true,
            EnableAsciiArt = true,
            EnableLogging = true
        };
    }

    private GameConfig CreateHighStakesConfig()
    {
        return new GameConfig
        {
            HumanPlayerCount = 1,
            AiPlayerCount = 3,
            StartingChips = 50000,
            SmallBlind = 500,
            BigBlind = 1000,
            Ante = 100,
            MaxHands = 50,
            IsBlindIncreaseEnabled = true,
            BlindIncreaseInterval = 10,
            BlindIncreaseMultiplier = 2.0,
            UseColors = true,
            EnableAsciiArt = true,
            EnableLogging = true
        };
    }

    private GameConfig CreateBeginnerFriendlyConfig()
    {
        return new GameConfig
        {
            HumanPlayerCount = 1,
            AiPlayerCount = 3,
            StartingChips = 20000,
            SmallBlind = 10,
            BigBlind = 20,
            Ante = 0,
            MaxHands = 0,
            IsBlindIncreaseEnabled = false,
            UseColors = true,
            EnableAsciiArt = true,
            EnableLogging = true
        };
    }

    private GameConfig CreateAiShowcaseConfig()
    {
        return new GameConfig
        {
            HumanPlayerCount = 0,
            AiPlayerCount = 8,
            StartingChips = 10000,
            SmallBlind = 50,
            BigBlind = 100,
            Ante = 5,
            MaxHands = 100,
            IsBlindIncreaseEnabled = true,
            BlindIncreaseInterval = 12,
            BlindIncreaseMultiplier = 1.3,
            UseColors = true,
            EnableAsciiArt = true,
            EnableLogging = true
        };
    }

    private GameConfig LoadFromConfiguration()
    {
        _inputHelper.ClearScreen();
        Console.WriteLine("🔧 LOADING FROM CONFIGURATION");
        Console.WriteLine("==============================");
        
        var config = _configManager.CreateGameConfigFromDefaults();
        
        Console.WriteLine("✅ Configuration loaded successfully!");
        _configManager.ShowCurrentConfiguration();
        
        var choice = _inputHelper.GetChoiceInput("\nWould you like to:", new Dictionary<string, string>
        {
            {"Use these settings", "use"},
            {"Modify settings", "modify"},
            {"Back to main menu", "back"}
        }, "use");

        return choice switch
        {
            "use" => config,
            "modify" => ConfigureNewGameFromTemplate(config)!,
            "back" => null,
            _ => null
        };
    }

    private void ManageSettings()
    {
        while (true)
        {
            _inputHelper.ClearScreen();
            Console.WriteLine("⚙️  SETTINGS MANAGEMENT");
            Console.WriteLine("=======================");
            
            var choice = _inputHelper.GetChoiceInput("What would you like to do?", new Dictionary<string, string>
            {
                {"View Current Settings", "view"},
                {"Update Game Defaults", "update"},
                {"Reset to Defaults", "reset"},
                {"Back to Main Menu", "back"}
            }, "view");

            switch (choice)
            {
                case "view":
                    _configManager.ShowCurrentConfiguration();
                    _inputHelper.PressAnyKeyToContinue("\nPress any key to continue...");
                    break;
                case "update":
                    UpdateGameDefaults();
                    break;
                case "reset":
                    if (_inputHelper.GetBooleanInput("Are you sure you want to reset all settings to defaults?"))
                    {
                        _configManager.ResetToDefaults();
                    }
                    break;
                case "back":
                    return;
            }
        }
    }

    private void UpdateGameDefaults()
    {
        _inputHelper.ClearScreen();
        Console.WriteLine("🔧 UPDATE GAME DEFAULTS");
        Console.WriteLine("=======================");
        
        var currentConfig = _configManager.CreateGameConfigFromDefaults();
        var updatedConfig = ConfigureNewGameFromTemplate(currentConfig);
        
        if (updatedConfig != null)
        {
            _configManager.UpdateGameDefaults(updatedConfig);
            Console.WriteLine("✅ Game defaults updated and saved!");
        }
        
        _inputHelper.PressAnyKeyToContinue("\nPress any key to continue...");
    }

    private GameConfig ConfigureNewGameFromTemplate(GameConfig template)
    {
        var config = new GameConfig
        {
            HumanPlayerCount = template.HumanPlayerCount,
            AiPlayerCount = template.AiPlayerCount,
            StartingChips = template.StartingChips,
            SmallBlind = template.SmallBlind,
            BigBlind = template.BigBlind,
            Ante = template.Ante,
            MaxHands = template.MaxHands,
            IsBlindIncreaseEnabled = template.IsBlindIncreaseEnabled,
            BlindIncreaseInterval = template.BlindIncreaseInterval,
            BlindIncreaseMultiplier = template.BlindIncreaseMultiplier,
            UseColors = template.UseColors,
            EnableAsciiArt = template.EnableAsciiArt,
            EnableLogging = template.EnableLogging,
            GeminiApiKey = template.GeminiApiKey
        };

        // Use the existing ConfigureNewGame logic but with pre-filled values
        return ConfigureGameSettings(config);
    }

    private GameConfig ConfigureGameSettings(GameConfig config)
    {
        _inputHelper.ClearScreen();
        Console.WriteLine("🎯 GAME CONFIGURATION");
        Console.WriteLine("=====================");

        // Players
        config.HumanPlayerCount = _inputHelper.GetIntegerInput($"Number of human players (1-8, current: {config.HumanPlayerCount})", 1, 8, config.HumanPlayerCount);
        var maxAi = Math.Min(8 - config.HumanPlayerCount, 7);
        config.AiPlayerCount = _inputHelper.GetIntegerInput($"Number of AI players (1-{maxAi}, current: {config.AiPlayerCount})", 1, maxAi, config.AiPlayerCount);

        // Chips and blinds
        config.StartingChips = _inputHelper.GetIntegerInput($"Starting chips per player (current: {config.StartingChips:N0})", 1000, 1000000, config.StartingChips);
        config.SmallBlind = _inputHelper.GetIntegerInput($"Small blind (current: {config.SmallBlind})", 1, config.StartingChips / 100, config.SmallBlind);
        config.BigBlind = _inputHelper.GetIntegerInput($"Big blind (current: {config.BigBlind})", config.SmallBlind, config.StartingChips / 50, config.BigBlind);
        config.Ante = _inputHelper.GetIntegerInput($"Ante per hand (current: {config.Ante})", 0, config.BigBlind, config.Ante);

        // Tournament settings
        config.IsBlindIncreaseEnabled = _inputHelper.GetBooleanInput($"Enable blind increases? (current: {config.IsBlindIncreaseEnabled})", config.IsBlindIncreaseEnabled);
        if (config.IsBlindIncreaseEnabled)
        {
            config.BlindIncreaseInterval = _inputHelper.GetIntegerInput($"Hands between blind increases (current: {config.BlindIncreaseInterval})", 5, 100, config.BlindIncreaseInterval);
            config.BlindIncreaseMultiplier = _inputHelper.GetDoubleInput($"Blind increase multiplier (current: {config.BlindIncreaseMultiplier:F1})", 1.1, 5.0, config.BlindIncreaseMultiplier);
        }

        config.MaxHands = _inputHelper.GetIntegerInput($"Maximum hands (0 for unlimited, current: {config.MaxHands})", 0, 1000, config.MaxHands);

        // Display and features
        config.UseColors = _inputHelper.GetBooleanInput($"Use colored output? (current: {config.UseColors})", config.UseColors);
        config.EnableAsciiArt = _inputHelper.GetBooleanInput($"Enable ASCII art? (current: {config.EnableAsciiArt})", config.EnableAsciiArt);
        config.EnableLogging = _inputHelper.GetBooleanInput($"Enable game logging? (current: {config.EnableLogging})", config.EnableLogging);

        return config;
    }
}