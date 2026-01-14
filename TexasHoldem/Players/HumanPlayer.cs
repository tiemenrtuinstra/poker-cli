using Spectre.Console;
using TexasHoldem.Domain;
using TexasHoldem.Domain.Enums;
using TexasHoldem.CLI;

namespace TexasHoldem.Players;

public class HumanPlayer : IPlayer
{
    private readonly InputHelper _inputHelper;
    private readonly SpectreGameUI _gameUI;

    public string Name { get; }
    public int Chips { get; set; }
    public List<Card> HoleCards { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public bool IsAllIn { get; set; }
    public bool HasFolded { get; set; }
    public PersonalityType? Personality => null; // Human players don't have AI personalities

    public HumanPlayer(string name, int startingChips, InputHelper? inputHelper = null, SpectreGameUI? gameUI = null)
    {
        Name = name;
        Chips = startingChips;
        _inputHelper = inputHelper ?? new InputHelper();
        _gameUI = gameUI ?? new SpectreGameUI();
    }

    public PlayerAction TakeTurn(GameState gameState)
    {
        // Clear screen for privacy when multiple humans play
        AnsiConsole.Clear();
        AnsiConsole.WriteLine();

        // Privacy header
        var headerPanel = new Panel(
            new Markup($"[bold yellow]{Name}[/], it's your turn!"))
            .Header("[bold cyan]YOUR TURN[/]")
            .HeaderAlignment(Justify.Center)
            .Border(BoxBorder.Double)
            .BorderColor(Color.Yellow)
            .Padding(2, 0);
        AnsiConsole.Write(headerPanel);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Press ENTER to see your cards...[/]");
        Console.ReadKey(true);
        AnsiConsole.Clear();

        // Show the visual poker table
        var playerIndex = gameState.Players.IndexOf(this);
        _gameUI.DisplayVisualPokerTable(gameState, playerIndex);

        // Show your hole cards with ASCII art
        _gameUI.DisplayHoleCardsAscii(this);

        // Show betting information in a panel
        AnsiConsole.WriteLine();
        var callAmount = Math.Max(0, gameState.CurrentBet - gameState.GetPlayerBetThisRound(this));

        var bettingTable = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .AddColumn(new TableColumn("[bold]Info[/]").Centered())
            .AddColumn(new TableColumn("[bold]Amount[/]").Centered());

        bettingTable.AddRow("[cyan]Your chips[/]", $"[green]€{Chips}[/]");
        bettingTable.AddRow("[cyan]Current bet[/]", $"[yellow]€{gameState.CurrentBet}[/]");
        bettingTable.AddRow("[cyan]To call[/]", callAmount > 0 ? $"[red]€{callAmount}[/]" : "[green]€0[/]");

        AnsiConsole.Write(bettingTable);
        AnsiConsole.WriteLine();

        var validActions = GetValidActions(gameState);

        // Build action choices with descriptions
        var actionChoices = validActions.Select(a => GetActionDisplayText(a, gameState)).ToList();

        var selectedText = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold green]Choose your action:[/]")
                .PageSize(10)
                .HighlightStyle(new Style(Color.Black, Color.Green))
                .AddChoices(actionChoices));

        // Find the selected action
        var selectedIndex = actionChoices.IndexOf(selectedText);
        var selectedAction = validActions[selectedIndex];
        int amount = 0;

        if (selectedAction == ActionType.Bet || selectedAction == ActionType.Raise)
        {
            amount = GetBetAmount(gameState, selectedAction);
        }
        else if (selectedAction == ActionType.Call)
        {
            amount = Math.Max(0, gameState.CurrentBet - gameState.GetPlayerBetThisRound(this));
            amount = Math.Min(amount, Chips); // Can't bet more than we have
        }
        else if (selectedAction == ActionType.AllIn)
        {
            amount = Chips;
        }

