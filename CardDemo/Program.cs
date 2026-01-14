using Spectre.Console;
using TexasHoldem.CLI;
using TexasHoldem.Domain;
using TexasHoldem.Domain.Enums;

// =============================================
// DEMO: Spectre.Console Poker UI met ASCII kaarten
// =============================================

AnsiConsole.Clear();

// Header
var header = new FigletText("Poker Demo")
    .Color(Color.Green)
    .Centered();
AnsiConsole.Write(header);

AnsiConsole.MarkupLine("[dim]Demonstratie van ASCII kaarten met Spectre.Console Tables en Panels[/]");
AnsiConsole.WriteLine();

// Wait for user
AnsiConsole.MarkupLine("[grey]Druk op een toets om te beginnen...[/]");
Console.ReadKey(true);

// =============================================
// DEMO 1: ASCII Cards in Panel
// =============================================
AnsiConsole.Clear();
AnsiConsole.Write(new Rule("[yellow]Demo 1: ASCII Kaarten in Panel[/]").RuleStyle("yellow"));
AnsiConsole.WriteLine();

var demoCards = new List<Card>
{
    new(Suit.Hearts, Rank.Ace),
    new(Suit.Diamonds, Rank.King),
    new(Suit.Spades, Rank.Queen),
    new(Suit.Clubs, Rank.Jack),
    new(Suit.Hearts, Rank.Ten)
};

var cardsPanel = SpectreCardRenderer.CreateAsciiCardsPanel(
    demoCards.Cast<Card?>(),
    "Community Cards"
);
AnsiConsole.Write(cardsPanel);

AnsiConsole.WriteLine();
AnsiConsole.MarkupLine("[grey]Druk op een toets voor de volgende demo...[/]");
Console.ReadKey(true);

// =============================================
// DEMO 2: Cards in Table format
// =============================================
AnsiConsole.Clear();
AnsiConsole.Write(new Rule("[yellow]Demo 2: Kaarten als Panels in Table[/]").RuleStyle("yellow"));
AnsiConsole.WriteLine();

var cardsTable = SpectreCardRenderer.CreateCardsTable(demoCards.Cast<Card?>(), "Hand");
AnsiConsole.Write(cardsTable);

AnsiConsole.WriteLine();
AnsiConsole.MarkupLine("[grey]Druk op een toets voor de volgende demo...[/]");
Console.ReadKey(true);

// =============================================
// DEMO 3: Mini ASCII Cards
// =============================================
AnsiConsole.Clear();
AnsiConsole.Write(new Rule("[yellow]Demo 3: Compacte Mini Kaarten[/]").RuleStyle("yellow"));
AnsiConsole.WriteLine();

var miniLines = SpectreCardRenderer.CombineMiniCardsWithMarkup(demoCards.Cast<Card?>());
foreach (var line in miniLines)
{
    AnsiConsole.MarkupLine($"  {line}");
}

AnsiConsole.WriteLine();
AnsiConsole.WriteLine();

// Also show compact text version
AnsiConsole.MarkupLine("[dim]Compact text formaat:[/]");
AnsiConsole.MarkupLine($"  {SpectreCardRenderer.CreateCompactCardsMarkup(demoCards)}");

AnsiConsole.WriteLine();
AnsiConsole.MarkupLine("[grey]Druk op een toets voor de volgende demo...[/]");
Console.ReadKey(true);

// =============================================
// DEMO 4: Hidden/Empty Cards
// =============================================
AnsiConsole.Clear();
AnsiConsole.Write(new Rule("[yellow]Demo 4: Verborgen en Lege Kaarten[/]").RuleStyle("yellow"));
AnsiConsole.WriteLine();

var mixedCards = new List<Card?>
{
    new Card(Suit.Hearts, Rank.Ace),
    new Card(Suit.Diamonds, Rank.King),
    null, // Empty slot
    null, // Empty slot
    null  // Empty slot
};

AnsiConsole.MarkupLine("[dim]Flop fase (3 kaarten zichtbaar, 2 nog niet gedeeld):[/]");
var flopPanel = SpectreCardRenderer.CreateAsciiCardsPanel(mixedCards, "FLOP", showHiddenAsBack: false);
AnsiConsole.Write(flopPanel);

AnsiConsole.WriteLine();

AnsiConsole.MarkupLine("[dim]Met verborgen kaarten (face-down):[/]");
var hiddenPanel = SpectreCardRenderer.CreateAsciiCardsPanel(mixedCards, "FLOP", showHiddenAsBack: true);
AnsiConsole.Write(hiddenPanel);

AnsiConsole.WriteLine();
AnsiConsole.MarkupLine("[grey]Druk op een toets voor de volgende demo...[/]");
Console.ReadKey(true);

