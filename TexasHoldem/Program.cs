using System.Reflection;
using System.Text;
using Spectre.Console;
using TexasHoldem.CLI;
using TexasHoldem.Game;

namespace TexasHoldem;

internal class Program
{
    private static async Task<int> Main(string[] args)
    {
        // Enable UTF-8 output for proper display of € and emoji symbols
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        // Handle command line arguments
        if (args.Length > 0)
        {
            var arg = args[0].ToLowerInvariant();

            switch (arg)
            {
                case "--update":
                case "-u":
                    // Check if a specific version was provided
                    string? targetVersion = args.Length > 1 ? args[1] : null;
                    return await VersionChecker.RunUpdateCommandAsync(targetVersion);

                case "--version":
                case "-v":
                    var version = Assembly.GetExecutingAssembly().GetName().Version;
                    Console.WriteLine($"poker-cli version {version?.ToString(3) ?? "unknown"}");
                    return 0;

                case "--help":
                case "-h":
                    ShowHelp();
                    return 0;
            }
        }

        Console.Clear();

        // Check for updates in the background, but don't block startup for too long
        await VersionChecker.CheckForUpdatesAsync();

        try
        {
            var menu = new Menu();
            bool keepRunning = true;

            while (keepRunning)
            {
                var gameConfig = menu.SetupGame();

                if (gameConfig == null)
                {
                    // User chose to exit
                    keepRunning = false;
                    continue;
                }

                // Initialize symbols based on config setting
                CLI.Symbols.Initialize(gameConfig.UseUnicodeSymbols);

                if (gameConfig.IsNetworkGame)
                {
                    // Network game - handled by NetworkMenu, loop back to multiplayer
                    var networkResult = menu.LastNetworkGameResult;
                    if (networkResult?.GamePlayed == true)
                    {
                        // Game was played, show return options
                        Console.Clear();
                        var returnChoice = AnsiConsole.Prompt(
                            new SelectionPrompt<string>()
                                .Title("[bold green]Game finished! What would you like to do?[/]")
                                .HighlightStyle(new Style(Color.Black, Color.Green))
                                .AddChoices(new[]
                                {
                                    "🌐  Return to Multiplayer Menu",
                                    "🏠  Return to Main Menu",
                                    "🚪  Exit"
                                }));

                        if (returnChoice.Contains("Exit"))
                        {
                            keepRunning = false;
                        }
                        // Otherwise loop continues and shows appropriate menu
                    }
                }
                else
                {
                    // Local game - loop to allow "Play Again"
                    bool playingLocal = true;
                    while (playingLocal)
                    {
                        var game = new TexasHoldemGame(gameConfig);
                        await game.StartGame();

                        // Game finished - show return options
                        Console.Clear();
                        var returnChoice = AnsiConsole.Prompt(
                            new SelectionPrompt<string>()
                                .Title("[bold green]Game finished! What would you like to do?[/]")
                                .HighlightStyle(new Style(Color.Black, Color.Green))
                                .AddChoices(new[]
                                {
                                    "🔄  Play Again (Same Settings)",
                                    "🏠  Return to Main Menu",
                                    "🚪  Exit"
                                }));

                        if (returnChoice.Contains("Exit"))
                        {
                            keepRunning = false;
                            playingLocal = false;
                        }
                        else if (returnChoice.Contains("Main Menu"))
                        {
                            playingLocal = false;
                            // keepRunning stays true, loop continues to main menu
                        }
                        // Play Again - playingLocal stays true, inner loop continues
                    }
                }
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ An error occurred: {ex.Message}");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            return 1;
        }
    }

    private static void ShowHelp()
    {
        Console.WriteLine("poker-cli - Texas Hold'em Poker CLI Game");
        Console.WriteLine();
        Console.WriteLine("Usage: poker-cli [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --update, -u [version]  Manage versions (upgrade, downgrade, or reinstall)");
        Console.WriteLine("                          Without version: interactive version selector");
        Console.WriteLine("                          With version: install specific version (e.g. v1.2.0)");
        Console.WriteLine("  --version, -v           Display the current version");
        Console.WriteLine("  --help, -h              Show this help message");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  poker-cli               Start the game");
        Console.WriteLine("  poker-cli --update      Open interactive version manager");
        Console.WriteLine("  poker-cli -u v1.2.0     Install specific version v1.2.0");
        Console.WriteLine("  poker-cli --version     Show current version");
    }
}
