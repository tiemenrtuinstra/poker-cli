using System.Reflection;
using Spectre.Console;

namespace TexasHoldem.CLI;

public static class HeaderDisplay
{
    public static void ShowHeader(string? subtitle = null, bool showVersion = true, bool showFeatureTable = true)
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

        // ASCII Art Title - Tiemen's Poker Texas Hold'em (big-money-sw font, rainbow)
        var pokerArt = new[]
        {
            "[red] ________[/]  [orange1]__[/]                                              [yellow]__[/]                [lime]_______[/]            [green]__[/]",
            "[red]/        |[/][orange1]/  |[/]                                            [yellow]/  |[/]              [lime]/       \\[/]          [green]/  |[/]",
            "[red]$$$$$$$$/ [/][orange1]$$/[/]  [yellow]______[/]   [lime]___ ___   ______[/]   [green]_______[/]  [cyan]$$/[/] [blue]_______[/]      [magenta]$$$$$$$  |[/][hotpink]______[/]  [red]$$ |  __[/]   [orange1]______[/]    [yellow]______[/]",
            "[red]   $$ |[/]   [orange1]/  |[/][yellow]/      \\[/] [lime]/       \\ /      \\[/] [green]/       |[/][cyan]/  |[/][blue]/       |[/]     [magenta]$$ |__$$ /[/][hotpink]      \\[/][red]$$ | /  |[/] [orange1]/      \\[/]  [yellow]/      \\[/]",
            "[red]   $$ |[/]   [orange1]$$ |[/][yellow]$$$$$$  |[/][lime]$$$$$$$  |$$$$$$  |[/][green]$$$$$$$/ [/][cyan]$$ |[/][blue]$$$$$$$/ [/]     [magenta]$$    $$/ [/][hotpink]$$$$$$  |[/][red]$$  /$$/[/] [orange1]/$$$$$$  |[/][yellow]/$$$$$$  |[/]",
            "[red]   $$ |[/]   [orange1]$$ |[/][yellow]$$ |  $$ |[/][lime]$$ |  $$ |$$ |  $$ |[/][green]$$      \\[/] [cyan]$$ |[/][blue]$$      \\[/]      [magenta]$$$$$$$/  [/][hotpink]$$ |  $$ |[/][red]$$$  \\[/]  [orange1]$$ |  $$ |[/][yellow]$$ |  $$/[/]",
            "[red]   $$ |[/]   [orange1]$$ |[/][yellow]$$ \\__$$ |[/][lime]$$ |  $$ |$$ \\__$$ |[/][green] $$$$$$  |[/][cyan]$$ |[/] [blue]$$$$$$  |[/]    [magenta]$$ |[/]      [hotpink]$$ \\__$$ |[/][red]$$  $$ \\[/] [orange1]$$ \\__$$ |[/][yellow]$$ |[/]",
            "[red]   $$ |[/]   [orange1]$$ |[/][yellow]$$    $$/ [/][lime]$$ |  $$ |$$    $$/ [/][green]/     $$/ [/][cyan]$$ |[/][blue]/     $$/ [/]    [magenta]$$ |[/]      [hotpink]$$    $$/ [/][red]$$ | $$  |[/][orange1]$$    $$/ [/][yellow]$$ |[/]",
            "[red]   $$/[/]    [orange1]$$/[/]  [yellow]$$$$$$/[/]  [lime]$$/   $$/  $$$$$$/[/]  [green]$$$$$$$/[/]  [cyan]$$/[/] [blue]$$$$$$$/[/]      [magenta]$$/[/]        [hotpink]$$$$$$/[/]  [red]$$/   $$/[/]  [orange1]$$$$$$/[/]  [yellow]$$/[/]"
        };

        var holdemArt = new[]
        {
            "[red] ________[/]                                             [yellow]__    __[/]            [lime]__[/]        [green]__  __[/]",
            "[red]/        |[/]                                           [yellow]/  |  /  |[/]          [lime]/  |[/]      [green]/  |/  |[/]",
            "[red]$$$$$$$$/ [/][orange1]______[/]   [yellow]__    __[/]   [lime]______[/]    [green]_______[/]      [cyan]$$ |  $$ |[/] [blue]______[/]  [magenta]$$ |  ____[/][hotpink]$$ $$/[/][red]______[/]   [orange1]___ ___[/]",
            "[red]   $$ |[/]  [orange1]/      \\[/] [yellow]/  \\  /  |[/] [lime]/      \\[/]  [green]/       |[/]     [cyan]$$ |__$$ |[/][blue]/      \\[/] [magenta]$$ | /    [/][hotpink]$$ /  |[/][red]/      \\[/] [orange1]/       \\[/]",
            "[red]   $$ |[/] [orange1]/$$$$$$  |[/][yellow]$$  \\/$$/ [/][lime]/$$$$$$  |[/][green]$$$$$$$/ [/]     [cyan]$$    $$ |[/][blue]$$$$$$  |[/][magenta]$$  /$$$$[/][hotpink]$$ $$ |[/][red]$$$$$$  |[/][orange1]$$$$$$$  |[/]",
            "[red]   $$ |[/] [orange1]$$ |  $$/[/]  [yellow]$$  $$<[/]  [lime]$$ |  $$ |[/][green]$$      \\[/]      [cyan]$$$$$$$$ |[/][blue]$$ |  $$ |[/][magenta]$$ |  $$ [/][hotpink]$$ $$ |[/][red]$$ |  $$ |[/][orange1]$$ |  $$ |[/]",
            "[red]   $$ |[/] [orange1]$$ |[/]      [yellow]/$$$$  \\[/] [lime]$$ \\__$$ |[/] [green]$$$$$$  |[/]     [cyan]$$ |  $$ |[/][blue]$$ \\__$$ |[/][magenta]$$ \\__$$ [/][hotpink]$$ $$ |[/][red]$$ \\__$$ |[/][orange1]$$ |  $$ |[/]",
            "[red]   $$ |[/] [orange1]$$ |[/]     [yellow]$$ /$$  |[/][lime]$$    $$/ [/][green]/     $$/ [/]     [cyan]$$ |  $$ |[/][blue]$$    $$/ [/][magenta]$$    $$ [/][hotpink]$$ $$ |[/][red]$$    $$/ [/][orange1]$$ |  $$ |[/]",
            "[red]   $$/[/]  [orange1]$$/[/]      [yellow]$$/  $$/[/]  [lime]$$$$$$/[/]  [green]$$$$$$$/[/]       [cyan]$$/   $$/[/]  [blue]$$$$$$/[/]   [magenta]$$$$$$$/[/] [hotpink]$$/$$/[/]  [red]$$$$$$/[/]  [orange1]$$/   $$/[/]"
        };

        var titleArt = string.Join("\n", pokerArt) + "\n\n" + string.Join("\n", holdemArt);

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
