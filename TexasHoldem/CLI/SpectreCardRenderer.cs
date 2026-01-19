using Spectre.Console;
using Spectre.Console.Rendering;
using TexasHoldem.Game;
using TexasHoldem.Game.Enums;

namespace TexasHoldem.CLI;

/// <summary>
/// Renders playing cards using Spectre.Console with ASCII art
/// </summary>
public static class SpectreCardRenderer
{
    /// <summary>
    /// Get Spectre markup color for a suit
    /// </summary>
    public static string GetSuitColor(Suit suit) => suit switch
    {
        Suit.Hearts or Suit.Diamonds => "red",
        Suit.Clubs or Suit.Spades => "blue",
        _ => "white"
    };

    /// <summary>
    /// Get colored markup string for a card's display (e.g., "[red]A♥[/]")
    /// </summary>
    public static string GetColoredCardMarkup(Card card)
    {
        var color = GetSuitColor(card.Suit);
        return $"[{color}]{card.GetDisplayString()}[/]";
    }

    /// <summary>
    /// Create a Panel containing a single ASCII card
    /// </summary>
    public static Panel CreateCardPanel(Card card)
    {
        var color = GetSuitColor(card.Suit);
        var rank = card.GetRankSymbol();
        var suit = card.GetSuitSymbol();

        var content = rank == "10"
            ? $"[{color}]10[/]\n[{color}]{suit}[/]"
            : $"[{color}]{rank}[/]\n[{color}]{suit}[/]";

        return new Panel(new Markup(content))
            .Border(BoxBorder.Rounded)
            .Padding(1, 0)
            .BorderColor(Color.Grey);
    }

    /// <summary>
    /// Create a Panel for a face-down card
    /// </summary>
    public static Panel CreateHiddenCardPanel()
    {
        return new Panel(new Markup("[blue]?[/]\n[blue]?[/]"))
            .Border(BoxBorder.Rounded)
            .Padding(1, 0)
            .BorderColor(Color.Blue);
    }

    /// <summary>
    /// Create a Panel for an empty card slot
    /// </summary>
    public static Panel CreateEmptySlotPanel()
    {
        return new Panel(new Markup("[grey] [/]\n[grey] [/]"))
            .Border(BoxBorder.Rounded)
            .Padding(1, 0)
            .BorderColor(Color.Grey37);
    }

    /// <summary>
    /// Get ASCII art lines for a card with Spectre markup
    /// </summary>
    public static string[] GetAsciiArtWithMarkup(Card card)
    {
        var color = GetSuitColor(card.Suit);
        var rank = card.GetRankSymbol();
        var suit = card.GetSuitSymbol();

        if (rank == "10")
        {
            return
            [
                "[grey]┌─────┐[/]",
                $"[grey]│[/][{color}]10[/][grey]   │[/]",
                $"[grey]│[/]  [{color}]{suit}[/]  [grey]│[/]",
                $"[grey]│[/]   [{color}]10[/][grey]│[/]",
                "[grey]└─────┘[/]"
            ];
        }

        return
        [
            "[grey]┌─────┐[/]",
            $"[grey]│[/][{color}]{rank}[/][grey]    │[/]",
            $"[grey]│[/]  [{color}]{suit}[/]  [grey]│[/]",
            $"[grey]│[/]    [{color}]{rank}[/][grey]│[/]",
            "[grey]└─────┘[/]"
        ];
    }

    /// <summary>
    /// Get ASCII art for a hidden card with markup
    /// </summary>
    public static string[] GetHiddenCardAsciiWithMarkup()
    {
        return
        [
            "[blue]┌─────┐[/]",
            "[blue]│░░░░░│[/]",
            "[blue]│░░░░░│[/]",
            "[blue]│░░░░░│[/]",
            "[blue]└─────┘[/]"
        ];
    }

    /// <summary>
    /// Get ASCII art for an empty slot with markup
    /// </summary>
    public static string[] GetEmptySlotAsciiWithMarkup()
    {
        return
        [
            "[grey37]┌─────┐[/]",
            "[grey37]│     │[/]",
            "[grey37]│     │[/]",
            "[grey37]│     │[/]",
            "[grey37]└─────┘[/]"
        ];
    }