// =============================================
// DEMO 5: Full Table Layout
// =============================================
AnsiConsole.Clear();
AnsiConsole.Write(new Rule("[yellow]Demo 5: Volledige Poker Tafel[/]").RuleStyle("yellow"));
AnsiConsole.WriteLine();

// Main table panel
var communityCards = new List<Card?>
{
    new Card(Suit.Spades, Rank.Ten),
    new Card(Suit.Clubs, Rank.Jack),
    new Card(Suit.Hearts, Rank.Queen),
    null,
    null
};

var communityMarkup = SpectreCardRenderer.CombineCardsHorizontallyWithMarkup(communityCards);

var tableContent = new Rows(
    new Markup("[bold cyan]TURN[/]").Centered(),
    new Text(""),
    new Markup(string.Join("\n", communityMarkup)).Centered(),
    new Text(""),
    new Rule("[dim]Pot[/]").RuleStyle("dim"),
    new Markup("[bold green]$1,250[/]").Centered(),
    new Text(""),
    new Markup("[dim]Dealer:[/] [yellow]Bot 1[/]  [dim]SB:[/] [cyan]Bot 2[/]  [dim]BB:[/] [magenta]You[/]").Centered()
);

var mainPanel = new Panel(tableContent)
    .Header("[bold white on darkgreen] POKER TABLE [/]")
    .Border(BoxBorder.Double)
    .BorderColor(Color.DarkGreen)
    .Padding(2, 1)
    .Expand();

AnsiConsole.Write(mainPanel);

AnsiConsole.WriteLine();

// Players table
var playersTable = new Table()
    .Border(TableBorder.Rounded)
    .BorderColor(Color.Grey)
    .Title("[bold]Players[/]")
    .AddColumn(new TableColumn("[bold]Seat[/]").Centered())
    .AddColumn(new TableColumn("[bold]Player[/]").LeftAligned())
    .AddColumn(new TableColumn("[bold]Chips[/]").RightAligned())
    .AddColumn(new TableColumn("[bold]Bet[/]").RightAligned())
    .AddColumn(new TableColumn("[bold]Status[/]").Centered())
    .AddColumn(new TableColumn("[bold]Position[/]").Centered());

playersTable.AddRow("[bold yellow]>1<[/]", "[bold yellow]You[/]", "[green]$2,500[/]", "[cyan]$50[/]", "[bold yellow]ACTING[/]", "[bold white on magenta] BB [/]");
playersTable.AddRow("2", "Aggressive Andy", "[green]$1,800[/]", "[cyan]$50[/]", "[green]ACTED[/]", "[bold white on blue] D [/]");
playersTable.AddRow("3", "Cautious Carol", "[green]$3,200[/]", "[dim]-[/]", "[red]FOLDED[/]", "[dim]-[/]");
playersTable.AddRow("4", "Bluffing Bob", "[green]$950[/]", "[cyan]$100[/]", "[green]ACTED[/]", "[bold white on cyan] SB [/]");

AnsiConsole.Write(playersTable);

AnsiConsole.WriteLine();

// Your hole cards
var holeCards = new List<Card>
{
    new(Suit.Hearts, Rank.Ace),
    new(Suit.Diamonds, Rank.King)
};

var holeCardsMarkup = SpectreCardRenderer.CombineCardsHorizontallyWithMarkup(holeCards.Cast<Card?>());

var holeCardsPanel = new Panel(new Rows(
    new Markup(string.Join("\n", holeCardsMarkup)).Centered(),
    new Text(""),
    new Rule("[dim]Best Hand[/]").RuleStyle("dim"),
    new Markup("[bold cyan]Straight (10 to Ace)[/]").Centered()
))
.Header("[bold green] Your Cards [/]")
.Border(BoxBorder.Rounded)
.BorderColor(Color.Green)
.Padding(1, 0);

AnsiConsole.Write(holeCardsPanel);

AnsiConsole.WriteLine();
AnsiConsole.MarkupLine("[grey]Druk op een toets voor de volgende demo...[/]");
Console.ReadKey(true);

// =============================================
// DEMO 6: Action Prompt
// =============================================
AnsiConsole.Clear();
AnsiConsole.Write(new Rule("[yellow]Demo 6: Actie Menu[/]").RuleStyle("yellow"));
AnsiConsole.WriteLine();

var actionTable = new Table()
    .Border(TableBorder.Rounded)
    .BorderColor(Color.Yellow)
    .Title("[bold yellow]Your Turn[/]")
    .AddColumn(new TableColumn("[bold]Action[/]"))
    .AddColumn(new TableColumn("[bold]Key[/]").Centered())
    .AddColumn(new TableColumn("[bold]Amount[/]"));

