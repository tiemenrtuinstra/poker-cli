using Spectre.Console;

namespace TexasHoldem.CLI;

/// <summary>
/// Provides consistent header rendering across all screens
/// </summary>
public static class HeaderHelper
{
    /// <summary>
    /// Displays the main "Poker Texas Hold'em" header with playing cards
    /// </summary>
    public static void DisplayMainHeader()
    {
        AnsiConsole.WriteLine();

        // Playing cards decoration
        var cardsArt = string.Join("\n",
            "[red]┌─────┐ ┌─────┐[/] [blue]┌─────┐ ┌─────┐[/]",
            "[red]│A    │ │K    │[/] [blue]│Q    │ │J    │[/]",
            "[red]│  ♥  │ │  ♦  │[/] [blue]│  ♠  │ │  ♣  │[/]",
            "[red]│    A│ │    K│[/] [blue]│    Q│ │    J│[/]",
            "[red]└─────┘ └─────┘[/] [blue]└─────┘ └─────┘[/]"
        );

        AnsiConsole.Write(Align.Center(new Markup(cardsArt)));
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine();

        // Main title - "POKER TEXAS HOLD'EM" in readable ASCII art
        var titleArt = string.Join("\n",
            "[green]██████╗  ██████╗ ██╗  ██╗███████╗██████╗ [/]",
            "[green]██╔══██╗██╔═══██╗██║ ██╔╝██╔════╝██╔══██╗[/]",
            "[green]██████╔╝██║   ██║█████╔╝ █████╗  ██████╔╝[/]",
            "[green]██╔═══╝ ██║   ██║██╔═██╗ ██╔══╝  ██╔══██╗[/]",
            "[green]██║     ╚██████╔╝██║  ██╗███████╗██║  ██║[/]",
            "[green]╚═╝      ╚═════╝ ╚═╝  ╚═╝╚══════╝╚═╝  ╚═╝[/]"
        );

        AnsiConsole.Write(Align.Center(new Markup(titleArt)));
        AnsiConsole.WriteLine();

        var subtitleArt = string.Join("\n",
            "[yellow]╔╦╗╔═╗═╗ ╦╔═╗╔═╗  ╦ ╦╔═╗╦  ╔╦╗ ╔═╗╔╦╗[/]",
            "[yellow] ║ ║╣ ╔╩╦╝╠═╣╚═╗  ╠═╣║ ║║   ║║ ║╣ ║║║[/]",
            "[yellow] ╩ ╚═╝╩ ╚═╩ ╩╚═╝  ╩ ╩╚═╝╩═╝═╩╝ ╚═╝╩ ╩[/]"
        );

        AnsiConsole.Write(Align.Center(new Markup(subtitleArt)));
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Displays a compact version of the header (for game screens)
    /// </summary>
    public static void DisplayCompactHeader()
    {
        var compactTitle = string.Join("\n",
            "[green]╔═╗╔═╗╦╔═╔═╗╦═╗[/]  [yellow]╔╦╗╔═╗═╗ ╦╔═╗╔═╗  ╦ ╦╔═╗╦  ╔╦╗ ╔═╗╔╦╗[/]",
            "[green]╠═╝║ ║╠╩╗║╣ ╠╦╝[/]  [yellow] ║ ║╣ ╔╩╦╝╠═╣╚═╗  ╠═╣║ ║║   ║║ ║╣ ║║║[/]",
            "[green]╩  ╚═╝╩ ╩╚═╝╩╚═[/]  [yellow] ╩ ╚═╝╩ ╚═╩ ╩╚═╝  ╩ ╩╚═╝╩═╝═╩╝ ╚═╝╩ ╩[/]"
        );

        AnsiConsole.Write(Align.Center(new Markup(compactTitle)));
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Displays a sub-page header with the main title and a sub-section name
    /// </summary>
    public static void DisplaySubHeader(string subSection, Color color)
    {
        DisplayCompactHeader();
        AnsiConsole.WriteLine();

        var subHeader = new FigletText(subSection)
            .Color(color)
            .Centered();
        AnsiConsole.Write(subHeader);
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Displays a minimal header with just the sub-section (for nested pages)
    /// </summary>
    public static void DisplayMinimalHeader(string title, Color color)
    {
        var header = new FigletText(title)
            .Color(color)
            .Centered();
        AnsiConsole.Write(header);
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Displays the subtitle decoration
    /// </summary>
    public static void DisplaySubtitle(string text)
    {
        AnsiConsole.Write(Align.Center(new Markup($"[bold yellow]♠ ♥ ♦ ♣[/]  [italic]{text}[/]  [bold yellow]♣ ♦ ♥ ♠[/]")));
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine();
    }
}
