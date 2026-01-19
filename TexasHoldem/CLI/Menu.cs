using System.Reflection;
using Spectre.Console;
using TexasHoldem.Game;
using TexasHoldem.Game.Enums;

namespace TexasHoldem.CLI;

public class Menu
{
    private readonly InputHelper _inputHelper;
    private readonly ConfigurationManager _configManager;
    private readonly NetworkMenu _networkMenu;

    public NetworkGameResult? LastNetworkGameResult { get; private set; }

    public Menu()
    {
        _inputHelper = new InputHelper();
        _configManager = new ConfigurationManager();
        _networkMenu = new NetworkMenu();
    }

    public GameConfig? SetupGame()
    {
        ShowWelcomeMessage();

        var mainChoice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold green]What would you like to do?[/]")
                .PageSize(8)
                .HighlightStyle(new Style(Color.Black, Color.Green))
                .AddChoices(new[]
                {
                    "🎲  Start New Game",
                    "⚡  Quick Start (Default Settings)",
                    "🌐  Multiplayer (LAN)",
                    "📁  Load Preset Configuration",
                    "⚙️   Manage Settings",
                    "📖  View Rules",
                    "🚪  Exit"
                }));

        return mainChoice switch
        {
            "🎲  Start New Game" => ConfigureNewGame(),
            "⚡  Quick Start (Default Settings)" => CreateQuickStartConfig(),
            "🌐  Multiplayer (LAN)" => ShowMultiplayerAndReturn(),
            "📁  Load Preset Configuration" => LoadPresetConfiguration(),
            "⚙️   Manage Settings" => ManageSettingsAndReturn(),
            "📖  View Rules" => ShowRulesAndReturn(),
            "🚪  Exit" => null,
            _ => null
        };
    }

    private GameConfig? ManageSettingsAndReturn()
    {
        ManageSettings();
        return SetupGame();
    }

    private GameConfig? ShowRulesAndReturn()
    {
        ShowRules();
        return SetupGame();
    }

    private GameConfig? ShowMultiplayerAndReturn()
    {
        var result = _networkMenu.ShowMultiplayerMenuAsync().GetAwaiter().GetResult();
        if (result != null)
        {
            LastNetworkGameResult = result;
            // Return a config that indicates this is a network game
            return new GameConfig
            {
                IsNetworkGame = true,
                HumanPlayerCount = result.Lobby?.Players.Count(p => !p.IsAi) ?? 1,
                AiPlayerCount = result.Lobby?.Players.Count(p => p.IsAi) ?? 0,
                StartingChips = result.Lobby?.Settings.StartingChips ?? 10000,
                SmallBlind = result.Lobby?.Settings.SmallBlind ?? 50,
                BigBlind = result.Lobby?.Settings.BigBlind ?? 100,
                Ante = result.Lobby?.Settings.Ante ?? 0,
                UseColors = true,
                EnableAsciiArt = true,
                EnableLogging = true
            };
        }
        return SetupGame();
    }

    private GameConfig CreateQuickStartConfig()
    {
        // Quick start with sensible defaults
        return new GameConfig
        {
            HumanPlayerCount = 1,
            AiPlayerCount = 5,
            HumanPlayerNames = new List<string> { "Player" },
            StartingChips = 10000,
            SmallBlind = 50,
            BigBlind = 100,
            Ante = 0,
            MaxHands = 0,
            UseColors = true,
            EnableAsciiArt = true,
            EnableLogging = true
        };
    }

    private void ShowWelcomeMessage()
    {
        AnsiConsole.Clear();

        // ASCII Art Cards Header - centered using Spectre.Console Align
        var cardsArt = string.Join("\n",
            "[red]┌─────┐ ┌─────┐[/] [blue]┌─────┐ ┌─────┐[/]",
            "[red]│A    │ │K    │[/] [blue]│Q    │ │J    │[/]",
            "[red]│  ♥  │ │  ♦  │[/] [blue]│  ♠  │ │  ♣  │[/]",
            "[red]│    A│ │    K│[/] [blue]│    Q│ │    J│[/]",
            "[red]└─────┘ └─────┘[/] [blue]└─────┘ └─────┘[/]"
        );

        AnsiConsole.WriteLine();
        AnsiConsole.Write(Align.Center(new Markup(cardsArt)));
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine();

        // ASCII Art Title - TEXAS HOLD'EM - centered
        var titleArt = string.Join("\n",
            "[green] _____   _____  __  __     _      ____      _   _    ___    _       ____    _   _____   __  __[/]",
            "[green]|_   _| | ____| \\ \\/ /    / \\    / ___|    | | | |  / _ \\  | |     |  _ \\  ( ) | ____| |  \\/  |[/]",
            "[green]  | |   |  _|    \\  /    / _ \\   \\___ \\    | |_| | | | | | | |     | | | | |/  |  _|   | |\\/| |[/]",
            "[green]  | |   | |___   /  \\   / ___ \\   ___) |   |  _  | | |_| | | |___  | |_| |     | |___  | |  | |[/]",
            "[green]  |_|   |_____| /_/\\_\\ /_/   \\_\\ |____/    |_| |_|  \\___/  |_____| |____/      |_____| |_|  |_|[/]"
        );

        AnsiConsole.Write(Align.Center(new Markup(titleArt)));
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine();

        // Subtitle - centered
        AnsiConsole.Write(Align.Center(new Markup("[bold yellow]♠ ♥ ♦ ♣[/]  [italic]The Ultimate CLI Poker Experience[/]  [bold yellow]♣ ♦ ♥ ♠[/]")));
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine();

        // Info table - centered
        var infoTable = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .AddColumn(new TableColumn("[bold cyan]Feature[/]").Centered())
            .AddColumn(new TableColumn("[bold cyan]Description[/]").Centered())
            .Centered();

        infoTable.AddRow("[green]🎮 Single & Multiplayer[/]", "Play solo or with friends (hot-seat)");
        infoTable.AddRow("[yellow]🤖 Smart AI Opponents[/]", "Multiple AI personalities & LLM support");
        infoTable.AddRow("[magenta]🏆 Tournament Mode[/]", "Increasing blinds & elimination");
        infoTable.AddRow("[cyan]📊 Statistics & Replay[/]", "Track your progress & review hands");

        AnsiConsole.Write(infoTable);
        AnsiConsole.WriteLine();

        // Version info - centered
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        var versionStr = version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.2.0";
        AnsiConsole.Write(Align.Center(new Markup($"[dim]Version {versionStr} • Made with ♥ in The Netherlands[/]")));
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine();
    }

    private GameConfig ConfigureNewGame()
    {
        AnsiConsole.Clear();

        // Header
        AnsiConsole.Write(new Rule("[bold green]GAME SETUP[/]").RuleStyle("green"));
        AnsiConsole.WriteLine();

        var config = new GameConfig();

        // Player setup section
        AnsiConsole.Write(new Rule("[bold cyan]👥 Player Configuration[/]").RuleStyle("cyan").LeftJustified());

        config.HumanPlayerCount = AnsiConsole.Prompt(
            new TextPrompt<int>("[yellow]Number of human players[/] [dim](0-8)[/]:")
                .DefaultValue(1)
                .Validate(n => n >= 0 && n <= 8 ? ValidationResult.Success() : ValidationResult.Error("[red]Must be between 0 and 8[/]")));

        var maxAiPlayers = 8 - config.HumanPlayerCount;
        if (maxAiPlayers > 0)
        {
            var defaultAi = Math.Min(5, maxAiPlayers);
            config.AiPlayerCount = AnsiConsole.Prompt(
                new TextPrompt<int>($"[yellow]Number of AI players[/] [dim](0-{maxAiPlayers})[/]:")
                    .DefaultValue(defaultAi)
                    .Validate(n => n >= 0 && n <= maxAiPlayers ? ValidationResult.Success() : ValidationResult.Error($"[red]Must be between 0 and {maxAiPlayers}[/]")));
        }
        else
        {
            config.AiPlayerCount = 0;
        }

        if (config.TotalPlayers < 2)
        {
            AnsiConsole.MarkupLine("[red]Need at least 2 total players![/]");
            AnsiConsole.WriteLine();
            return ConfigureNewGame();
        }

        // Get human player names
        if (config.HumanPlayerCount > 0)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Rule("[bold cyan]📝 Player Names[/]").RuleStyle("cyan").LeftJustified());
            config.HumanPlayerNames = new List<string>();
            for (int i = 0; i < config.HumanPlayerCount; i++)
            {
                var name = AnsiConsole.Prompt(
                    new TextPrompt<string>($"[yellow]Name for Player {i + 1}[/]:")
                        .DefaultValue($"Player {i + 1}"));
                config.HumanPlayerNames.Add(name);
            }
        }

        // Chip and blind configuration
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[bold cyan]💰 Chips & Blinds[/]").RuleStyle("cyan").LeftJustified());

        config.StartingChips = AnsiConsole.Prompt(
            new TextPrompt<int>("[yellow]Starting chips per player[/]:")
                .DefaultValue(10000));

        config.SmallBlind = AnsiConsole.Prompt(
            new TextPrompt<int>("[yellow]Small blind[/]:")
                .DefaultValue(50));

        config.BigBlind = AnsiConsole.Prompt(
            new TextPrompt<int>("[yellow]Big blind[/]:")
                .DefaultValue(config.SmallBlind * 2));

        config.Ante = AnsiConsole.Prompt(
            new TextPrompt<int>("[yellow]Ante[/] [dim](0 for none)[/]:")
                .DefaultValue(0));

        // Tournament settings
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[bold cyan]🏆 Tournament Settings[/]").RuleStyle("cyan").LeftJustified());

        config.MaxHands = AnsiConsole.Prompt(
            new TextPrompt<int>("[yellow]Max hands[/] [dim](0 for unlimited)[/]:")
                .DefaultValue(0));

        config.IsBlindIncreaseEnabled = AnsiConsole.Confirm("[yellow]Enable blind increases?[/]", false);

        if (config.IsBlindIncreaseEnabled)
        {
            config.BlindIncreaseInterval = AnsiConsole.Prompt(
                new TextPrompt<int>("[yellow]Increase blinds every X hands[/]:")
                    .DefaultValue(10));

            config.BlindIncreaseMultiplier = AnsiConsole.Prompt(
                new TextPrompt<double>("[yellow]Blind multiplier[/]:")
                    .DefaultValue(1.5));
        }

        // Display settings
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[bold cyan]🎨 Display Settings[/]").RuleStyle("cyan").LeftJustified());

        config.UseColors = AnsiConsole.Confirm("[yellow]Use colors?[/]", true);
        config.EnableAsciiArt = AnsiConsole.Confirm("[yellow]Enable ASCII art?[/]", true);
        config.UseUnicodeSymbols = AnsiConsole.Confirm("[yellow]Use Unicode symbols (EUR, emoji)?[/]", true);
        config.EnableLogging = AnsiConsole.Confirm("[yellow]Enable logging?[/]", true);

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
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[bold cyan]🤖 AI Configuration[/]").RuleStyle("cyan").LeftJustified());

        var configuredProviders = config.GetConfiguredProviders();
        if (configuredProviders.Any())
        {
            AnsiConsole.MarkupLine($"[green]✓[/] API keys found: [bold]{string.Join(", ", configuredProviders)}[/]");

            var activeProviders = config.EnabledProviders
                .Where(p => p == Game.Enums.AiProvider.None || configuredProviders.Contains(p))
                .ToList();

            if (activeProviders.Any(p => p != Game.Enums.AiProvider.None))
            {
                AnsiConsole.MarkupLine($"[green]✓[/] Will use: [bold]{string.Join(", ", activeProviders.Where(p => p != Game.Enums.AiProvider.None))}[/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[yellow]![/] Using: Basic AI (enabled providers have no API keys)");
            }
        }
        else
        {
            AnsiConsole.MarkupLine("[yellow]![/] No API keys found - using basic AI");
            AnsiConsole.MarkupLine("[dim]   Add keys to .env file to enable LLM players[/]");
        }

        // Confirm configuration
        ShowConfigurationSummary(config);

        AnsiConsole.WriteLine();
        var confirmed = AnsiConsole.Confirm("[bold green]Start game with this configuration?[/]", true);
        if (!confirmed)
        {
            return ConfigureNewGame();
        }

        return config;
    }

    private GameConfig? LoadPresetConfiguration()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule("[bold green]⚙️ Preset Configurations[/]").RuleStyle("green"));
        AnsiConsole.WriteLine();

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold cyan]Select a preset configuration:[/]")
                .PageSize(8)
                .HighlightStyle(new Style(Color.Black, Color.Cyan1))
                .AddChoices(new[]
                {
                    "⚡  Quick Game (1v1)",
                    "🏆  Classic Tournament (6 players)",
                    "💎  High Stakes (4 players)",
                    "🌱  Beginner Friendly",
                    "🤖  AI Showcase (8 AI players)",
                    "🔙  Back to Main Menu"
                }));

        if (choice == "🔙  Back to Main Menu")
        {
            return SetupGame();
        }

        var config = choice switch
        {
            "⚡  Quick Game (1v1)" => CreateQuickGameConfig(),
            "🏆  Classic Tournament (6 players)" => CreateClassicTournamentConfig(),
            "💎  High Stakes (4 players)" => CreateHighStakesConfig(),
            "🌱  Beginner Friendly" => CreateBeginnerFriendlyConfig(),
            "🤖  AI Showcase (8 AI players)" => CreateAiShowcaseConfig(),
            _ => null
        };

        if (config == null) return SetupGame();

        ShowConfigurationSummary(config);

        AnsiConsole.WriteLine();
        var confirmed = AnsiConsole.Confirm("[bold green]Start game with this preset?[/]", true);
        if (!confirmed)
        {
            return LoadPresetConfiguration();
        }

        // Allow customization of player names for human players
        if (config.HumanPlayerCount > 0)
        {
            var customizeNames = AnsiConsole.Confirm("[yellow]Customize player names?[/]", false);
            if (customizeNames)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.Write(new Rule("[bold cyan]📝 Customize Player Names[/]").RuleStyle("cyan").LeftJustified());
                config.HumanPlayerNames = new List<string>();
                for (int i = 0; i < config.HumanPlayerCount; i++)
                {
                    var name = AnsiConsole.Prompt(
                        new TextPrompt<string>($"[yellow]Name for Player {i + 1}[/]:")
                            .DefaultValue($"Player {i + 1}"));
                    config.HumanPlayerNames.Add(name);
                }
            }
        }

        return config;
    }

    private void ShowConfigurationSummary(GameConfig config)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[bold yellow]📊 Configuration Summary[/]").RuleStyle("yellow"));
        AnsiConsole.WriteLine();

        // Create a nice summary table
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Green)
            .AddColumn(new TableColumn("[bold]Setting[/]"))
            .AddColumn(new TableColumn("[bold]Value[/]"));

        // Players
        table.AddRow("[cyan]Players[/]", $"{config.HumanPlayerCount} human, {config.AiPlayerCount} AI ([bold]{config.TotalPlayers}[/] total)");

        if (config.HumanPlayerNames?.Any() == true)
        {
            table.AddRow("[cyan]Human Players[/]", string.Join(", ", config.HumanPlayerNames));
        }

        // Chips & Blinds
        table.AddRow("[green]Starting Chips[/]", $"[bold]€{config.StartingChips:N0}[/] per player");
        table.AddRow("[green]Blinds[/]", $"[bold]€{config.SmallBlind}/€{config.BigBlind}[/]");

        if (config.Ante > 0)
        {
            table.AddRow("[green]Ante[/]", $"€{config.Ante}");
        }

        // Tournament
        var handsDisplay = config.MaxHands > 0 ? $"{config.MaxHands} hands" : "[dim]Unlimited[/]";
        table.AddRow("[magenta]Max Hands[/]", handsDisplay);

        if (config.IsBlindIncreaseEnabled)
        {
            table.AddRow("[magenta]Blind Increase[/]", $"Every {config.BlindIncreaseInterval} hands × {config.BlindIncreaseMultiplier:F1}");
        }

        // Display settings
        table.AddRow("[yellow]Colors[/]", config.UseColors ? "[green]✓[/]" : "[red]✗[/]");
        table.AddRow("[yellow]ASCII Art[/]", config.EnableAsciiArt ? "[green]✓[/]" : "[red]✗[/]");
        table.AddRow("[yellow]Unicode Symbols[/]", config.UseUnicodeSymbols ? "[green]✓[/]" : "[red]✗[/]");
        table.AddRow("[yellow]Logging[/]", config.EnableLogging ? "[green]✓[/]" : "[red]✗[/]");

        // AI provider info
        var configuredProviders = config.GetConfiguredProviders();
        if (configuredProviders.Any())
        {
            var providers = string.Join(", ", config.EnabledProviders.Where(p => configuredProviders.Contains(p) || p == Game.Enums.AiProvider.None));
            table.AddRow("[blue]AI Providers[/]", providers);
        }
        else
        {
            table.AddRow("[blue]AI[/]", "[dim]Basic (no LLM)[/]");
        }

        AnsiConsole.Write(table);
    }

    private void ShowRules()
    {
        AnsiConsole.Clear();

        AnsiConsole.Write(
            new FigletText("RULES")
                .Color(Color.Yellow)
                .Centered());

        AnsiConsole.Write(new Rule("[bold yellow]Texas Hold'em Poker Rules[/]").RuleStyle("yellow"));
        AnsiConsole.WriteLine();

        // Objective
        var objectivePanel = new Panel(
            new Markup("Win chips by making the [bold]best 5-card poker hand[/] using your 2 hole cards\nand the 5 community cards, or by making all other players [bold]fold[/]."))
            .Header("[bold green]OBJECTIVE[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Green);
        AnsiConsole.Write(objectivePanel);
        AnsiConsole.WriteLine();

        // Hand Rankings
        AnsiConsole.Write(new Rule("[bold cyan]Hand Rankings (highest to lowest)[/]").RuleStyle("cyan").LeftJustified());

        var handTable = new Table()
            .Border(TableBorder.Simple)
            .AddColumn(new TableColumn("[bold]Rank[/]").Centered())
            .AddColumn(new TableColumn("[bold]Hand[/]"))
            .AddColumn(new TableColumn("[bold]Description[/]"));

        handTable.AddRow("[yellow]1[/]", "[bold]Royal Flush[/]", "A, K, Q, J, 10 all same suit");
        handTable.AddRow("[yellow]2[/]", "[bold]Straight Flush[/]", "Five cards in sequence, same suit");
        handTable.AddRow("[yellow]3[/]", "[bold]Four of a Kind[/]", "Four cards of same rank");
        handTable.AddRow("[yellow]4[/]", "[bold]Full House[/]", "Three of a kind + pair");
        handTable.AddRow("[yellow]5[/]", "[bold]Flush[/]", "Five cards same suit");
        handTable.AddRow("[yellow]6[/]", "[bold]Straight[/]", "Five cards in sequence");
        handTable.AddRow("[yellow]7[/]", "[bold]Three of a Kind[/]", "Three cards of same rank");
        handTable.AddRow("[yellow]8[/]", "[bold]Two Pair[/]", "Two pairs of different ranks");
        handTable.AddRow("[yellow]9[/]", "[bold]One Pair[/]", "Two cards of same rank");
        handTable.AddRow("[yellow]10[/]", "[bold]High Card[/]", "Highest single card");

        AnsiConsole.Write(handTable);
        AnsiConsole.WriteLine();

        // Betting Rounds
        AnsiConsole.Write(new Rule("[bold magenta]Betting Rounds[/]").RuleStyle("magenta").LeftJustified());

        var roundsTable = new Table()
            .Border(TableBorder.Simple)
            .AddColumn("[bold]Round[/]")
            .AddColumn("[bold]When[/]");

        roundsTable.AddRow("[cyan]Pre-flop[/]", "After hole cards are dealt");
        roundsTable.AddRow("[cyan]Flop[/]", "After first 3 community cards");
        roundsTable.AddRow("[cyan]Turn[/]", "After 4th community card");
        roundsTable.AddRow("[cyan]River[/]", "After 5th community card");
        roundsTable.AddRow("[cyan]Showdown[/]", "Compare hands if multiple players remain");

        AnsiConsole.Write(roundsTable);
        AnsiConsole.WriteLine();

        // Betting Actions
        AnsiConsole.Write(new Rule("[bold green]Betting Actions[/]").RuleStyle("green").LeftJustified());

        var actionsTable = new Table()
            .Border(TableBorder.Simple)
            .AddColumn("[bold]Action[/]")
            .AddColumn("[bold]Description[/]");

        actionsTable.AddRow("[red]Fold[/]", "Give up your hand");
        actionsTable.AddRow("[dim]Check[/]", "Pass action (no bet required)");
        actionsTable.AddRow("[yellow]Call[/]", "Match current bet");
        actionsTable.AddRow("[green]Bet[/]", "Make first bet in a round");
        actionsTable.AddRow("[cyan]Raise[/]", "Increase current bet");
        actionsTable.AddRow("[magenta]All-in[/]", "Bet all remaining chips");

        AnsiConsole.Write(actionsTable);
        AnsiConsole.WriteLine();

        // AI Personalities
        AnsiConsole.Write(new Rule("[bold blue]AI Personalities[/]").RuleStyle("blue").LeftJustified());
        AnsiConsole.MarkupLine("[dim]Each AI player has a unique personality affecting their play style:[/]");
        AnsiConsole.WriteLine();

        var personalityTable = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Blue)
            .AddColumn("[bold]Personality[/]")
            .AddColumn("[bold]Play Style[/]");

        personalityTable.AddRow("[cyan]Tight[/]", "Plays only premium hands");
        personalityTable.AddRow("[green]Loose[/]", "Plays many hands");
        personalityTable.AddRow("[red]Aggressive[/]", "Bets and raises frequently");
        personalityTable.AddRow("[dim]Passive[/]", "Calls more than betting");
        personalityTable.AddRow("[magenta]Bluffer[/]", "Bluffs with weak hands");
        personalityTable.AddRow("[yellow]Shark[/]", "Skilled, optimal play");
        personalityTable.AddRow("[orange1]Maniac[/]", "Very aggressive, unpredictable");

        AnsiConsole.Write(personalityTable);
        AnsiConsole.WriteLine();

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

    private GameConfig? LoadFromConfiguration()
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
            "modify" => ConfigureNewGameFromTemplate(config),
            "back" => null,
            _ => null
        };
    }

    private void ManageSettings()
    {
        while (true)
        {
            AnsiConsole.Clear();

            AnsiConsole.Write(
                new FigletText("SETTINGS")
                    .Color(Color.Grey)
                    .Centered());

            AnsiConsole.Write(new Rule("[bold grey]Settings Management[/]").RuleStyle("grey"));
            AnsiConsole.WriteLine();

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold green]What would you like to do?[/]")
                    .PageSize(8)
                    .HighlightStyle(new Style(Color.Black, Color.Grey))
                    .AddChoices(new[]
                    {
                        "View Current Settings",
                        "Update Game Defaults",
                        "Reset to Defaults",
                        "Back to Main Menu"
                    }));

            switch (choice)
            {
                case "View Current Settings":
                    _configManager.ShowCurrentConfiguration();
                    _inputHelper.PressAnyKeyToContinue();
                    break;
                case "Update Game Defaults":
                    UpdateGameDefaults();
                    break;
                case "Reset to Defaults":
                    if (AnsiConsole.Confirm("[yellow]Are you sure you want to reset all settings to defaults?[/]", false))
                    {
                        _configManager.ResetToDefaults();
                        AnsiConsole.MarkupLine("[green]Settings reset to defaults![/]");
                        _inputHelper.PressAnyKeyToContinue();
                    }
                    break;
                case "Back to Main Menu":
                    return;
            }
        }
    }

    private void UpdateGameDefaults()
    {
        AnsiConsole.Clear();

        AnsiConsole.Write(new Rule("[bold cyan]Update Game Defaults[/]").RuleStyle("cyan"));
        AnsiConsole.WriteLine();

        var currentConfig = _configManager.CreateGameConfigFromDefaults();
        var updatedConfig = ConfigureNewGameFromTemplate(currentConfig);

        if (updatedConfig != null)
        {
            _configManager.UpdateGameDefaults(updatedConfig);
            AnsiConsole.MarkupLine("[green]Game defaults updated and saved![/]");
        }

        _inputHelper.PressAnyKeyToContinue();
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
            UseUnicodeSymbols = template.UseUnicodeSymbols,
            EnableLogging = template.EnableLogging,
            GeminiApiKey = template.GeminiApiKey
        };

        // Use the existing ConfigureNewGame logic but with pre-filled values
        return ConfigureGameSettings(config);
    }

    private GameConfig ConfigureGameSettings(GameConfig config)
    {
        AnsiConsole.Clear();

        AnsiConsole.Write(new Rule("[bold cyan]Game Configuration[/]").RuleStyle("cyan"));
        AnsiConsole.WriteLine();

        // Players section
        AnsiConsole.Write(new Rule("[bold yellow]Player Settings[/]").RuleStyle("yellow").LeftJustified());

        config.HumanPlayerCount = AnsiConsole.Prompt(
            new TextPrompt<int>($"[yellow]Number of human players[/] [dim](current: {config.HumanPlayerCount})[/]:")
                .DefaultValue(config.HumanPlayerCount)
                .Validate(n => n >= 0 && n <= 8 ? ValidationResult.Success() : ValidationResult.Error("[red]Must be between 0 and 8[/]")));

        var maxAi = Math.Min(8 - config.HumanPlayerCount, 7);
        config.AiPlayerCount = AnsiConsole.Prompt(
            new TextPrompt<int>($"[yellow]Number of AI players[/] [dim](current: {config.AiPlayerCount})[/]:")
                .DefaultValue(config.AiPlayerCount)
                .Validate(n => n >= 0 && n <= maxAi ? ValidationResult.Success() : ValidationResult.Error($"[red]Must be between 0 and {maxAi}[/]")));

        AnsiConsole.WriteLine();

        // Chips and blinds section
        AnsiConsole.Write(new Rule("[bold green]Chips & Blinds[/]").RuleStyle("green").LeftJustified());

        config.StartingChips = AnsiConsole.Prompt(
            new TextPrompt<int>($"[yellow]Starting chips per player[/] [dim](current: €{config.StartingChips:N0})[/]:")
                .DefaultValue(config.StartingChips)
                .Validate(n => n >= 1000 && n <= 1000000 ? ValidationResult.Success() : ValidationResult.Error("[red]Must be between 1,000 and 1,000,000[/]")));

        config.SmallBlind = AnsiConsole.Prompt(
            new TextPrompt<int>($"[yellow]Small blind[/] [dim](current: €{config.SmallBlind})[/]:")
                .DefaultValue(config.SmallBlind)
                .Validate(n => n >= 1 && n <= config.StartingChips / 100 ? ValidationResult.Success() : ValidationResult.Error("[red]Invalid small blind[/]")));

        config.BigBlind = AnsiConsole.Prompt(
            new TextPrompt<int>($"[yellow]Big blind[/] [dim](current: €{config.BigBlind})[/]:")
                .DefaultValue(config.BigBlind)
                .Validate(n => n >= config.SmallBlind && n <= config.StartingChips / 50 ? ValidationResult.Success() : ValidationResult.Error("[red]Invalid big blind[/]")));

        config.Ante = AnsiConsole.Prompt(
            new TextPrompt<int>($"[yellow]Ante per hand[/] [dim](current: €{config.Ante})[/]:")
                .DefaultValue(config.Ante)
                .Validate(n => n >= 0 && n <= config.BigBlind ? ValidationResult.Success() : ValidationResult.Error("[red]Invalid ante[/]")));

        AnsiConsole.WriteLine();

        // Tournament settings
        AnsiConsole.Write(new Rule("[bold magenta]Tournament Settings[/]").RuleStyle("magenta").LeftJustified());

        config.IsBlindIncreaseEnabled = AnsiConsole.Confirm($"[yellow]Enable blind increases?[/] [dim](current: {config.IsBlindIncreaseEnabled})[/]", config.IsBlindIncreaseEnabled);

        if (config.IsBlindIncreaseEnabled)
        {
            config.BlindIncreaseInterval = AnsiConsole.Prompt(
                new TextPrompt<int>($"[yellow]Hands between blind increases[/] [dim](current: {config.BlindIncreaseInterval})[/]:")
                    .DefaultValue(config.BlindIncreaseInterval)
                    .Validate(n => n >= 5 && n <= 100 ? ValidationResult.Success() : ValidationResult.Error("[red]Must be between 5 and 100[/]")));

            config.BlindIncreaseMultiplier = AnsiConsole.Prompt(
                new TextPrompt<double>($"[yellow]Blind increase multiplier[/] [dim](current: {config.BlindIncreaseMultiplier:F1})[/]:")
                    .DefaultValue(config.BlindIncreaseMultiplier)
                    .Validate(n => n >= 1.1 && n <= 5.0 ? ValidationResult.Success() : ValidationResult.Error("[red]Must be between 1.1 and 5.0[/]")));
        }

        config.MaxHands = AnsiConsole.Prompt(
            new TextPrompt<int>($"[yellow]Maximum hands[/] [dim](0 = unlimited, current: {config.MaxHands})[/]:")
                .DefaultValue(config.MaxHands)
                .Validate(n => n >= 0 && n <= 1000 ? ValidationResult.Success() : ValidationResult.Error("[red]Must be between 0 and 1000[/]")));

        AnsiConsole.WriteLine();

        // Display settings
        AnsiConsole.Write(new Rule("[bold blue]Display Settings[/]").RuleStyle("blue").LeftJustified());

        config.UseColors = AnsiConsole.Confirm($"[yellow]Use colored output?[/] [dim](current: {config.UseColors})[/]", config.UseColors);
        config.EnableAsciiArt = AnsiConsole.Confirm($"[yellow]Enable ASCII art?[/] [dim](current: {config.EnableAsciiArt})[/]", config.EnableAsciiArt);
        config.UseUnicodeSymbols = AnsiConsole.Confirm($"[yellow]Use Unicode symbols (EUR, emoji)?[/] [dim](current: {config.UseUnicodeSymbols})[/]", config.UseUnicodeSymbols);
        config.EnableLogging = AnsiConsole.Confirm($"[yellow]Enable game logging?[/] [dim](current: {config.EnableLogging})[/]", config.EnableLogging);

        return config;
    }
}