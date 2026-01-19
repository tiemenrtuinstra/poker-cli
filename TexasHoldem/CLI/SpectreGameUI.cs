using Spectre.Console;
using Spectre.Console.Rendering;
using TexasHoldem.Game;
using TexasHoldem.Game.Enums;
using TexasHoldem.Players;

namespace TexasHoldem.CLI;

/// <summary>
/// Game UI using Spectre.Console for beautiful terminal output
/// </summary>
public class SpectreGameUI : IGameUI
{
    /// <summary>
    /// Display the full poker table with all elements
    /// </summary>
    public void DisplayPokerTable(GameState gameState, IPlayer? currentPlayer = null)
    {
        AnsiConsole.Clear();

        // Header
        var header = new FigletText("Texas Hold'em")
            .Color(Color.Green)
            .Centered();
        AnsiConsole.Write(header);

        // Main table layout
        var layout = new Layout("Root")
            .SplitRows(
                new Layout("Top"),
                new Layout("Middle"),
                new Layout("Bottom")
            );

        // Build and display the table
        DisplayTablePanel(gameState);

        AnsiConsole.WriteLine();

        // Player info table
        DisplayPlayersTable(gameState);

        AnsiConsole.WriteLine();

        // Current player's hole cards (if applicable)
        if (currentPlayer != null && currentPlayer.HoleCards.Any())
        {
            DisplayHoleCards(currentPlayer, gameState.CommunityCards);
        }
    }

    /// <summary>
    /// Display the main poker table panel with community cards
    /// </summary>
    public void DisplayTablePanel(GameState gameState)
    {
        // Build community cards (pad to 5)
        var communityCards = new List<Card?>();
        for (int i = 0; i < 5; i++)
        {
            communityCards.Add(i < gameState.CommunityCards.Count ? gameState.CommunityCards[i] : null);
        }

        // Create the ASCII cards
        var cardsMarkup = SpectreCardRenderer.CombineCardsHorizontallyWithMarkup(communityCards, showHiddenAsBack: false);
        var cardsContent = cardsMarkup.Length > 0
            ? string.Join("\n", cardsMarkup)
            : "[grey]Waiting for flop...[/]";

        // Phase display
        var phaseColor = gameState.Phase switch
        {
            GamePhase.PreFlop => "yellow",
            GamePhase.Flop => "cyan",
            GamePhase.Turn => "blue",
            GamePhase.River => "magenta",
            GamePhase.Showdown => "red",
            _ => "white"
        };

        var content = new Rows(
            new Markup($"[bold {phaseColor}]{gameState.Phase.ToString().ToUpper()}[/]").Centered(),
            new Text(""),
            new Markup(cardsContent).Centered(),
            new Text(""),
            new Rule("[dim]Pot[/]").RuleStyle("dim"),
            new Markup($"[bold green]€{gameState.TotalPot}[/]").Centered(),
            new Text(""),
            CreateBlindsInfo(gameState)
        );

        var panel = new Panel(content)
            .Header("[bold white on darkgreen] POKER TABLE [/]")
            .Border(BoxBorder.Double)
            .BorderColor(Color.DarkGreen)
            .Padding(2, 1)
            .Expand();

        AnsiConsole.Write(panel);
    }

    /// <summary>
    /// Create blinds info markup
    /// </summary>
    private IRenderable CreateBlindsInfo(GameState gameState)
    {
        var dealer = gameState.Dealer?.Name ?? "?";
        var sb = gameState.SmallBlindPosition >= 0 && gameState.SmallBlindPosition < gameState.Players.Count
            ? gameState.Players[gameState.SmallBlindPosition].Name
            : "?";
        var bb = gameState.BigBlindPosition >= 0 && gameState.BigBlindPosition < gameState.Players.Count
            ? gameState.Players[gameState.BigBlindPosition].Name
            : "?";

        return new Markup($"[dim]Dealer:[/] [yellow]{dealer}[/]  [dim]SB:[/] [cyan]{sb}[/]  [dim]BB:[/] [magenta]{bb}[/]").Centered();
    }

    /// <summary>
    /// Display players in a table format
    /// </summary>
    public void DisplayPlayersTable(GameState gameState)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .Title("[bold]Players[/]")
            .AddColumn(new TableColumn("[bold]Seat[/]").Centered())
            .AddColumn(new TableColumn("[bold]Player[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold]Chips[/]").RightAligned())
            .AddColumn(new TableColumn("[bold]Bet[/]").RightAligned())
            .AddColumn(new TableColumn("[bold]Status[/]").Centered())
            .AddColumn(new TableColumn("[bold]Position[/]").Centered());

        for (int i = 0; i < gameState.Players.Count; i++)
        {
            var player = gameState.Players[i];
            var isCurrentPlayer = i == gameState.CurrentPlayerPosition;

            // Seat number
            var seat = isCurrentPlayer ? $"[bold yellow]>{i + 1}<[/]" : $"{i + 1}";

            // Player name with styling
            var name = player.Name;
            if (isCurrentPlayer)
            {
                name = $"[bold yellow]{name}[/]";
            }
            else if (!player.IsActive)
            {
                name = $"[strikethrough dim]{name}[/]";
            }

            // Chips
            var chips = player.IsActive ? $"[green]€{player.Chips}[/]" : "[dim]$0[/]";

            // Current bet
            var bet = gameState.GetPlayerBetThisRound(player);
            var betStr = bet > 0 ? $"[cyan]€{bet}[/]" : "[dim]-[/]";

            // Status
            var status = GetStatusMarkup(player, gameState);

            // Position badges
            var position = GetPositionMarkup(i, gameState);

            table.AddRow(seat, name, chips, betStr, status, position);
        }

