using TexasHoldem.Domain;
using TexasHoldem.Domain.Enums;
using TexasHoldem.CLI;

namespace TexasHoldem.Players;

public class HumanPlayer : IPlayer
{
    private readonly InputHelper _inputHelper;
    private readonly GameUI _gameUI;

    public string Name { get; }
    public int Chips { get; set; }
    public List<Card> HoleCards { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public bool IsAllIn { get; set; }
    public bool HasFolded { get; set; }
    public PersonalityType? Personality => null; // Human players don't have AI personalities

    public HumanPlayer(string name, int startingChips, InputHelper? inputHelper = null, GameUI? gameUI = null)
    {
        Name = name;
        Chips = startingChips;
        _inputHelper = inputHelper ?? new InputHelper();
        _gameUI = gameUI ?? new GameUI();
    }

    public PlayerAction TakeTurn(GameState gameState)
    {
        // Clear screen for privacy when multiple humans play
        Console.Clear();
        Console.WriteLine();
        _gameUI.DrawSeparator('═', 60);
        _gameUI.ShowColoredMessage($"  🎯 {Name}, it's your turn!", ConsoleColor.Yellow);
        _gameUI.DrawSeparator('═', 60);
        Console.WriteLine();
        Console.Write("Press ENTER to see your cards...");
        Console.ReadLine();
        Console.Clear();

        // Show the visual poker table
        var playerIndex = gameState.Players.IndexOf(this);
        _gameUI.DisplayVisualPokerTable(gameState, playerIndex);

        // Show your hole cards with ASCII art
        _gameUI.DisplayHoleCardsAscii(this);

        // Show betting information
        Console.WriteLine();
        _gameUI.DrawSeparator('-', 40);
        Console.WriteLine($"  💰 Your chips: ${Chips}");
        Console.WriteLine($"  💵 Current bet to match: ${gameState.CurrentBet}");
        Console.WriteLine($"  💸 You need to call: ${Math.Max(0, gameState.CurrentBet - gameState.GetPlayerBetThisRound(this))}");
        _gameUI.DrawSeparator('-', 40);
        Console.WriteLine();

        var validActions = GetValidActions(gameState);
        
        while (true)
        {
            Console.WriteLine("Available actions:");
            for (int i = 0; i < validActions.Count; i++)
            {
                var action = validActions[i];
                var actionText = GetActionDisplayText(action, gameState);
                Console.WriteLine($"{i + 1}. {actionText}");
            }

            Console.Write("Choose your action (1-{0}): ", validActions.Count);
            
            if (int.TryParse(Console.ReadLine(), out int choice) && 
                choice >= 1 && choice <= validActions.Count)
            {
                var selectedAction = validActions[choice - 1];
                int amount = 0;

                if (selectedAction == ActionType.Bet || selectedAction == ActionType.Raise)
                {
                    amount = GetBetAmount(gameState, selectedAction);
                    if (amount == -1) continue; // Invalid amount, retry
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

            Console.WriteLine("❌ Invalid choice. Please try again.");
        }
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
            ActionType.Call => $"Call ${callAmount}",
            ActionType.Bet => "Bet",
            ActionType.Raise => $"Raise (min ${gameState.CurrentBet * 2})",
            ActionType.AllIn => $"All In (${Chips})",
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
        
        Console.WriteLine($"💵 Minimum {action.ToString().ToLower()}: ${minBet}");
        Console.WriteLine($"💰 Maximum {action.ToString().ToLower()}: ${maxBet}");
        
        while (true)
        {
            Console.Write($"Enter {action.ToString().ToLower()} amount: $");
            
            if (int.TryParse(Console.ReadLine(), out int amount))
            {
                if (amount >= minBet && amount <= maxBet)
                {
                    return amount;
                }
                Console.WriteLine($"❌ Amount must be between ${minBet} and ${maxBet}");
            }
            else
            {
                Console.WriteLine("❌ Please enter a valid number");
            }
            
            Console.Write("Try again? (y/n): ");
            if (Console.ReadLine()?.ToLower() != "y")
            {
                return -1; // Cancel action
            }
        }
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
        Console.WriteLine($"{Name}'s cards: {string.Join(" ", HoleCards.Select(c => c.GetDisplayString()))}");
    }

    public void HideCards()
    {
        // For hot-seat mode, we might want to clear the screen or show a message
        Console.WriteLine($"🔒 {Name}'s cards are hidden");
    }

    public override string ToString()
    {
        return $"{Name} (Human) - ${Chips}";
    }
}