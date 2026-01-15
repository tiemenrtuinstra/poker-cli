using System.Reflection;
using System.Text;
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
            var gameConfig = menu.SetupGame();

            if (gameConfig != null)
            {
                // Initialize symbols based on config setting
                CLI.Symbols.Initialize(gameConfig.UseUnicodeSymbols);

                var game = new TexasHoldemGame(gameConfig);
                await game.StartGame();
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
