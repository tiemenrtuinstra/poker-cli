namespace TexasHoldem.Data.Configuration;

public class DatabaseSettings
{
    public string DatabasePath { get; }
    public string ConnectionString { get; }

    public DatabaseSettings()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(localAppData, "PokerCLI");

        // Ensure directory exists
        if (!Directory.Exists(appFolder))
        {
            Directory.CreateDirectory(appFolder);
        }

        DatabasePath = Path.Combine(appFolder, "poker_history.db");
        ConnectionString = $"Data Source={DatabasePath}";
    }
}
