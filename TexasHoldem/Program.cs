using System.Reflection;
using TexasHoldem.CLI;
using TexasHoldem.Game;

namespace TexasHoldem;

internal class Program
{
    private static async Task<int> Main(string[] args)
    {
        // Handle command line arguments
        if (args.Length > 0)
        {
            var arg = args[0].ToLowerInvariant();

            switch (arg)
            {
                case "--update":
                case "-u":
                    return await VersionChecker.RunUpdateCommandAsync();

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
        Console.WriteLine("  --update, -u     Check for updates and install if available");
        Console.WriteLine("  --version, -v    Display the current version");
        Console.WriteLine("  --help, -h       Show this help message");
        Console.WriteLine();
        Console.WriteLine("Run without options to start the game.");
    }
}
