using TexasHoldem.CLI;
using TexasHoldem.Game;

namespace TexasHoldem;

internal class Program
{
    private static async Task Main(string[] args)
    {
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
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ An error occurred: {ex.Message}");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
