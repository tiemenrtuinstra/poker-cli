using Spectre.Console;

namespace TexasHoldem.CLI;

/// <summary>
/// Provides consistent header rendering across all screens
/// </summary>
public static class HeaderHelper
{
    // Playing cards decoration - full size
    private static readonly string CardsArt = string.Join("\n",
        "[red]┌─────┐ ┌─────┐[/] [blue]┌─────┐ ┌─────┐[/]",
        "[red]│A    │ │K    │[/] [blue]│Q    │ │J    │[/]",
        "[red]│  ♥  │ │  ♦  │[/] [blue]│  ♠  │ │  ♣  │[/]",
        "[red]│    A│ │    K│[/] [blue]│    Q│ │    J│[/]",
        "[red]└─────┘ └─────┘[/] [blue]└─────┘ └─────┘[/]"
    );

    // Mini cards for compact headers
    private static readonly string MiniCardsArt = "[red]♥ ♦[/] [blue]♠ ♣[/]";

    /// <summary>
    /// Displays the main "Poker Texas Hold'em" header with playing cards
    /// </summary>
    public static void DisplayMainHeader()
    {
        AnsiConsole.WriteLine();

        // Playing cards decoration
        AnsiConsole.Write(Align.Center(new Markup(CardsArt)));
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine();

        // Main title - "POKER" in big ASCII art
        var pokerArt = string.Join("\n",
            "[green]██████╗  ██████╗ ██╗  ██╗███████╗██████╗ [/]",
            "[green]██╔══██╗██╔═══██╗██║ ██╔╝██╔════╝██╔══██╗[/]",
            "[green]██████╔╝██║   ██║█████╔╝ █████╗  ██████╔╝[/]",
            "[green]██╔═══╝ ██║   ██║██╔═██╗ ██╔══╝  ██╔══██╗[/]",
            "[green]██║     ╚██████╔╝██║  ██╗███████╗██║  ██║[/]",
            "[green]╚═╝      ╚═════╝ ╚═╝  ╚═╝╚══════╝╚═╝  ╚═╝[/]"
        );

        AnsiConsole.Write(Align.Center(new Markup(pokerArt)));
        AnsiConsole.WriteLine();

        // Subtitle - "TEXAS HOLD'EM" with apostrophe
        var texasHoldemArt = string.Join("\n",
            "[yellow]╔╦╗╔═╗═╗ ╦╔═╗╔═╗  ╦ ╦╔═╗╦  ╔╦╗[/][white]'[/][yellow]╔═╗╔╦╗[/]",
            "[yellow] ║ ║╣ ╔╩╦╝╠═╣╚═╗  ╠═╣║ ║║   ║║ ║╣ ║║║[/]",
            "[yellow] ╩ ╚═╝╩ ╚═╩ ╩╚═╝  ╩ ╩╚═╝╩═╝═╩╝ ╚═╝╩ ╩[/]"
        );

        AnsiConsole.Write(Align.Center(new Markup(texasHoldemArt)));
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Displays a compact version of the header (for game screens)
    /// </summary>
    public static void DisplayCompactHeader()
    {
        // Mini cards + compact title
        AnsiConsole.Write(Align.Center(new Markup(MiniCardsArt)));
        AnsiConsole.WriteLine();

        var compactTitle = string.Join("\n",
            "[green]╔═╗╔═╗╦╔═╔═╗╦═╗[/]  [yellow]╔╦╗╔═╗═╗ ╦╔═╗╔═╗  ╦ ╦╔═╗╦  ╔╦╗[/][white]'[/][yellow]╔═╗╔╦╗[/]",
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
        // Mini cards + compact title
        AnsiConsole.Write(Align.Center(new Markup(MiniCardsArt)));
        AnsiConsole.WriteLine();

        var compactTitle = string.Join("\n",
            "[green]╔═╗╔═╗╦╔═╔═╗╦═╗[/]  [yellow]╔╦╗╔═╗═╗ ╦╔═╗╔═╗  ╦ ╦╔═╗╦  ╔╦╗[/][white]'[/][yellow]╔═╗╔╦╗[/]",
            "[green]╠═╝║ ║╠╩╗║╣ ╠╦╝[/]  [yellow] ║ ║╣ ╔╩╦╝╠═╣╚═╗  ╠═╣║ ║║   ║║ ║╣ ║║║[/]",
            "[green]╩  ╚═╝╩ ╩╚═╝╩╚═[/]  [yellow] ╩ ╚═╝╩ ╚═╩ ╩╚═╝  ╩ ╩╚═╝╩═╝═╩╝ ╚═╝╩ ╩[/]"
        );

        AnsiConsole.Write(Align.Center(new Markup(compactTitle)));
        AnsiConsole.WriteLine();
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
