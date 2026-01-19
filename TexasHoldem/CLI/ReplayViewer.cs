using Spectre.Console;
using TexasHoldem.Game;
using TexasHoldem.Game.Enums;

namespace TexasHoldem.CLI;

public class ReplayViewer
{
    private readonly Logger _logger;
    private readonly InputHelper _inputHelper;
    private readonly SpectreGameUI _gameUI;

    public ReplayViewer(Logger logger)
    {
        _logger = logger;
        _inputHelper = new InputHelper();
        _gameUI = new SpectreGameUI();
    }

    public async Task ShowReplayMenu()
    {
        while (true)
        {
            AnsiConsole.Clear();

            // Header
            AnsiConsole.Write(
                new FigletText("REPLAY")
                    .Color(Color.Cyan1)
                    .Centered());

            AnsiConsole.Write(new Rule("[bold cyan]Hand History Viewer[/]").RuleStyle("cyan"));
            AnsiConsole.WriteLine();

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold green]What would you like to do?[/]")
                    .PageSize(8)
                    .HighlightStyle(new Style(Color.Black, Color.Cyan1))
                    .AddChoices(new[]
                    {
                        "View Recent Hands",
                        "Load Hand History File",
                        "Show Game Statistics",
                        "Back to Main Menu"
                    }));

