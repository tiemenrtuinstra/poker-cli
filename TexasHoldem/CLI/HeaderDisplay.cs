using System.Reflection;
using Spectre.Console;

namespace TexasHoldem.CLI;

public static class HeaderDisplay
{
    public static void ShowHeader(string? subtitle = null, bool showVersion = true, bool showFeatureTable = true)
    {
        AnsiConsole.Clear();

        // Use the shared header from HeaderHelper (single source of truth)
        HeaderHelper.DisplayMainHeader();

        // Subtitle
        if (!string.IsNullOrEmpty(subtitle))
        {
            AnsiConsole.Write(Align.Center(new Markup(subtitle)));
        }
        else
        {
            AnsiConsole.Write(Align.Center(new Markup("[bold yellow]♠ ♥ ♦ ♣[/]  [italic]The Ultimate CLI Poker Experience[/]  [bold yellow]♣ ♦ ♥ ♠[/]")));
        }
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine();

        // Feature table
        if (showFeatureTable)
        {
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
        }

        // Version info
        if (showVersion)
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var versionStr = version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";
            AnsiConsole.Write(Align.Center(new Markup($"[dim]Version {versionStr} • Made with ♥ in The Netherlands[/]")));
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine();
        }
    }
}
