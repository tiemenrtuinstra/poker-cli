using Spectre.Console;
using TexasHoldem.Data.Services;

namespace TexasHoldem.CLI;

/// <summary>
/// Menu for viewing hand history and player statistics.
/// </summary>
public class HandHistoryMenu
{
    private readonly IGameHistoryQueryService _queryService;

    public HandHistoryMenu(IGameHistoryQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task ShowAsync()
    {
        while (true)
        {
            Console.Clear();
            HeaderHelper.DisplaySubHeader("HISTORY", Color.Aqua);

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold yellow]What would you like to view?[/]")
                    .HighlightStyle(new Style(Color.Black, Color.Yellow))
                    .AddChoices(new[]
                    {
                        "📊  View My Statistics",
                        "👤  View Player Statistics",
                        "🎴  View Recent Hands",
                        "🔍  View Hand Details",
                        "👥  View Opponent Profile",
                        "🔙  Back to Main Menu"
                    }));

            if (choice.Contains("Back"))
            {
                return;
            }

            try
            {
                if (choice.Contains("My Statistics"))
                {
                    await ShowMyStatisticsAsync();
                }
                else if (choice.Contains("Player Statistics"))
                {
                    await ShowPlayerStatisticsAsync();
                }
                else if (choice.Contains("Recent Hands"))
                {
                    await ShowRecentHandsAsync();
                }
                else if (choice.Contains("Hand Details"))
                {
                    await ShowHandDetailsAsync();
                }
                else if (choice.Contains("Opponent Profile"))
                {
                    await ShowOpponentProfileAsync();
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
            }
        }
    }

    private async Task ShowMyStatisticsAsync()
    {
        var playerName = AnsiConsole.Ask<string>("Enter [green]your player name[/]:");
        await ShowStatisticsForPlayerAsync(playerName);
    }

    private async Task ShowPlayerStatisticsAsync()
    {
        var playerName = AnsiConsole.Ask<string>("Enter [green]player name[/]:");
        await ShowStatisticsForPlayerAsync(playerName);
    }

    private async Task ShowStatisticsForPlayerAsync(string playerName)
    {
        var stats = await _queryService.GetPlayerStatisticsAsync(playerName);

        if (stats == null)
        {
            AnsiConsole.MarkupLine($"[yellow]No data found for player '{playerName}'[/]");
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
            return;
        }

        Console.Clear();
        AnsiConsole.Write(new Rule($"[bold aqua]Statistics for {stats.PlayerName}[/]").RuleStyle("grey"));
        Console.WriteLine();

        var table = new Table();
        table.Border = TableBorder.Rounded;
        table.AddColumn(new TableColumn("[bold]Metric[/]").LeftAligned());
        table.AddColumn(new TableColumn("[bold]Value[/]").RightAligned());

        table.AddRow("Player Type", stats.PlayerType);
        table.AddRow("Total Hands", stats.TotalHands.ToString());
        table.AddRow("Hands Won", stats.HandsWon.ToString());
        table.AddRow("Win Rate", $"{stats.WinRate:P1}");
        table.AddRow(new Markup("[grey]─────────────[/]"), new Markup("[grey]─────────[/]"));
        table.AddRow("Total Chips Won", $"[green]€{stats.TotalChipsWon:N0}[/]");
        table.AddRow("Total Chips Lost", $"[red]€{stats.TotalChipsLost:N0}[/]");
        table.AddRow("Net Profit/Loss", stats.NetChips >= 0
            ? $"[green]+€{stats.NetChips:N0}[/]"
            : $"[red]€{stats.NetChips:N0}[/]");
        table.AddRow(new Markup("[grey]─────────────[/]"), new Markup("[grey]─────────[/]"));
        table.AddRow("VPIP %", $"{stats.VpipPercent:F1}%");
        table.AddRow("PFR %", $"{stats.PfrPercent:F1}%");
        table.AddRow("Aggression Factor", $"{stats.AggressionFactor:F2}");
        table.AddRow(new Markup("[grey]─────────────[/]"), new Markup("[grey]─────────[/]"));
        table.AddRow("Showdowns Reached", stats.ShowdownsReached.ToString());
        table.AddRow("Showdowns Won", stats.ShowdownsWon.ToString());
        table.AddRow("Showdown Win Rate", $"{stats.ShowdownWinRate:P1}");

        AnsiConsole.Write(table);

        // Interpretation
        Console.WriteLine();
        AnsiConsole.Write(new Rule("[bold]Interpretation[/]").RuleStyle("grey"));

        var style = (stats.VpipPercent, stats.AggressionFactor) switch
        {
            ( < 25, > 2) => "Tight-Aggressive (TAG) - Optimal style",
            ( < 25, _) => "Tight-Passive (Rock) - Too passive",
            ( >= 25, > 2) => "Loose-Aggressive (LAG) - Advanced style",
            _ => "Loose-Passive (Calling Station) - Losing style"
        };

        AnsiConsole.MarkupLine($"Playing Style: [bold]{style}[/]");

        Console.WriteLine("\nPress any key to continue...");
        Console.ReadKey();
    }

