using System.Reflection;
using Spectre.Console;

namespace TexasHoldem.CLI;

public static class HeaderDisplay
{
    public static void ShowHeader(string? subtitle = null, bool showVersion = true)
    {
        AnsiConsole.Clear();

        // ASCII Art Cards Header
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

        // ASCII Art Title - TEXAS HOLD'EM
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