        AnsiConsole.Write(table);
    }

    /// <summary>
    /// Get player status with color markup
    /// </summary>
    private string GetStatusMarkup(IPlayer player, GameState gameState)
    {
        if (!player.IsActive)
            return "[dim strikethrough]ELIMINATED[/]";
        if (player.HasFolded)
            return "[red]FOLDED[/]";
        if (player.IsAllIn)
            return "[bold magenta]ALL-IN[/]";
        if (gameState.CurrentPlayerPosition == gameState.Players.IndexOf(player))
            return "[bold yellow]ACTING[/]";
        if (gameState.HasPlayerActed(player))
            return "[green]ACTED[/]";

        return "[dim]WAITING[/]";
    }

    /// <summary>
    /// Get position badges markup
    /// </summary>
    private string GetPositionMarkup(int playerIndex, GameState gameState)
    {
        var badges = new List<string>();

        if (playerIndex == gameState.DealerPosition)
            badges.Add("[bold white on blue] D [/]");
        if (playerIndex == gameState.SmallBlindPosition)
            badges.Add("[bold white on cyan] SB [/]");
        if (playerIndex == gameState.BigBlindPosition)
            badges.Add("[bold white on magenta] BB [/]");

        return badges.Count > 0 ? string.Join(" ", badges) : "[dim]-[/]";
    }

    /// <summary>
    /// Display player's hole cards with hand evaluation
    /// </summary>
    public void DisplayHoleCards(IPlayer player, List<Card> communityCards)
    {
        // Create ASCII cards for hole cards
        var cardsMarkup = SpectreCardRenderer.CombineCardsHorizontallyWithMarkup(player.HoleCards.Cast<Card?>());
        var cardsContent = string.Join("\n", cardsMarkup);

        var content = new Rows(
            new Markup(cardsContent).Centered()
        );

        // Add hand evaluation if we have community cards
        if (communityCards.Count >= 3)
        {
            var allCards = player.HoleCards.Concat(communityCards).ToList();
            var handResult = HandEvaluator.EvaluateHand(allCards);

            content = new Rows(
                new Markup(cardsContent).Centered(),
                new Text(""),
                new Rule("[dim]Best Hand[/]").RuleStyle("dim"),
                new Markup($"[bold cyan]{handResult.Description}[/]").Centered()
            );
        }

        var panel = new Panel(content)
            .Header($"[bold green] Your Cards [/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Green)
            .Padding(1, 0);

        AnsiConsole.Write(panel);
    }

    /// <summary>
    /// Display action prompt for human player
    /// </summary>
    public void DisplayActionPrompt(GameState gameState, IPlayer player, int currentBet, int minRaise)
    {
        var callAmount = currentBet - gameState.GetPlayerBetThisRound(player);

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Yellow)
            .Title("[bold yellow]Your Turn[/]")
            .AddColumn(new TableColumn("[bold]Action[/]"))
            .AddColumn(new TableColumn("[bold]Key[/]").Centered())
            .AddColumn(new TableColumn("[bold]Amount[/]"));

        table.AddRow("[white]Fold[/]", "[bold]F[/]", "[dim]-[/]");

        if (callAmount == 0)
        {
            table.AddRow("[white]Check[/]", "[bold]C[/]", "[dim]$0[/]");
        }
        else
        {
            table.AddRow("[white]Call[/]", "[bold]C[/]", $"[cyan]€{callAmount}[/]");
        }

        table.AddRow("[white]Raise[/]", "[bold]R[/]", $"[green]Min €{minRaise}[/]");

        if (player.Chips > 0)
        {
            table.AddRow("[bold magenta]All-In[/]", "[bold]A[/]", $"[magenta]€{player.Chips}[/]");
        }

        AnsiConsole.Write(table);
    }

    /// <summary>
    /// Display winners at showdown
    /// </summary>
    public void DisplayWinners(List<Game.PotWinner> winners)
    {
        AnsiConsole.WriteLine();

        var panel = new Panel(
            new Rows(
                winners.Select(w => new Markup(
                    $"[bold green]{w.Player.Name}[/] wins [bold yellow]€{w.Amount}[/] from {w.PotType}\n" +
                    $"[dim]with[/] [cyan]{w.HandDescription}[/]"
                )).ToArray()
            ))
            .Header("[bold yellow] WINNERS [/]")
            .Border(BoxBorder.Double)
            .BorderColor(Color.Gold1)
            .Padding(2, 1);

        AnsiConsole.Write(panel);
    }

    /// <summary>
    /// Display showdown with all players' cards
    /// </summary>
    public void DisplayShowdown(List<IPlayer> players, List<Card> communityCards)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[bold red]SHOWDOWN[/]").RuleStyle("red"));
        AnsiConsole.WriteLine();

        foreach (var player in players.Where(p => p.IsActive && !p.HasFolded))
        {
            var cardsMarkup = SpectreCardRenderer.CreateCompactCardsMarkup(player.HoleCards);
            var allCards = player.HoleCards.Concat(communityCards).ToList();
            var handResult = HandEvaluator.EvaluateHand(allCards);

            var panel = new Panel(new Rows(
                new Markup(cardsMarkup),
                new Markup($"[cyan]{handResult.Description}[/]")
            ))
            .Header($"[bold]{player.Name}[/]")
            .Border(BoxBorder.Rounded)
            .Padding(1, 0);

            AnsiConsole.Write(panel);
        }
    }

    /// <summary>
    /// Display community cards with a phase label
    /// </summary>
    public void DisplayCommunityCards(List<Card> cards, BettingPhase phase)
    {
        var phaseNames = new Dictionary<BettingPhase, (string name, string color)>
        {
            { BettingPhase.Flop, ("FLOP", "cyan") },
            { BettingPhase.Turn, ("TURN", "blue") },
            { BettingPhase.River, ("RIVER", "magenta") },
            { BettingPhase.Showdown, ("FINAL BOARD", "red") }
        };

        var (phaseName, color) = phaseNames.GetValueOrDefault(phase, ("COMMUNITY CARDS", "white"));

        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule($"[bold {color}]{phaseName}[/]").RuleStyle(color));

        var cardsPanel = SpectreCardRenderer.CreateAsciiCardsPanel(cards.Cast<Card?>(), phaseName, showHiddenAsBack: false);
        AnsiConsole.Write(cardsPanel);
    }

    /// <summary>
    /// Display thinking animation
    /// </summary>
    public void ShowThinkingAnimation(string playerName, int durationMs = 2000)
    {
        AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("yellow"))
            .Start($"[yellow]{playerName} is thinking...[/]", ctx =>
            {
                Thread.Sleep(durationMs);
            });
    }

    /// <summary>
    /// Display dealing animation
    /// </summary>
    public void ShowDealingAnimation(string message)
    {
        AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots2)
            .SpinnerStyle(Style.Parse("green"))
            .Start($"[green]{message}[/]", ctx =>
            {
                Thread.Sleep(1000);
            });
    }

    /// <summary>
    /// Display a progress bar
    /// </summary>
    public void ShowProgressBar(string label, int durationMs = 2000)
    {
        AnsiConsole.Progress()
            .AutoClear(true)
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn()
            )
            .Start(ctx =>
            {
                var task = ctx.AddTask($"[green]{label}[/]");
                while (!ctx.IsFinished)
                {
                    task.Increment(100.0 / (durationMs / 50.0));
                    Thread.Sleep(50);
                }
            });
    }

    /// <summary>
    /// Display game statistics
    /// </summary>
    public void DisplayGameStatistics(Game.GameStatistics stats)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold]Game Statistics[/]")
            .AddColumn("Statistic")
            .AddColumn("Value");

        table.AddRow("Hands Played", stats.HandsPlayed.ToString());
        table.AddRow("Players Remaining", stats.PlayersRemaining.ToString());
        table.AddRow("Current Blinds", stats.CurrentBlinds);

        if (stats.RoundHistory.Any())
        {
            var avgPot = stats.RoundHistory.Average(r => r.TotalPot);
            var maxPot = stats.RoundHistory.Max(r => r.TotalPot);
            table.AddRow("Average Pot", $"€{avgPot:F0}");
            table.AddRow("Largest Pot", $"€{maxPot}");
        }

        AnsiConsole.Write(table);
    }

    /// <summary>
    /// Display hand rankings reference
    /// </summary>
    public void DisplayHandRankings()
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold yellow]Poker Hand Rankings[/]")
            .AddColumn(new TableColumn("[bold]Rank[/]").Centered())
            .AddColumn("[bold]Hand[/]")
            .AddColumn("[bold]Example[/]");

        table.AddRow("1", "[bold]Royal Flush[/]", "[red]A♥[/] [red]K♥[/] [red]Q♥[/] [red]J♥[/] [red]10♥[/]");
        table.AddRow("2", "[bold]Straight Flush[/]", "[red]9♥[/] [red]8♥[/] [red]7♥[/] [red]6♥[/] [red]5♥[/]");
        table.AddRow("3", "[bold]Four of a Kind[/]", "[blue]A♠[/] [red]A♥[/] [red]A♦[/] [blue]A♣[/] [blue]K♠[/]");
        table.AddRow("4", "[bold]Full House[/]", "[blue]K♠[/] [red]K♥[/] [red]K♦[/] [blue]Q♠[/] [red]Q♥[/]");
        table.AddRow("5", "[bold]Flush[/]", "[blue]A♠[/] [blue]J♠[/] [blue]9♠[/] [blue]6♠[/] [blue]4♠[/]");
        table.AddRow("6", "[bold]Straight[/]", "[blue]10♠[/] [red]9♥[/] [red]8♦[/] [blue]7♣[/] [blue]6♠[/]");
        table.AddRow("7", "[bold]Three of a Kind[/]", "[blue]Q♠[/] [red]Q♥[/] [red]Q♦[/] [blue]J♠[/] [red]9♥[/]");
        table.AddRow("8", "[bold]Two Pair[/]", "[blue]J♠[/] [red]J♥[/] [red]8♦[/] [blue]8♣[/] [blue]A♠[/]");
        table.AddRow("9", "[bold]One Pair[/]", "[blue]10♠[/] [red]10♥[/] [red]A♦[/] [blue]5♣[/] [blue]4♠[/]");
        table.AddRow("10", "[bold]High Card[/]", "[blue]A♠[/] [red]K♥[/] [red]Q♦[/] [blue]J♣[/] [blue]9♠[/]");

        AnsiConsole.Write(table);
    }

    /// <summary>
    /// Display a message with style
    /// </summary>
    public void DisplayMessage(string message, string style = "white")
    {
        AnsiConsole.MarkupLine($"[{style}]{message}[/]");
    }

    /// <summary>
    /// Clear the screen
    /// </summary>
    public void ClearScreen()
    {
        AnsiConsole.Clear();
    }

    /// <summary>
    /// Ask for confirmation
    /// </summary>
    public bool Confirm(string message)
    {
        return AnsiConsole.Confirm(message);
    }

    /// <summary>
    /// Ask for a number input
    /// </summary>
    public int AskForNumber(string prompt, int min, int max)
    {
        return AnsiConsole.Prompt(
            new TextPrompt<int>(prompt)
                .Validate(n => n >= min && n <= max
                    ? ValidationResult.Success()
                    : ValidationResult.Error($"[red]Please enter a number between {min} and {max}[/]")));
    }

    /// <summary>
    /// Ask for a selection from options
    /// </summary>
    public string AskForSelection(string prompt, IEnumerable<string> options)
    {
        return AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title(prompt)
                .AddChoices(options));
    }

    /// <summary>
    /// Display action taken by a player
    /// </summary>
    public void DisplayAction(string playerName, ActionType action, int amount = 0)
    {
        var actionStr = action switch
        {
            ActionType.Fold => "[red]FOLDS[/]",
            ActionType.Check => "[cyan]CHECKS[/]",
            ActionType.Call => $"[green]CALLS €{amount}[/]",
            ActionType.Bet => $"[yellow]BETS €{amount}[/]",
            ActionType.Raise => $"[bold yellow]RAISES to €{amount}[/]",
            ActionType.AllIn => $"[bold magenta]ALL-IN €{amount}[/]",
            _ => action.ToString()
        };

        AnsiConsole.MarkupLine($"[bold]{playerName}[/] {actionStr}");
    }

    /// <summary>
    /// Display pot information
    /// </summary>
    public void DisplayPotInfo(int mainPot, List<SidePot>? sidePots = null)
    {
        var rows = new List<IRenderable>
        {
            new Markup($"[bold green]Main Pot: €{mainPot}[/]")
        };

        if (sidePots?.Any() == true)
        {
            foreach (var (pot, index) in sidePots.Select((p, i) => (p, i)))
            {
                rows.Add(new Markup($"[yellow]Side Pot {index + 1}: €{pot.Amount}[/]"));
            }
        }

        var panel = new Panel(new Rows(rows.ToArray()))
            .Header("[bold]Pot[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Green);

        AnsiConsole.Write(panel);
    }

    // ============================================================
    // Compatibility methods for existing GameUI interface
    // ============================================================

    /// <summary>
    /// Draw a separator line
    /// </summary>
    public void DrawSeparator(char character = '=', int length = 60)
    {
        var style = character switch
        {
            '=' => "yellow",
            '-' => "grey",
            '═' => "green",
            _ => "white"
        };
        AnsiConsole.Write(new Rule().RuleStyle(style));
    }

    /// <summary>
    /// Show a colored message (compatibility with ConsoleColor)
    /// </summary>
    public void ShowColoredMessage(string message, ConsoleColor color)
    {
        var spectreColor = color switch
        {
            ConsoleColor.Red => "red",
            ConsoleColor.Green => "green",
            ConsoleColor.Yellow => "yellow",
            ConsoleColor.Blue => "blue",
            ConsoleColor.Cyan => "cyan",
            ConsoleColor.Magenta => "magenta",
            ConsoleColor.White => "white",
            ConsoleColor.Gray => "grey",
            ConsoleColor.DarkGray => "grey",
            ConsoleColor.DarkRed => "maroon",
            ConsoleColor.DarkGreen => "darkgreen",
            ConsoleColor.DarkYellow => "olive",
            ConsoleColor.DarkBlue => "navy",
            ConsoleColor.DarkCyan => "teal",
            ConsoleColor.DarkMagenta => "purple",
            _ => "white"
        };
        AnsiConsole.MarkupLine($"[{spectreColor}]{Markup.Escape(message)}[/]");
    }

    /// <summary>
    /// Show chip movement animation
    /// </summary>
    public void ShowChipAnimation(string from, string to, int amount)
    {
        AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("green"))
            .Start($"[green]Moving chips...[/]", ctx =>
            {
                Thread.Sleep(300);
            });
        AnsiConsole.MarkupLine($"[yellow]$[/] [bold]{from}[/] [dim]───>[/] [bold]{to}[/] [green](€{amount})[/]");
    }

    /// <summary>
    /// Display the visual poker table with players around it
    /// </summary>
    public void DisplayVisualPokerTable(GameState gameState, int? highlightPlayerIndex = null)
    {
        // Build community cards (pad to 5)
        var communityCards = new List<Card?>();
        for (int i = 0; i < 5; i++)
        {
            communityCards.Add(i < gameState.CommunityCards.Count ? gameState.CommunityCards[i] : null);
        }

        // Create the ASCII cards
        var cardsMarkup = SpectreCardRenderer.CombineCardsHorizontallyWithMarkup(communityCards, showHiddenAsBack: false);
        var cardsContent = cardsMarkup.Length > 0
            ? string.Join("\n", cardsMarkup)
            : "[grey]Waiting for flop...[/]";

        // Phase display
        var phaseColor = gameState.Phase switch
        {
            GamePhase.PreFlop => "yellow",
            GamePhase.Flop => "cyan",
            GamePhase.Turn => "blue",
            GamePhase.River => "magenta",
            GamePhase.Showdown => "red",
            _ => "white"
        };

        var content = new Rows(
            new Markup($"[bold {phaseColor}]{gameState.Phase.ToString().ToUpper()}[/]").Centered(),
            new Text(""),
            new Markup(cardsContent).Centered(),
            new Text(""),
            new Rule("[dim]Pot[/]").RuleStyle("dim"),
            new Markup($"[bold green]€{gameState.TotalPot}[/]").Centered(),
            new Text(""),
            CreateBlindsInfo(gameState)
        );

        var panel = new Panel(content)
            .Header("[bold white on darkgreen] POKER TABLE [/]")
            .Border(BoxBorder.Double)
            .BorderColor(Color.DarkGreen)
            .Padding(2, 1)
            .Expand();

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();

        // Display players table with highlight
        DisplayPlayersTableWithHighlight(gameState, highlightPlayerIndex);
    }

    /// <summary>
    /// Display players table with optional highlight
    /// </summary>
    private void DisplayPlayersTableWithHighlight(GameState gameState, int? highlightIndex)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .Title("[bold]Players[/]")
            .AddColumn(new TableColumn("[bold]Seat[/]").Centered())
            .AddColumn(new TableColumn("[bold]Player[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold]Chips[/]").RightAligned())
            .AddColumn(new TableColumn("[bold]Bet[/]").RightAligned())
            .AddColumn(new TableColumn("[bold]Status[/]").Centered())
            .AddColumn(new TableColumn("[bold]Position[/]").Centered());

        for (int i = 0; i < gameState.Players.Count; i++)
        {
            var player = gameState.Players[i];
            var isHighlighted = highlightIndex.HasValue && i == highlightIndex.Value;
            var isCurrentPlayer = i == gameState.CurrentPlayerPosition;

            // Seat number
            var seat = isHighlighted ? $"[bold green]>{i + 1}<[/]" :
                       isCurrentPlayer ? $"[bold yellow]>{i + 1}<[/]" : $"{i + 1}";

            // Player name with styling
            var name = player.Name;
            if (isHighlighted)
            {
                name = $"[bold green]{name}[/]";
            }
            else if (isCurrentPlayer)
            {
                name = $"[bold yellow]{name}[/]";
            }
            else if (!player.IsActive)
            {
                name = $"[strikethrough dim]{name}[/]";
            }

            // Chips
            var chips = player.IsActive ? $"[green]€{player.Chips}[/]" : "[dim]$0[/]";

            // Current bet
            var bet = gameState.GetPlayerBetThisRound(player);
            var betStr = bet > 0 ? $"[cyan]€{bet}[/]" : "[dim]-[/]";

            // Status
            var status = GetStatusMarkup(player, gameState);

            // Position badges
            var position = GetPositionMarkup(i, gameState);

            table.AddRow(seat, name, chips, betStr, status, position);
        }

        AnsiConsole.Write(table);
    }

    /// <summary>
    /// Display player's hole cards with ASCII art
    /// </summary>
    public void DisplayHoleCardsAscii(IPlayer player)
    {
        AnsiConsole.WriteLine();

        var cardsMarkup = SpectreCardRenderer.CombineCardsHorizontallyWithMarkup(player.HoleCards.Cast<Card?>());
        var cardsContent = string.Join("\n", cardsMarkup);

        var panel = new Panel(new Markup(cardsContent).Centered())
            .Header("[bold green] Your Cards [/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Green)
            .Padding(1, 0);

        AnsiConsole.Write(panel);
    }

    /// <summary>
    /// Display community cards with ASCII art
    /// </summary>
    public void DisplayCommunityCardsAscii(List<Card> cards, BettingPhase phase)
    {
        var phaseNames = new Dictionary<BettingPhase, (string name, string color)>
        {
            { BettingPhase.Flop, ("FLOP", "cyan") },
            { BettingPhase.Turn, ("TURN", "blue") },
            { BettingPhase.River, ("RIVER", "magenta") },
            { BettingPhase.Showdown, ("FINAL BOARD", "red") }
        };

        var (phaseName, color) = phaseNames.GetValueOrDefault(phase, ("BOARD", "white"));

        AnsiConsole.WriteLine();

        var cardsMarkup = SpectreCardRenderer.CombineCardsHorizontallyWithMarkup(cards.Cast<Card?>());
        var cardsContent = string.Join("\n", cardsMarkup);

        var panel = new Panel(new Markup(cardsContent).Centered())
            .Header($"[bold {color}] {phaseName} [/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Yellow)
            .Padding(1, 0);

        AnsiConsole.Write(panel);
    }

    /// <summary>
    /// Display pot information box
    /// </summary>
    public void DisplayPotBox(int mainPot, List<SidePot>? sidePots = null)
    {
        var rows = new List<IRenderable>
        {
            new Markup($"[bold green]Main Pot: €{mainPot}[/]")
        };

        if (sidePots?.Any() == true)
        {
            foreach (var (pot, index) in sidePots.Select((p, i) => (p, i)))
            {
                rows.Add(new Markup($"[yellow]Side Pot {index + 1}: €{pot.Amount}[/]"));
            }
        }

        var panel = new Panel(new Rows(rows.ToArray()))
            .Header("[bold yellow] POT [/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Yellow)
            .Padding(1, 0);

        AnsiConsole.Write(panel);
    }

    /// <summary>
    /// Dealing animation with custom duration
    /// </summary>
    public void ShowDealingAnimation(string message, int durationMs)
    {
        AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots2)
            .SpinnerStyle(Style.Parse("green"))
            .Start($"[green]{message}[/]", ctx =>
            {
                Thread.Sleep(durationMs);
            });
    }

    /// <summary>
    /// Display player info box
    /// </summary>
    public void DisplayPlayerInfoBox(IPlayer player, GameState gameState, bool showCards = false)
    {
        var status = GetStatusMarkup(player, gameState);
        var position = GetPositionMarkup(gameState.Players.IndexOf(player), gameState);
        var personality = player.Personality?.ToString() ?? "Human";

        var rows = new List<IRenderable>
        {
            new Markup($"[bold]{player.Name}[/]"),
            new Markup($"[green]€{player.Chips}[/]"),
            new Markup($"[dim]{personality}[/]"),
            new Markup($"{position} {status}")
        };

        if (showCards && player.HoleCards.Any())
        {
            var cardsMarkup = SpectreCardRenderer.CreateCompactCardsMarkup(player.HoleCards);
            rows.Add(new Markup(cardsMarkup));
        }

        var panel = new Panel(new Rows(rows.ToArray()))
            .Border(BoxBorder.Rounded)
            .Padding(1, 0);

        AnsiConsole.Write(panel);
    }

    /// <summary>
    /// Display hand header (HAND #X)
    /// </summary>
    public void DisplayHandHeader(int handNumber, List<IPlayer> players, IPlayer? dealer)
    {
        AnsiConsole.Clear();

        // Hand number in a panel
        var handPanel = new Panel(new Markup($"[bold white]HAND #{handNumber}[/]").Centered())
            .Header("[bold yellow] TEXAS HOLD'EM [/]")
            .Border(BoxBorder.Double)
            .BorderColor(Color.Yellow)
            .Padding(2, 0)
            .Expand();

        AnsiConsole.Write(handPanel);
        AnsiConsole.WriteLine();

        // Players info in a table
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .AddColumn(new TableColumn("[bold]Player[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold]Chips[/]").RightAligned())
            .AddColumn(new TableColumn("[bold]Role[/]").Centered());

        foreach (var player in players.Where(p => p.IsActive))
        {
            var role = player == dealer ? "[bold blue] D [/]" : "";
            table.AddRow(
                $"[white]{player.Name}[/]",
                $"[green]€{player.Chips}[/]",
                role
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Display betting phase header (PRE-FLOP, FLOP, TURN, RIVER)
    /// </summary>
    public void DisplayPhaseHeader(BettingPhase phase)
    {
        var (phaseName, color, emoji) = phase switch
        {
            BettingPhase.PreFlop => ("PRE-FLOP", "yellow", "🃏"),
            BettingPhase.Flop => ("FLOP", "cyan", "🎴"),
            BettingPhase.Turn => ("TURN", "blue", "🎴"),
            BettingPhase.River => ("RIVER", "magenta", "🎴"),
            BettingPhase.Showdown => ("SHOWDOWN", "red", "🏆"),
            _ => ("BETTING", "white", "💰")
        };

        AnsiConsole.WriteLine();
        var rule = new Rule($"[bold {color}]{emoji} {phaseName}[/]")
            .RuleStyle(color)
            .LeftJustified();
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Display blinds posted
    /// </summary>
    public void DisplayBlindsPosted(IPlayer smallBlind, int sbAmount, IPlayer bigBlind, int bbAmount)
    {
        var table = new Table()
            .Border(TableBorder.Simple)
            .BorderColor(Color.Grey)
            .HideHeaders()
            .AddColumn("")
            .AddColumn("")
            .AddColumn("");

        table.AddRow(
            $"[yellow]{smallBlind.Name}[/]",
            "[dim]posts SB[/]",
            $"[green]€{sbAmount}[/]"
        );
        table.AddRow(
            $"[yellow]{bigBlind.Name}[/]",
            "[dim]posts BB[/]",
            $"[green]€{bbAmount}[/]"
        );

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Display player turn header
    /// </summary>
    public void DisplayPlayerTurn(IPlayer player, int chipsRemaining)
    {
        AnsiConsole.MarkupLine($"[bold yellow]▶ {player.Name}[/] [dim]({(player.Personality?.ToString() ?? "Human")})[/] [green]€{chipsRemaining}[/]");
    }

    /// <summary>
    /// Display player action in a nice format
    /// </summary>
    public void DisplayPlayerAction(IPlayer player, ActionType action, int amount = 0, string? message = null)
    {
        var (actionText, color, emoji) = action switch
        {
            ActionType.Fold => ("FOLDS", "red", "❌"),
            ActionType.Check => ("CHECKS", "cyan", "✓"),
            ActionType.Call => ($"CALLS €{amount}", "green", "📞"),
            ActionType.Bet => ($"BETS €{amount}", "yellow", "💰"),
            ActionType.Raise => ($"RAISES TO €{amount}", "orange1", "🚀"),
            ActionType.AllIn => ($"ALL-IN €{amount}", "magenta", "🔥"),
            _ => (action.ToString(), "white", "•")
        };

        var messageText = !string.IsNullOrEmpty(message) ? $" [dim italic]\"{message}\"[/]" : "";

        AnsiConsole.MarkupLine($"   {emoji} [{color}]{player.Name} {actionText}[/]{messageText}");
    }

    /// <summary>
    /// Display betting round complete
    /// </summary>
    public void DisplayBettingComplete(int potAmount)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[dim]───────────────────────────────────[/]");
        AnsiConsole.MarkupLine($"[bold green]💵 Betting complete. Pot: €{potAmount}[/]");
    }

    /// <summary>
    /// Display winner when everyone folds
    /// </summary>
    public void DisplayFoldWinner(IPlayer winner, int amount)
    {
        AnsiConsole.WriteLine();
        var panel = new Panel(new Markup($"[bold green]{winner.Name}[/] wins [bold yellow]€{amount}[/]\n[dim](everyone else folded)[/]").Centered())
            .Header("[bold green] WINNER [/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Green)
            .Padding(1, 0);

        AnsiConsole.Write(panel);
    }

    /// <summary>
    /// Display hand summary
    /// </summary>
    public void DisplayHandSummary(int totalPot, int bettingRounds, List<IPlayer> players)
    {
        AnsiConsole.WriteLine();
        var rule = new Rule("[bold cyan]📊 HAND SUMMARY[/]")
            .RuleStyle("cyan");
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();

        // Summary stats
        var statsTable = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Cyan1)
            .AddColumn("[bold]Statistic[/]")
            .AddColumn(new TableColumn("[bold]Value[/]").RightAligned());

        statsTable.AddRow("Total Pot", $"[green]€{totalPot}[/]");
        statsTable.AddRow("Betting Rounds", $"[white]{bettingRounds}[/]");
        statsTable.AddRow("Players Remaining", $"[white]{players.Count(p => p.IsActive)}[/]");

        AnsiConsole.Write(statsTable);
        AnsiConsole.WriteLine();

        // Chip counts leaderboard
        var chipTable = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Yellow)
            .Title("[bold yellow]💰 CHIP COUNTS[/]")
            .AddColumn(new TableColumn("[bold]#[/]").Centered())
            .AddColumn("[bold]Player[/]")
            .AddColumn(new TableColumn("[bold]Chips[/]").RightAligned());

        var activePlayers = players.Where(p => p.IsActive).OrderByDescending(p => p.Chips).ToList();
        for (int i = 0; i < activePlayers.Count; i++)
        {
            var player = activePlayers[i];
            var medal = i switch
            {
                0 => "🥇",
                1 => "🥈",
                2 => "🥉",
                _ => $"{i + 1}."
            };
            var chipColor = i == 0 ? "green" : "white";
            chipTable.AddRow(medal, $"[white]{player.Name}[/]", $"[{chipColor}]€{player.Chips:N0}[/]");
        }

        AnsiConsole.Write(chipTable);

        // Eliminated players
        var eliminatedPlayers = players.Where(p => !p.IsActive).ToList();
        if (eliminatedPlayers.Any())
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold red]❌ ELIMINATED:[/]");
            foreach (var player in eliminatedPlayers)
            {
                AnsiConsole.MarkupLine($"   [strikethrough dim]{player.Name}[/]");
            }
        }
    }

    /// <summary>
    /// Display player summary for between hands
    /// </summary>
    public void DisplayPlayerSummary(List<IPlayer> players, int dealerPosition)
    {
        AnsiConsole.WriteLine();

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .Title("[bold]👥 PLAYERS[/]")
            .AddColumn(new TableColumn("[bold]#[/]").Centered())
            .AddColumn("[bold]Player[/]")
            .AddColumn("[bold]Type[/]")
            .AddColumn(new TableColumn("[bold]Chips[/]").RightAligned())
            .AddColumn(new TableColumn("[bold]Position[/]").Centered());

        var activePlayers = players.Where(p => p.IsActive).OrderByDescending(p => p.Chips).ToList();
        for (int i = 0; i < activePlayers.Count; i++)
        {
            var player = activePlayers[i];
            var playerIndex = players.IndexOf(player);
            var position = playerIndex == dealerPosition ? "[bold blue] D [/]" : "";
            var playerType = player.Personality?.ToString() ?? "Human";
            var typeColor = player.Personality == null ? "cyan" : "grey";

            table.AddRow(
                $"{i + 1}",
                $"[white]{player.Name}[/]",
                $"[{typeColor}]{playerType}[/]",
                $"[green]€{player.Chips:N0}[/]",
                position
            );
        }

        AnsiConsole.Write(table);
    }

    /// <summary>
    /// Display preparing for next hand
    /// </summary>
    public void DisplayPreparingNextHand(string newDealer)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[dim]🔄 Preparing for next hand...[/]");
        AnsiConsole.MarkupLine($"[yellow]🔘 Dealer button moves to {newDealer}[/]");
    }

    /// <summary>
    /// Display hand completed time
    /// </summary>
    public void DisplayHandCompleted(int handNumber, double seconds)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[dim]⏱️ Hand #{handNumber} completed in {seconds:F1} seconds[/]");
    }

    /// <summary>
    /// Display all players' hands at showdown with nice formatting
    /// </summary>
    public void DisplayShowdownHands(List<IPlayer> players, List<Card> communityCards)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[bold cyan]Revealing Hands[/]").RuleStyle("cyan"));
        AnsiConsole.WriteLine();

        foreach (var player in players)
        {
            var handResult = HandEvaluator.EvaluateHand(player.HoleCards.Concat(communityCards));
            var cardsMarkup = SpectreCardRenderer.CombineCardsHorizontallyWithMarkup(player.HoleCards.Cast<Card?>());
            var cardsContent = string.Join("\n", cardsMarkup);

            var panel = new Panel(new Rows(
                new Markup(cardsContent).Centered(),
                new Text(""),
                new Markup($"[bold cyan]{handResult.Description}[/]").Centered()
            ))
            .Header($"[bold yellow] {player.Name} [/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Yellow)
            .Padding(1, 0);

            AnsiConsole.Write(panel);
            AnsiConsole.WriteLine();
        }
    }

    /// <summary>
    /// Display winners at showdown with nice formatting
    /// </summary>
    public void DisplayShowdownWinners(List<Game.PotWinner> winners)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[bold green]Winners[/]").RuleStyle("green"));
        AnsiConsole.WriteLine();

        var rows = new List<IRenderable>();

        foreach (var winner in winners)
        {
            rows.Add(new Markup($"[bold green]{winner.Player.Name}[/] wins [bold yellow]€{winner.Amount}[/]"));
            rows.Add(new Markup($"[dim]from[/] {winner.PotType}"));
            rows.Add(new Markup($"[dim]with[/] [cyan]{winner.HandDescription}[/]"));
            rows.Add(new Text(""));
        }

        var panel = new Panel(new Rows(rows.ToArray()))
            .Header("[bold yellow on green] WINNERS [/]")
            .Border(BoxBorder.Double)
            .BorderColor(Color.Gold1)
            .Padding(2, 1);

        AnsiConsole.Write(panel);
    }

    /// <summary>
    /// Display game over with winner information
    /// </summary>
    public void DisplayGameOver(List<IPlayer> activePlayers)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[bold red]GAME OVER[/]").RuleStyle("red").DoubleBorder());
        AnsiConsole.WriteLine();

        if (activePlayers.Count == 1)
        {
            var winner = activePlayers.First();
            var isHuman = winner is HumanPlayer;

            var title = isHuman ? "[bold green]CONGRATULATIONS![/]" : "[bold cyan]WINNER[/]";
            var subtitle = isHuman
                ? "You won the tournament!"
                : $"The AI player {winner.Name} ({winner.Personality}) has won!";

            var winnerPanel = new Panel(new Rows(
                new FigletText("WINNER!")
                    .Color(Color.Gold1)
                    .Centered(),
                new Text(""),
                new Markup($"[bold yellow]{winner.Name}[/]").Centered(),
                new Markup($"[bold green]€{winner.Chips:N0}[/]").Centered(),
                new Text(""),
                new Markup($"[dim]{subtitle}[/]").Centered()
            ))
            .Header($"[bold yellow on green] TOURNAMENT CHAMPION [/]")
            .Border(BoxBorder.Double)
            .BorderColor(Color.Gold1)
            .Padding(2, 1);

            AnsiConsole.Write(winnerPanel);
        }
        else if (activePlayers.Count > 1)
        {
            var rankings = activePlayers.OrderByDescending(p => p.Chips).ToList();

            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Gold1)
                .AddColumn(new TableColumn("[bold]Rank[/]").Centered())
                .AddColumn(new TableColumn("[bold]Player[/]"))
                .AddColumn(new TableColumn("[bold]Chips[/]").RightAligned())
                .AddColumn(new TableColumn("[bold]Type[/]").Centered());

            for (int i = 0; i < rankings.Count; i++)
            {
                var player = rankings[i];
                var medal = i switch { 0 => "🥇", 1 => "🥈", 2 => "🥉", _ => $"{i + 1}." };
                var rankColor = i switch { 0 => "gold1", 1 => "grey", 2 => "orange3", _ => "white" };
                var playerType = player is HumanPlayer ? "Human" : player.Personality?.ToString() ?? "AI";

                table.AddRow(
                    new Markup($"[{rankColor}]{medal}[/]"),
                    new Markup($"[bold]{player.Name}[/]"),
                    new Markup($"[green]€{player.Chips:N0}[/]"),
                    new Markup($"[dim]{playerType}[/]")
                );
            }

            var panel = new Panel(table)
                .Header("[bold yellow] FINAL RANKINGS [/]")
                .Border(BoxBorder.Double)
                .BorderColor(Color.Yellow)
                .Padding(1, 0);

            AnsiConsole.Write(panel);
        }
        else
        {
            AnsiConsole.MarkupLine("[dim red]No players remaining! What a strange game...[/]");
        }

        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Display game statistics in a nice table
    /// </summary>
    public void DisplayGameStatistics(int handsPlayed, TimeSpan duration, double? avgPot, int? maxPot, int? totalBettingRounds)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Cyan1)
            .AddColumn(new TableColumn("[bold cyan]Statistic[/]"))
            .AddColumn(new TableColumn("[bold cyan]Value[/]").RightAligned());

        table.AddRow("[bold]Total Hands Played[/]", $"[yellow]{handsPlayed}[/]");
        table.AddRow("[bold]Game Duration[/]", $"[yellow]{duration:hh\\:mm\\:ss}[/]");

        if (avgPot.HasValue)
            table.AddRow("[bold]Average Pot Size[/]", $"[green]€{avgPot.Value:F0}[/]");
        if (maxPot.HasValue)
            table.AddRow("[bold]Largest Pot[/]", $"[green]€{maxPot.Value:N0}[/]");
        if (totalBettingRounds.HasValue)
            table.AddRow("[bold]Total Betting Rounds[/]", $"[yellow]{totalBettingRounds.Value}[/]");

        var panel = new Panel(table)
            .Header("[bold cyan] GAME STATISTICS [/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Cyan1)
            .Padding(1, 0);

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Display blinds increase notification
    /// </summary>
    public void DisplayBlindsIncrease(int oldSmallBlind, int oldBigBlind, int newSmallBlind, int newBigBlind)
    {
        AnsiConsole.WriteLine();

        var content = new Rows(
            new Markup("[bold]OLD BLINDS[/]").Centered(),
            new Markup($"[dim red]€{oldSmallBlind}/€{oldBigBlind}[/]").Centered(),
            new Text(""),
            new Markup("[bold green]↓[/]").Centered(),
            new Text(""),
            new Markup("[bold]NEW BLINDS[/]").Centered(),
            new Markup($"[bold green]€{newSmallBlind}/€{newBigBlind}[/]").Centered()
        );

        var panel = new Panel(content)
            .Header("[bold yellow on red] BLINDS INCREASED! [/]")
            .Border(BoxBorder.Double)
            .BorderColor(Color.Red)
            .Padding(2, 1);

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Display thanks for playing message
    /// </summary>
    public void DisplayThanksForPlaying()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[bold green]Thanks for playing Texas Hold'em![/]").RuleStyle("green"));
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Press any key to exit...[/]");
    }
}