    private async Task ShowRecentHandsAsync()
    {
        var playerName = AnsiConsole.Ask<string>("Enter [green]player name[/]:");
        var count = AnsiConsole.Ask<int>("How many hands to show?", 20);

        var hands = await _queryService.GetRecentHandsAsync(playerName, count);

        if (!hands.Any())
        {
            AnsiConsole.MarkupLine($"[yellow]No hands found for player '{playerName}'[/]");
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
            return;
        }

        Console.Clear();
        AnsiConsole.Write(new Rule($"[bold aqua]Recent Hands for {playerName}[/]").RuleStyle("grey"));
        Console.WriteLine();

        var table = new Table();
        table.Border = TableBorder.Rounded;
        table.AddColumn("Hand #");
        table.AddColumn("Hole Cards");
        table.AddColumn("Result");
        table.AddColumn("Chips +/-");
        table.AddColumn("Pot");
        table.AddColumn("Showdown");

        foreach (var hand in hands)
        {
            var resultColor = hand.FinalStatus switch
            {
                "Won" => "green",
                "Folded" => "grey",
                _ => "red"
            };

            var chipsChange = hand.ChipsWonOrLost >= 0
                ? $"[green]+€{hand.ChipsWonOrLost}[/]"
                : $"[red]€{hand.ChipsWonOrLost}[/]";

            table.AddRow(
                $"#{hand.HandNumber}",
                hand.HoleCards ?? "?",
                $"[{resultColor}]{hand.FinalStatus}[/]",
                chipsChange,
                $"€{hand.PotSize}",
                hand.WentToShowdown ? "Yes" : "No"
            );
        }

        AnsiConsole.Write(table);

        Console.WriteLine("\nPress any key to continue...");
        Console.ReadKey();
    }

    private async Task ShowHandDetailsAsync()
    {
        var handIdStr = AnsiConsole.Ask<string>("Enter [green]hand ID (GUID)[/]:");

        if (!Guid.TryParse(handIdStr, out var handId))
        {
            AnsiConsole.MarkupLine("[red]Invalid hand ID format[/]");
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
            return;
        }

        var hand = await _queryService.GetHandDetailAsync(handId);

        if (hand == null)
        {
            AnsiConsole.MarkupLine("[yellow]Hand not found[/]");
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
            return;
        }

        Console.Clear();
        AnsiConsole.Write(new Rule($"[bold aqua]Hand #{hand.HandNumber} Details[/]").RuleStyle("grey"));
        Console.WriteLine();

        // Hand info
        AnsiConsole.MarkupLine($"[bold]Date:[/] {hand.StartedAt:g}");
        AnsiConsole.MarkupLine($"[bold]Blinds:[/] €{hand.SmallBlind}/€{hand.BigBlind}");
        AnsiConsole.MarkupLine($"[bold]Final Pot:[/] €{hand.FinalPotSize}");
        AnsiConsole.MarkupLine($"[bold]Showdown:[/] {(hand.WentToShowdown ? "Yes" : "No")}");

        if (hand.CommunityCards.Any())
        {
            AnsiConsole.MarkupLine($"[bold]Board:[/] {string.Join(" ", hand.CommunityCards)}");
        }

        Console.WriteLine();

        // Participants
        AnsiConsole.Write(new Rule("[bold]Participants[/]").RuleStyle("grey"));
        var participantTable = new Table();
        participantTable.Border = TableBorder.Simple;
        participantTable.AddColumn("Player");
        participantTable.AddColumn("Hole Cards");
        participantTable.AddColumn("Start");
        participantTable.AddColumn("End");
        participantTable.AddColumn("Result");

        foreach (var p in hand.Participants)
        {
            var change = p.EndingChips - p.StartingChips;
            var changeStr = change >= 0 ? $"[green]+€{change}[/]" : $"[red]€{change}[/]";

            participantTable.AddRow(
                p.PlayerName,
                p.HoleCards ?? "?",
                $"€{p.StartingChips}",
                $"€{p.EndingChips}",
                $"{p.FinalStatus} ({changeStr})"
            );
        }

        AnsiConsole.Write(participantTable);

        // Actions
        if (hand.Actions.Any())
        {
            Console.WriteLine();
            AnsiConsole.Write(new Rule("[bold]Actions[/]").RuleStyle("grey"));
            var actionTable = new Table();
            actionTable.Border = TableBorder.Simple;
            actionTable.AddColumn("#");
            actionTable.AddColumn("Phase");
            actionTable.AddColumn("Player");
            actionTable.AddColumn("Action");
            actionTable.AddColumn("Amount");

            foreach (var a in hand.Actions)
            {
                actionTable.AddRow(
                    a.ActionOrder.ToString(),
                    a.Phase,
                    a.PlayerName,
                    a.ActionType,
                    a.Amount > 0 ? $"€{a.Amount}" : "-"
                );
            }

            AnsiConsole.Write(actionTable);
        }

        // Outcomes
        if (hand.Outcomes.Any())
        {
            Console.WriteLine();
            AnsiConsole.Write(new Rule("[bold]Winners[/]").RuleStyle("grey"));
            foreach (var outcome in hand.Outcomes)
            {
                var wonBy = outcome.WonByFold ? "by fold" : outcome.HandDescription ?? "";
                AnsiConsole.MarkupLine($"[green]{outcome.PlayerName}[/] won [bold]€{outcome.Amount}[/] ({outcome.PotType} pot) {wonBy}");
            }
        }

        Console.WriteLine("\nPress any key to continue...");
        Console.ReadKey();
    }