actionTable.AddRow("[white]Fold[/]", "[bold]F[/]", "[dim]-[/]");
actionTable.AddRow("[white]Call[/]", "[bold]C[/]", "[cyan]$50[/]");
actionTable.AddRow("[white]Raise[/]", "[bold]R[/]", "[green]Min $100[/]");
actionTable.AddRow("[bold magenta]All-In[/]", "[bold]A[/]", "[magenta]$2,500[/]");

AnsiConsole.Write(actionTable);

AnsiConsole.WriteLine();
AnsiConsole.MarkupLine("[grey]Druk op een toets voor de volgende demo...[/]");
Console.ReadKey(true);

// =============================================
// DEMO 7: Winner Display
// =============================================
AnsiConsole.Clear();
AnsiConsole.Write(new Rule("[yellow]Demo 7: Winnaar Weergave[/]").RuleStyle("yellow"));
AnsiConsole.WriteLine();

var winnerPanel = new Panel(new Rows(
    new Markup("[bold green]You[/] wins [bold yellow]$1,250[/] from Main Pot"),
    new Markup("[dim]with[/] [cyan]Straight (10 to Ace)[/]"),
    new Text(""),
    new Markup($"[dim]Winning hand:[/] {SpectreCardRenderer.CreateCompactCardsMarkup(new[] {
        new Card(Suit.Spades, Rank.Ten),
        new Card(Suit.Clubs, Rank.Jack),
        new Card(Suit.Hearts, Rank.Queen),
        new Card(Suit.Diamonds, Rank.King),
        new Card(Suit.Hearts, Rank.Ace)
    })}")
))
.Header("[bold yellow] WINNER [/]")
.Border(BoxBorder.Double)
.BorderColor(Color.Gold1)
.Padding(2, 1);

AnsiConsole.Write(winnerPanel);

AnsiConsole.WriteLine();
AnsiConsole.MarkupLine("[grey]Druk op een toets voor de hand rankings...[/]");
Console.ReadKey(true);

// =============================================
// DEMO 8: Hand Rankings
// =============================================
AnsiConsole.Clear();
AnsiConsole.Write(new Rule("[yellow]Demo 8: Hand Rankings[/]").RuleStyle("yellow"));
AnsiConsole.WriteLine();

var rankingsTable = new Table()
    .Border(TableBorder.Rounded)
    .Title("[bold yellow]Poker Hand Rankings[/]")
    .AddColumn(new TableColumn("[bold]#[/]").Centered())
    .AddColumn("[bold]Hand[/]")
    .AddColumn("[bold]Example[/]");

rankingsTable.AddRow("1", "[bold]Royal Flush[/]", "[red]A♥ K♥ Q♥ J♥ 10♥[/]");
rankingsTable.AddRow("2", "[bold]Straight Flush[/]", "[red]9♥ 8♥ 7♥ 6♥ 5♥[/]");
rankingsTable.AddRow("3", "[bold]Four of a Kind[/]", "[blue]A♠[/] [red]A♥ A♦[/] [blue]A♣ K♠[/]");
rankingsTable.AddRow("4", "[bold]Full House[/]", "[blue]K♠[/] [red]K♥ K♦[/] [blue]Q♠[/] [red]Q♥[/]");
rankingsTable.AddRow("5", "[bold]Flush[/]", "[blue]A♠ J♠ 9♠ 6♠ 4♠[/]");
rankingsTable.AddRow("6", "[bold]Straight[/]", "[blue]10♠[/] [red]9♥ 8♦[/] [blue]7♣ 6♠[/]");
rankingsTable.AddRow("7", "[bold]Three of a Kind[/]", "[blue]Q♠[/] [red]Q♥ Q♦[/] [blue]J♠[/] [red]9♥[/]");
rankingsTable.AddRow("8", "[bold]Two Pair[/]", "[blue]J♠[/] [red]J♥ 8♦[/] [blue]8♣ A♠[/]");
rankingsTable.AddRow("9", "[bold]One Pair[/]", "[blue]10♠[/] [red]10♥ A♦[/] [blue]5♣ 4♠[/]");
rankingsTable.AddRow("10", "[bold]High Card[/]", "[blue]A♠[/] [red]K♥ Q♦[/] [blue]J♣ 9♠[/]");

AnsiConsole.Write(rankingsTable);

AnsiConsole.WriteLine();
AnsiConsole.WriteLine();
AnsiConsole.Write(new Rule("[green]Demo Compleet![/]").RuleStyle("green"));
AnsiConsole.MarkupLine(@"
[bold]Samenvatting:[/]

De volgende bestanden zijn aangemaakt:
  - [cyan]CLI/SpectreCardRenderer.cs[/] - ASCII kaarten met Spectre markup
  - [cyan]CLI/SpectreGameUI.cs[/] - Complete game UI met tables en panels

[dim]Druk op een toets om af te sluiten...[/]
");

Console.ReadKey(true);