            switch (choice)
            {
                case "View Recent Hands":
                    await ShowRecentHands();
                    break;
                case "Load Hand History File":
                    await LoadAndViewHandHistory();
                    break;
                case "Show Game Statistics":
                    ShowGameStatistics();
                    break;
                case "Back to Main Menu":
                    return;
            }
        }
    }

    private async Task ShowRecentHands()
    {
        var historyFiles = _logger.GetAvailableHandHistoryFiles();

        if (!historyFiles.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No hand history files found.[/]");
            _inputHelper.PressAnyKeyToContinue();
            return;
        }

        // Get the most recent file
        var latestFile = historyFiles.OrderByDescending(f => f).First();
        var handRecords = _logger.LoadHandHistory(latestFile);

        if (!handRecords.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No hands found in the history file.[/]");
            _inputHelper.PressAnyKeyToContinue();
            return;
        }

        await ViewHandRecords(handRecords, $"Recent hands from {latestFile}");
    }

    private async Task LoadAndViewHandHistory()
    {
        var historyFiles = _logger.GetAvailableHandHistoryFiles();

        if (!historyFiles.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No hand history files found.[/]");
            _inputHelper.PressAnyKeyToContinue();
            return;
        }

        var selectedFile = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold cyan]Select a hand history file:[/]")
                .PageSize(10)
                .HighlightStyle(new Style(Color.Black, Color.Cyan1))
                .AddChoices(historyFiles.OrderByDescending(f => f)));

        var handRecords = _logger.LoadHandHistory(selectedFile);

        if (!handRecords.Any())
        {
            AnsiConsole.MarkupLine($"[yellow]No hands found in {selectedFile}[/]");
            _inputHelper.PressAnyKeyToContinue();
            return;
        }

        await ViewHandRecords(handRecords, selectedFile);
    }

    private async Task ViewHandRecords(List<HandRecord> handRecords, string source)
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule($"[bold cyan]{source}[/]").RuleStyle("cyan"));
        AnsiConsole.MarkupLine($"[dim]Total hands: {handRecords.Count}[/]");
        AnsiConsole.WriteLine();

        // Show hands in a table
        var handsTable = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .AddColumn(new TableColumn("[bold]#[/]").Centered())
            .AddColumn(new TableColumn("[bold]Hand[/]").Centered())
            .AddColumn(new TableColumn("[bold]Date/Time[/]").Centered())
            .AddColumn(new TableColumn("[bold]Pot[/]").Centered());

        for (int i = 0; i < Math.Min(handRecords.Count, 15); i++)
        {
            var hand = handRecords[i];
            handsTable.AddRow(
                $"{i + 1}",
                $"#{hand.HandNumber}",
                hand.StartTime.ToString("yyyy-MM-dd HH:mm"),
                $"[green]€{hand.TotalPot}[/]");
        }

        if (handRecords.Count > 15)
        {
            handsTable.AddRow("[dim]...[/]", $"[dim]+{handRecords.Count - 15} more[/]", "", "");
        }

        AnsiConsole.Write(handsTable);
        AnsiConsole.WriteLine();

        while (true)
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold green]Select an option:[/]")
                    .PageSize(8)
                    .HighlightStyle(new Style(Color.Black, Color.Green))
                    .AddChoices(new[]
                    {
                        "View Specific Hand",
                        "Step Through All Hands",
                        "Show Summary Statistics",
                        "Back to Replay Menu"
                    }));

            switch (choice)
            {
                case "View Specific Hand":
                    await ViewSpecificHand(handRecords);
                    break;
                case "Step Through All Hands":
                    await StepThroughHands(handRecords);
                    break;
                case "Show Summary Statistics":
                    ShowHandSummaryStatistics(handRecords);
                    break;
                case "Back to Replay Menu":
                    return;
            }
        }
    }

    private async Task ViewSpecificHand(List<HandRecord> handRecords)
    {
        var choices = handRecords.Select((h, i) =>
            $"Hand #{h.HandNumber} - {h.StartTime:HH:mm} - Pot: €{h.TotalPot}").ToList();

        var selectedText = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold cyan]Select a hand to view:[/]")
                .PageSize(15)
                .HighlightStyle(new Style(Color.Black, Color.Cyan1))
                .AddChoices(choices));

        var handIndex = choices.IndexOf(selectedText);
        var hand = handRecords[handIndex];

        await DisplayHandReplay(hand);
        _inputHelper.PressAnyKeyToContinue();
    }

    private async Task StepThroughHands(List<HandRecord> handRecords)
    {
        for (int i = 0; i < handRecords.Count; i++)
        {
            AnsiConsole.Clear();

            var progressPanel = new Panel(
                new Markup($"[bold]Hand {i + 1} of {handRecords.Count}[/]"))
                .Header("[bold yellow]STEPPING THROUGH HANDS[/]")
                .Border(BoxBorder.Double)
                .BorderColor(Color.Yellow);
            AnsiConsole.Write(progressPanel);
            AnsiConsole.WriteLine();

            await DisplayHandReplay(handRecords[i]);

            AnsiConsole.WriteLine();
            if (i < handRecords.Count - 1)
            {
                var continueChoice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[bold green]Continue?[/]")
                        .HighlightStyle(new Style(Color.Black, Color.Green))
                        .AddChoices(new[] { "Next Hand", "Stop" }));

                if (continueChoice == "Stop") break;
            }
            else
            {
                AnsiConsole.MarkupLine("[green]Reached the end of hand history.[/]");
                _inputHelper.PressAnyKeyToContinue();
            }
        }
    }

    private async Task DisplayHandReplay(HandRecord hand)
    {
        // Hand header
        var headerTable = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Cyan1)
            .AddColumn(new TableColumn("[bold]Property[/]"))
            .AddColumn(new TableColumn("[bold]Value[/]"));

        headerTable.AddRow("Hand Number", $"[bold]#{hand.HandNumber}[/]");
        headerTable.AddRow("Time", $"{hand.StartTime:yyyy-MM-dd HH:mm:ss}");
        headerTable.AddRow("Duration", $"{(hand.EndTime - hand.StartTime).TotalSeconds:F1} seconds");
        headerTable.AddRow("Blinds", $"[yellow]€{hand.SmallBlind}/€{hand.BigBlind}[/]");
        headerTable.AddRow("Total Pot", $"[green]€{hand.TotalPot}[/]");

        AnsiConsole.Write(headerTable);
        AnsiConsole.WriteLine();

        // Starting positions
        AnsiConsole.Write(new Rule("[bold cyan]Starting Positions[/]").RuleStyle("cyan").LeftJustified());

        var positionsTable = new Table()
            .Border(TableBorder.Simple)
            .AddColumn("Seat")
            .AddColumn("Player")
            .AddColumn("Chips")
            .AddColumn("Type")
            .AddColumn("Position");

        for (int i = 0; i < hand.Players.Count; i++)
        {
            var player = hand.Players[i];
            var position = i == hand.DealerPosition ? "[yellow](D)[/]" : "";
            var personality = player.IsHuman ? "[cyan]Human[/]" : $"[magenta]{player.Personality ?? "AI"}[/]";
            positionsTable.AddRow(
                $"{i + 1}",
                player.Name,
                $"€{player.ChipsBefore}",
                personality,
                position);
        }

        AnsiConsole.Write(positionsTable);
        AnsiConsole.WriteLine();

        // Hole cards
        AnsiConsole.Write(new Rule("[bold cyan]Hole Cards[/]").RuleStyle("cyan").LeftJustified());

        foreach (var player in hand.Players)
        {
            if (player.IsHuman)
            {
                AnsiConsole.MarkupLine($"  [bold]{player.Name}[/]: {string.Join(" ", player.HoleCards.Select(c => c.GetDisplayString()))}");
            }
            else
            {
                AnsiConsole.MarkupLine($"  [bold]{player.Name}[/]: [dim][Hidden] [Hidden][/]");
            }
        }
        AnsiConsole.WriteLine();

        // Betting rounds
        foreach (var round in hand.BettingRounds)
        {
            AnsiConsole.Write(new Rule($"[bold yellow]{round.Phase}[/]").RuleStyle("yellow").LeftJustified());

            if (round.Phase != BettingPhase.PreFlop && hand.CommunityCards.Any())
            {
                var cardsToShow = round.Phase switch
                {
                    BettingPhase.Flop => hand.CommunityCards.Take(3),
                    BettingPhase.Turn => hand.CommunityCards.Take(4),
                    BettingPhase.River => hand.CommunityCards.Take(5),
                    _ => hand.CommunityCards
                };
                AnsiConsole.MarkupLine($"  [cyan]Board:[/] {string.Join(" ", cardsToShow.Select(c => c.GetDisplayString()))}");
            }

            foreach (var action in round.Actions)
            {
                var amountStr = action.Amount > 0 ? $" [green]€{action.Amount}[/]" : "";
                AnsiConsole.MarkupLine($"  [bold]{action.PlayerId}[/]: {action.Action}{amountStr}");
            }

            if (round.TotalBet > 0)
            {
                AnsiConsole.MarkupLine($"  [dim]Total bet this round: €{round.TotalBet}[/]");
            }
            AnsiConsole.WriteLine();

            await Task.Delay(300); // Small delay for readability
        }

        // Final community cards
        if (hand.CommunityCards.Count == 5)
        {
            AnsiConsole.Write(new Rule("[bold green]Final Board[/]").RuleStyle("green").LeftJustified());
            AnsiConsole.MarkupLine($"  {string.Join(" ", hand.CommunityCards.Select(c => c.GetDisplayString()))}");
            AnsiConsole.WriteLine();
        }

        // Showdown results
        if (hand.Winners.Any())
        {
            AnsiConsole.Write(new Rule("[bold magenta]Showdown Results[/]").RuleStyle("magenta").LeftJustified());

            // Show all players' final hands
            foreach (var player in hand.Players.Where(p => !p.Folded))
            {
                var allCards = player.HoleCards.Concat(hand.CommunityCards).ToList();
                if (allCards.Count >= 5)
                {
                    var handResult = HandEvaluator.EvaluateHand(allCards);
                    AnsiConsole.MarkupLine($"  [bold]{player.Name}[/]: {string.Join(" ", player.HoleCards.Select(c => c.GetDisplayString()))} - [cyan]{handResult.Description}[/]");
                }
            }

            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Rule("[bold green]Winners[/]").RuleStyle("green").LeftJustified());

            foreach (var winner in hand.Winners)
            {
                AnsiConsole.MarkupLine($"  [bold green]{winner.PlayerName}[/] wins [bold]€{winner.AmountWon}[/] from {winner.PotType}");
                AnsiConsole.MarkupLine($"    [dim]with {winner.HandDescription}[/]");
            }
        }
        else
        {
            var winner = hand.Players.FirstOrDefault(p => !p.Folded);
            if (winner != null)
            {
                AnsiConsole.MarkupLine($"[bold green]{winner.Name}[/] wins by default (everyone else folded)");
            }
        }

        // Final chip counts
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[bold cyan]Final Chip Counts[/]").RuleStyle("cyan").LeftJustified());

        var chipsTable = new Table()
            .Border(TableBorder.Simple)
            .AddColumn("Player")
            .AddColumn("Final Chips")
            .AddColumn("Change");

        foreach (var player in hand.Players)
        {
            var change = player.ChipsAfter - player.ChipsBefore;
            var changeStr = change > 0 ? $"[green]+€{change}[/]" : change < 0 ? $"[red]-€{Math.Abs(change)}[/]" : "[dim]€0[/]";

            chipsTable.AddRow(player.Name, $"€{player.ChipsAfter}", changeStr);
        }

        AnsiConsole.Write(chipsTable);
    }

    private void ShowHandSummaryStatistics(List<HandRecord> handRecords)
    {
        AnsiConsole.Clear();

        AnsiConsole.Write(
            new FigletText("STATS")
                .Color(Color.Magenta1)
                .Centered());

        AnsiConsole.Write(new Rule("[bold magenta]Hand Summary Statistics[/]").RuleStyle("magenta"));
        AnsiConsole.WriteLine();

        if (!handRecords.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No hands to analyze.[/]");
            _inputHelper.PressAnyKeyToContinue();
            return;
        }

        var totalHands = handRecords.Count;
        var totalPot = handRecords.Sum(h => h.TotalPot);
        var averagePot = handRecords.Average(h => h.TotalPot);
        var largestPot = handRecords.Max(h => h.TotalPot);
        var smallestPot = handRecords.Min(h => h.TotalPot);

        // Overview table
        var overviewTable = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Magenta1)
            .AddColumn(new TableColumn("[bold]Statistic[/]"))
            .AddColumn(new TableColumn("[bold]Value[/]").RightAligned());

        overviewTable.AddRow("Total Hands", $"[cyan]{totalHands}[/]");
        overviewTable.AddRow("Total Money in Play", $"[green]€{totalPot:N0}[/]");
        overviewTable.AddRow("Average Pot Size", $"[yellow]€{averagePot:F0}[/]");
        overviewTable.AddRow("Largest Pot", $"[green]€{largestPot:N0}[/]");
        overviewTable.AddRow("Smallest Pot", $"[dim]€{smallestPot:N0}[/]");

        AnsiConsole.Write(overviewTable);
        AnsiConsole.WriteLine();

        // Player statistics
        var allPlayers = handRecords.SelectMany(h => h.Players).GroupBy(p => p.Name);

        AnsiConsole.Write(new Rule("[bold cyan]Player Statistics[/]").RuleStyle("cyan"));
        AnsiConsole.WriteLine();

        var playerTable = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Cyan1)
            .AddColumn(new TableColumn("[bold]Player[/]"))
            .AddColumn(new TableColumn("[bold]Hands[/]").Centered())
            .AddColumn(new TableColumn("[bold]Winnings[/]").RightAligned())
            .AddColumn(new TableColumn("[bold]Win Rate[/]").Centered())
            .AddColumn(new TableColumn("[bold]Fold Rate[/]").Centered());

        foreach (var playerGroup in allPlayers.OrderByDescending(g => g.Sum(p => p.ChipsAfter - p.ChipsBefore)))
        {
            var playerHands = playerGroup.ToList();
            var handsPlayed = playerHands.Count;
            var totalWinnings = playerHands.Sum(p => p.ChipsAfter - p.ChipsBefore);
            var winRate = playerHands.Count(p => p.ChipsAfter > p.ChipsBefore) / (double)handsPlayed * 100;
            var foldRate = playerHands.Count(p => p.Folded) / (double)handsPlayed * 100;

            var winningsColor = totalWinnings > 0 ? "green" : totalWinnings < 0 ? "red" : "dim";

            playerTable.AddRow(
                playerGroup.Key,
                handsPlayed.ToString(),
                $"[{winningsColor}]€{totalWinnings:+#;-#;0}[/]",
                $"{winRate:F1}%",
                $"{foldRate:F1}%");
        }

        AnsiConsole.Write(playerTable);
        AnsiConsole.WriteLine();

        _inputHelper.PressAnyKeyToContinue();
    }

    private void ShowGameStatistics()
    {
        AnsiConsole.Clear();

        AnsiConsole.Write(
            new FigletText("FILES")
                .Color(Color.Green)
                .Centered());

        AnsiConsole.Write(new Rule("[bold green]Game Statistics[/]").RuleStyle("green"));
        AnsiConsole.WriteLine();

        var logFiles = _logger.GetAvailableLogFiles();
        var handHistoryFiles = _logger.GetAvailableHandHistoryFiles();

        // Overview
        var overviewTable = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Green)
            .AddColumn(new TableColumn("[bold]Info[/]"))
            .AddColumn(new TableColumn("[bold]Value[/]"));

        overviewTable.AddRow("Log Directory", $"[dim]{_logger.GetLogDirectory()}[/]");
        overviewTable.AddRow("Log Files", $"[cyan]{logFiles.Count}[/]");
        overviewTable.AddRow("Hand History Files", $"[cyan]{handHistoryFiles.Count}[/]");

        AnsiConsole.Write(overviewTable);
        AnsiConsole.WriteLine();

        if (handHistoryFiles.Any())
        {
            AnsiConsole.Write(new Rule("[bold cyan]Hand History Files[/]").RuleStyle("cyan"));
            AnsiConsole.WriteLine();

            var filesTable = new Table()
                .Border(TableBorder.Simple)
                .AddColumn("File")
                .AddColumn("Created")
                .AddColumn("Size")
                .AddColumn("Hands");

            foreach (var file in handHistoryFiles.OrderByDescending(f => f))
            {
                var filePath = Path.Combine(_logger.GetLogDirectory(), file);
                var fileInfo = new FileInfo(filePath);
                var handCount = _logger.LoadHandHistory(file).Count;

                filesTable.AddRow(
                    file,
                    fileInfo.CreationTime.ToString("yyyy-MM-dd HH:mm"),
                    $"{fileInfo.Length / 1024.0:F1} KB",
                    handCount.ToString());
            }

            AnsiConsole.Write(filesTable);
        }

        AnsiConsole.WriteLine();
        _inputHelper.PressAnyKeyToContinue();
    }
}