        return new PlayerAction
        {
            PlayerId = Name,
            Action = selectedAction,
            Amount = amount,
            Timestamp = DateTime.Now,
            BettingPhase = gameState.BettingPhase
        };
    }

    private List<ActionType> GetValidActions(GameState gameState)
    {
        var actions = new List<ActionType>();

        // Always can fold (unless already folded)
        if (!HasFolded)
        {
            actions.Add(ActionType.Fold);
        }

        if (IsAllIn || Chips <= 0)
        {
            return actions; // Only fold if all-in or no chips
        }

        var callAmount = Math.Max(0, gameState.CurrentBet - gameState.GetPlayerBetThisRound(this));

        // Can check if no bet to call
        if (callAmount == 0)
        {
            actions.Add(ActionType.Check);
        }

        // Can call if there's a bet and we have chips
        if (callAmount > 0 && callAmount <= Chips)
        {
            actions.Add(ActionType.Call);
        }

        // Can bet if no current bet
        if (gameState.CurrentBet == 0 && Chips > 0)
        {
            actions.Add(ActionType.Bet);
        }

        // Can raise if there's a current bet and we have enough chips for minimum raise
        // (accounting for what we've already bet this round)
        var minRaise = gameState.CurrentBet * 2;
        var alreadyBet = gameState.GetPlayerBetThisRound(this);
        var chipsNeededForMinRaise = minRaise - alreadyBet;
        if (gameState.CurrentBet > 0 && Chips >= chipsNeededForMinRaise)
        {
            actions.Add(ActionType.Raise);
        }

        // Can always go all-in if we have chips
        if (Chips > 0)
        {
            actions.Add(ActionType.AllIn);
        }

        return actions;
    }

    private string GetActionDisplayText(ActionType action, GameState gameState)
    {
        var callAmount = Math.Max(0, gameState.CurrentBet - gameState.GetPlayerBetThisRound(this));

        return action switch
        {
            ActionType.Fold => "Fold",
            ActionType.Check => "Check",
            ActionType.Call => $"Call €{callAmount}",
            ActionType.Bet => "Bet",
            ActionType.Raise => $"Raise (min €{gameState.CurrentBet * 2})",
            ActionType.AllIn => $"All In (€{Chips})",
            _ => action.ToString()
        };
    }

    private int GetBetAmount(GameState gameState, ActionType action)
    {
        var alreadyBet = gameState.GetPlayerBetThisRound(this);

        var minBet = action == ActionType.Bet
            ? Math.Max(gameState.BigBlindAmount, 1)
            : gameState.CurrentBet * 2; // Minimum raise is double current bet

        // Max bet/raise = remaining chips + what you've already bet this round
        var maxBet = Chips + alreadyBet;

        // Show bet range info
        var infoPanel = new Panel(
            new Markup($"[cyan]Minimum:[/] [green]€{minBet}[/]\n[cyan]Maximum:[/] [green]€{maxBet}[/]"))
            .Header($"[bold yellow]{action} Amount[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Yellow);
        AnsiConsole.Write(infoPanel);

        return AnsiConsole.Prompt(
            new TextPrompt<int>($"[yellow]Enter {action.ToString().ToLower()} amount:[/]")
                .DefaultValue(minBet)
                .Validate(amount =>
                {
                    if (amount < minBet)
                        return ValidationResult.Error($"[red]Amount must be at least €{minBet}[/]");
                    if (amount > maxBet)
                        return ValidationResult.Error($"[red]Amount cannot exceed €{maxBet}[/]");
                    return ValidationResult.Success();
                }));
    }

    public void ReceiveCards(List<Card> cards)
    {
        HoleCards.Clear();
        HoleCards.AddRange(cards);
    }

    public void AddChips(int amount)
    {
        Chips += amount;
    }

    public bool RemoveChips(int amount)
    {
        if (amount > Chips) return false;
        Chips -= amount;
        return true;
    }

    public void Reset()
    {
        HoleCards.Clear();
        IsAllIn = false;
        HasFolded = false;
        IsActive = Chips > 0;
    }

    public void ShowCards()
    {
        AnsiConsole.MarkupLine($"[bold]{Name}[/]'s cards: {string.Join(" ", HoleCards.Select(c => c.GetDisplayString()))}");
    }

    public void HideCards()
    {
        // For hot-seat mode, we might want to clear the screen or show a message
        AnsiConsole.MarkupLine($"[dim]{Name}'s cards are hidden[/]");
    }

    public override string ToString()
    {
        return $"{Name} (Human) - €{Chips}";
    }
}
