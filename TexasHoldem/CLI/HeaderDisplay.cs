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

        // ASCII Art Title - Tiemen's Poker Texas Hold'em (clean readable font, rainbow)
        var titleArt = string.Join("\n",
            "[red]╔╦╗[/][orange1]╦[/][yellow]╔═╗[/][lime]╔╦╗[/][green]╔═╗[/][cyan]╔╗╔[/][blue]╦[/][purple]╔═╗[/]  [red]╔═╗[/][orange1]╔═╗[/][yellow]╦╔═[/][lime]╔═╗[/][green]╦═╗[/]",
            "[red] ║ [/][orange1]║[/][yellow]║╣ [/][lime]║║║[/][green]║╣ [/][cyan]║║║[/][blue]║[/][purple]╚═╗[/]  [red]╠═╝[/][orange1]║ ║[/][yellow]╠╩╗[/][lime]║╣ [/][green]╠╦╝[/]",
            "[red] ╩ [/][orange1]╩[/][yellow]╚═╝[/][lime]╩ ╩[/][green]╚═╝[/][cyan]╝╚╝[/][blue]╩[/][purple]╚═╝[/]  [red]╩  [/][orange1]╚═╝[/][yellow]╩ ╩[/][lime]╚═╝[/][green]╩╚═[/]",
            "",
            "[red]╔╦╗[/][orange1]╔═╗[/][yellow]═╗╔[/][lime]╔═╗[/][green]╔═╗[/]  [cyan]╦ ╦[/][blue]╔═╗[/][purple]╦  [/][magenta]╔╦╗[/][hotpink]╦[/][red]╔═╗[/][orange1]╔╦╗[/]",
            "[red] ║ [/][orange1]║╣ [/][yellow]╔╩╦╝[/][lime]╠═╣[/][green]╚═╗[/]  [cyan]╠═╣[/][blue]║ ║[/][purple]║  [/][magenta] ║║[/][hotpink]║[/][red]║╣ [/][orange1]║║║[/]",
            "[red] ╩ [/][orange1]╚═╝[/][yellow]╩ ╚═[/][lime]╩ ╩[/][green]╚═╝[/]  [cyan]╩ ╩[/][blue]╚═╝[/][purple]╩═╝[/][magenta]═╩╝[/][hotpink]╩[/][red]╚═╝[/][orange1]╩ ╩[/]"
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