    /// <summary>
    /// Combine multiple cards horizontally into markup lines
    /// </summary>
    public static string[] CombineCardsHorizontallyWithMarkup(IEnumerable<Card?> cards, bool showHiddenAsBack = false)
    {
        var cardArts = new List<string[]>();

        foreach (var card in cards)
        {
            if (card == null)
            {
                cardArts.Add(showHiddenAsBack ? GetHiddenCardAsciiWithMarkup() : GetEmptySlotAsciiWithMarkup());
            }
            else
            {
                cardArts.Add(GetAsciiArtWithMarkup(card));
            }
        }

        if (cardArts.Count == 0)
            return [];

        var result = new string[5];
        for (int line = 0; line < 5; line++)
        {
            result[line] = string.Join(" ", cardArts.Select(art => art[line]));
        }

        return result;
    }

    /// <summary>
    /// Create a Table containing cards displayed horizontally
    /// </summary>
    public static Table CreateCardsTable(IEnumerable<Card?> cards, string? title = null, bool showHiddenAsBack = false)
    {
        var cardList = cards.ToList();
        var table = new Table()
            .Border(TableBorder.None)
            .HideHeaders();

        // Add a column for each card
        foreach (var _ in cardList)
        {
            table.AddColumn(new TableColumn("").Centered());
        }

        // Create panels for each card
        var panels = new List<IRenderable>();
        foreach (var card in cardList)
        {
            if (card == null)
            {
                panels.Add(showHiddenAsBack ? CreateHiddenCardPanel() : CreateEmptySlotPanel());
            }
            else
            {
                panels.Add(CreateCardPanel(card));
            }
        }

        if (panels.Count > 0)
        {
            table.AddRow(panels.ToArray());
        }

        return table;
    }

    /// <summary>
    /// Create a Grid with ASCII art cards (more detailed view)
    /// </summary>
    public static IRenderable CreateAsciiCardsGrid(IEnumerable<Card?> cards, bool showHiddenAsBack = false)
    {
        var lines = CombineCardsHorizontallyWithMarkup(cards, showHiddenAsBack);
        if (lines.Length == 0)
            return new Text("");

        var markup = string.Join("\n", lines);
        return new Markup(markup);
    }

    /// <summary>
    /// Create a Panel containing ASCII art cards with a title
    /// </summary>
    public static Panel CreateAsciiCardsPanel(IEnumerable<Card?> cards, string title, bool showHiddenAsBack = false)
    {
        var grid = CreateAsciiCardsGrid(cards, showHiddenAsBack);

        return new Panel(grid)
            .Header($"[bold yellow]{title}[/]")
            .Border(BoxBorder.Rounded)
            .Padding(1, 0)
            .BorderColor(Color.Yellow);
    }

    /// <summary>
    /// Create compact card display (single line)
    /// </summary>
    public static string CreateCompactCardsMarkup(IEnumerable<Card> cards)
    {
        return string.Join("  ", cards.Select(GetColoredCardMarkup));
    }

    /// <summary>
    /// Create mini ASCII cards (3 lines)
    /// </summary>
    public static string[] GetMiniAsciiWithMarkup(Card card)
    {
        var color = GetSuitColor(card.Suit);
        var display = card.GetDisplayString();

        return
        [
            "[grey]┌───┐[/]",
            $"[grey]│[/][{color}]{display,-3}[/][grey]│[/]",
            "[grey]└───┘[/]"
        ];
    }

    /// <summary>
    /// Combine mini cards horizontally
    /// </summary>
    public static string[] CombineMiniCardsWithMarkup(IEnumerable<Card?> cards, bool showHiddenAsBack = false)
    {
        var cardArts = new List<string[]>();

        foreach (var card in cards)
        {
            if (card == null)
            {
                cardArts.Add(showHiddenAsBack
                    ? ["[blue]┌───┐[/]", "[blue]│░░░│[/]", "[blue]└───┘[/]"]
                    : ["[grey37]┌───┐[/]", "[grey37]│   │[/]", "[grey37]└───┘[/]"]);
            }
            else
            {
                cardArts.Add(GetMiniAsciiWithMarkup(card));
            }
        }

        if (cardArts.Count == 0)
            return [];

        var result = new string[3];
        for (int line = 0; line < 3; line++)
        {
            result[line] = string.Join(" ", cardArts.Select(art => art[line]));
        }

        return result;
    }
}