    private async Task ShowOpponentProfileAsync()
    {
        var opponentName = AnsiConsole.Ask<string>("Enter [green]opponent name[/]:");

        var profile = await _queryService.GetOpponentProfileAsync(opponentName);

        if (profile == null || profile.HandsSampled < 5)
        {
            AnsiConsole.MarkupLine($"[yellow]Not enough data for '{opponentName}' (need at least 5 hands)[/]");
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
            return;
        }

        Console.Clear();
        AnsiConsole.Write(new Rule($"[bold aqua]Opponent Profile: {profile.PlayerName}[/]").RuleStyle("grey"));
        Console.WriteLine();

        var table = new Table();
        table.Border = TableBorder.Rounded;
        table.AddColumn(new TableColumn("[bold]Metric[/]").LeftAligned());
        table.AddColumn(new TableColumn("[bold]Value[/]").RightAligned());

        table.AddRow("Player Type", profile.PlayerType);
        if (!string.IsNullOrEmpty(profile.Personality))
        {
            table.AddRow("Personality", profile.Personality);
        }
        table.AddRow("Hands Analyzed", profile.HandsSampled.ToString());
        table.AddRow("Playing Style", $"[bold]{profile.TendencyDescription}[/]");
        table.AddRow(new Markup("[grey]─────────────[/]"), new Markup("[grey]─────────[/]"));
        table.AddRow("PreFlop Fold Rate", $"{profile.PreFlopFoldRate:P0}");
        table.AddRow("Flop Fold Rate", $"{profile.FlopFoldRate:P0}");
        table.AddRow("Turn Fold Rate", $"{profile.TurnFoldRate:P0}");
        table.AddRow("River Fold Rate", $"{profile.RiverFoldRate:P0}");
        table.AddRow("Overall Fold Rate", $"{profile.OverallFoldRate:P0}");
        table.AddRow(new Markup("[grey]─────────────[/]"), new Markup("[grey]─────────[/]"));
        table.AddRow("Aggression Factor", $"{profile.AggressionFactor:F2}");
        table.AddRow("C-Bet Rate", $"{profile.ContinuationBetRate:P0}");

        AnsiConsole.Write(table);

        // Strategy advice
        Console.WriteLine();
        AnsiConsole.Write(new Rule("[bold]Strategy Advice[/]").RuleStyle("grey"));

        if (profile.OverallFoldRate > 0.5)
        {
            AnsiConsole.MarkupLine("[green]• This player folds often - consider bluffing more frequently[/]");
        }
        else if (profile.OverallFoldRate < 0.3)
        {
            AnsiConsole.MarkupLine("[yellow]• This player rarely folds - value bet thin, avoid bluffs[/]");
        }

        if (profile.AggressionFactor > 2.5)
        {
            AnsiConsole.MarkupLine("[yellow]• Very aggressive - trap with strong hands, call down light[/]");
        }
        else if (profile.AggressionFactor < 1.0)
        {
            AnsiConsole.MarkupLine("[green]• Passive player - bet for value, respect raises[/]");
        }

        if (profile.ContinuationBetRate > 0.7)
        {
            AnsiConsole.MarkupLine("[yellow]• High c-bet rate - consider check-raising or floating[/]");
        }

        Console.WriteLine("\nPress any key to continue...");
        Console.ReadKey();
    }
}
