using TexasHoldem.Domain;
using TexasHoldem.Domain.Enums;

namespace TexasHoldem.Players;

public abstract class AiPlayer : IPlayer
{
    protected readonly Random _random;
    
    public string Name { get; }
    public int Chips { get; set; }
    public List<Card> HoleCards { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public bool IsAllIn { get; set; }
    public bool HasFolded { get; set; }
    public PersonalityType? Personality { get; }
    
    // AI stats tracking
    protected Dictionary<string, PlayerStats> _opponentStats = new();
    protected int _handsPlayed = 0;
    protected int _handsWon = 0;

    protected AiPlayer(string name, int startingChips, PersonalityType personality, Random? random = null)
    {
        Name = name;
        Chips = startingChips;
        Personality = personality;
        _random = random ?? new Random();
    }

    public abstract PlayerAction TakeTurn(GameState gameState);

    public virtual void ReceiveCards(List<Card> cards)
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

    public virtual void Reset()
    {
        HoleCards.Clear();
        IsAllIn = false;
        HasFolded = false;
        IsActive = Chips > 0;
    }

    /// <summary>
    /// Resets AI stats for a completely new game session.
    /// Call this when starting a fresh game, not between hands.
    /// </summary>
    public virtual void ResetForNewGame()
    {
        Reset();
        _opponentStats.Clear();
        _handsPlayed = 0;
        _handsWon = 0;
    }

    public virtual void ShowCards()
    {
        Console.WriteLine($"{Name}'s cards: {string.Join(" ", HoleCards.Select(c => c.GetDisplayString()))}");
    }

    public virtual void HideCards()
    {
        // AI cards are typically hidden during play
    }

    protected virtual List<ActionType> GetValidActions(GameState gameState)
    {
        var actions = new List<ActionType>();
        
        if (!HasFolded)
        {
            actions.Add(ActionType.Fold);
        }

        if (IsAllIn || Chips <= 0)
        {
            return actions;
        }

        var callAmount = Math.Max(0, gameState.CurrentBet - gameState.GetPlayerBetThisRound(this));
        
        if (callAmount == 0)
        {
            actions.Add(ActionType.Check);
        }
        
        if (callAmount > 0 && callAmount <= Chips)
        {
            actions.Add(ActionType.Call);
        }

        if (gameState.CurrentBet == 0 && Chips > 0)
        {
            actions.Add(ActionType.Bet);
        }

        if (gameState.CurrentBet > 0 && Chips > callAmount)
        {
            actions.Add(ActionType.Raise);
        }

        if (Chips > 0)
        {
            actions.Add(ActionType.AllIn);
        }

        return actions;
    }

    protected virtual double EvaluateHandStrength(GameState gameState)
    {
        var allCards = new List<Card>(HoleCards);
        allCards.AddRange(gameState.CommunityCards);
        
        if (allCards.Count < 5) 
        {
            // Pre-flop or early betting rounds - evaluate hole cards
            return EvaluateHoleCards();
        }
        
        var handResult = HandEvaluator.EvaluateHand(allCards);
        
        // Normalize hand strength to 0.0 - 1.0 scale
        return (double)handResult.Strength / 10.0;
    }

    private double EvaluateHoleCards()
    {
        if (HoleCards.Count != 2) return 0.0;
        
        var card1 = HoleCards[0];
        var card2 = HoleCards[1];
        
        // Pocket pairs are strong
        if (card1.Rank == card2.Rank)
        {
            var pairStrength = (int)card1.Rank / 14.0; // Normalize to 0-1
            return 0.6 + (pairStrength * 0.4); // 0.6-1.0 range for pairs
        }
        
        // Suited cards get bonus
        bool suited = card1.Suit == card2.Suit;
        
        // High cards are valuable
        var highCard = (int)((int)card1.Rank > (int)card2.Rank ? card1.Rank : card2.Rank);
        var lowCard = (int)((int)card1.Rank < (int)card2.Rank ? card1.Rank : card2.Rank);
        
        double strength = (highCard + lowCard) / 28.0; // Normalize
        
        if (suited) strength += 0.1; // Suited bonus
        
        // Connected cards (potential straights) get small bonus
        if (Math.Abs((int)card1.Rank - (int)card2.Rank) == 1)
        {
            strength += 0.05;
        }
        
        return Math.Min(strength, 1.0);
    }

    protected virtual void UpdateOpponentStats(GameState gameState)
    {
        foreach (var action in gameState.ActionsThisRound)
        {
            if (action.PlayerId != Name)
            {
                if (!_opponentStats.ContainsKey(action.PlayerId))
                {
                    _opponentStats[action.PlayerId] = new PlayerStats();
                }
                
                var stats = _opponentStats[action.PlayerId];
                stats.TotalActions++;
                
                switch (action.Action)
                {
                    case ActionType.Fold:
                        stats.Folds++;
                        break;
                    case ActionType.Bet:
                    case ActionType.Raise:
                        stats.AggressiveActions++;
                        break;
                    case ActionType.Call:
                        stats.Calls++;
                        break;
                }
            }
        }
    }

    protected virtual double GetOpponentTightness(string playerId)
    {
        if (!_opponentStats.ContainsKey(playerId))
            return 0.5; // Default unknown
        
        var stats = _opponentStats[playerId];
        if (stats.TotalActions == 0) return 0.5;
        
        return (double)stats.Folds / stats.TotalActions;
    }

    protected virtual double GetOpponentAggression(string playerId)
    {
        if (!_opponentStats.ContainsKey(playerId))
            return 0.5; // Default unknown
        
        var stats = _opponentStats[playerId];
        var nonFoldActions = stats.TotalActions - stats.Folds;
        
        if (nonFoldActions == 0) return 0.0;
        
        return (double)stats.AggressiveActions / nonFoldActions;
    }

    protected class PlayerStats
    {
        public int TotalActions { get; set; }
        public int Folds { get; set; }
        public int Calls { get; set; }
        public int AggressiveActions { get; set; }
    }

    public override string ToString()
    {
        var personalityStr = Personality?.ToString() ?? "Unknown";
        return $"{Name} ({personalityStr}) - €{Chips}";
    }
}