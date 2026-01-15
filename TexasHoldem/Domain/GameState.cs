using System.Text.Json.Serialization;
using TexasHoldem.Domain.Enums;
using TexasHoldem.Players;

namespace TexasHoldem.Domain;

public class GameState
{
    public int HandNumber { get; set; }
    public GamePhase Phase { get; set; }
    public BettingPhase BettingPhase { get; set; }
    public List<IPlayer> Players { get; set; } = new();
    public List<IPlayer> ActivePlayers { get; set; } = new();
    public int DealerPosition { get; set; }
    public int SmallBlindPosition { get; set; }
    public int BigBlindPosition { get; set; }
    public int CurrentPlayerPosition { get; set; }
    public List<Card> CommunityCards { get; set; } = new();
    public int TotalPot { get; set; }
    public int CurrentBet { get; set; }
    public int SmallBlindAmount { get; set; }
    public int BigBlindAmount { get; set; }
    public int AnteAmount { get; set; }
    public bool IsRoundComplete { get; set; }
    public DateTime HandStartTime { get; set; }
    public List<PlayerAction> ActionsThisRound { get; set; } = new();
    public List<SidePot> SidePots { get; set; } = new();
    public Dictionary<string, int> PlayerBets { get; set; } = new();
    public Dictionary<string, bool> PlayerHasFolded { get; set; } = new();
    public Dictionary<string, bool> PlayerHasActed { get; set; } = new();
    public Dictionary<string, int> PlayerChips { get; set; } = new();

    /// <summary>
    /// Tracks total contributions per player across ALL betting rounds in current hand.
    /// Used for side pot calculation. Reset at start of each new hand.
    /// </summary>
    public Dictionary<string, int> TotalContributions { get; set; } = new();

    [JsonIgnore]
    public IPlayer? CurrentPlayer => CurrentPlayerPosition < Players.Count ? Players[CurrentPlayerPosition] : null;

    [JsonIgnore]
    public IPlayer? Dealer => DealerPosition < Players.Count ? Players[DealerPosition] : null;

    [JsonIgnore]
    public IPlayer? SmallBlind => SmallBlindPosition < Players.Count ? Players[SmallBlindPosition] : null;

    [JsonIgnore]
    public IPlayer? BigBlind => BigBlindPosition < Players.Count ? Players[BigBlindPosition] : null;

    public GameState()
    {
        HandStartTime = DateTime.Now;
    }

    public GameState Clone()
    {
        return new GameState
        {
            HandNumber = HandNumber,
            Phase = Phase,
            BettingPhase = BettingPhase,
            Players = new List<IPlayer>(Players),
            ActivePlayers = new List<IPlayer>(ActivePlayers),
            DealerPosition = DealerPosition,
            SmallBlindPosition = SmallBlindPosition,
            BigBlindPosition = BigBlindPosition,
            CurrentPlayerPosition = CurrentPlayerPosition,
            CommunityCards = new List<Card>(CommunityCards),
            TotalPot = TotalPot,
            CurrentBet = CurrentBet,
            SmallBlindAmount = SmallBlindAmount,
            BigBlindAmount = BigBlindAmount,
            AnteAmount = AnteAmount,
            IsRoundComplete = IsRoundComplete,
            HandStartTime = HandStartTime,
            ActionsThisRound = new List<PlayerAction>(ActionsThisRound),
            SidePots = new List<SidePot>(SidePots),
            PlayerBets = new Dictionary<string, int>(PlayerBets),
            PlayerHasFolded = new Dictionary<string, bool>(PlayerHasFolded),
            PlayerHasActed = new Dictionary<string, bool>(PlayerHasActed),
            PlayerChips = new Dictionary<string, int>(PlayerChips),
            TotalContributions = new Dictionary<string, int>(TotalContributions)
        };
    }

    public void ResetForNewBettingRound()
    {
        CurrentBet = 0;
        ActionsThisRound.Clear();
        PlayerBets.Clear();
        PlayerHasActed.Clear();
        IsRoundComplete = false;
    }

    public void AddPlayerAction(IPlayer player, ActionType action, int amount = 0)
    {
        var playerAction = new PlayerAction
        {
            PlayerId = player.Name,
            Action = action,
            Amount = amount,
            Timestamp = DateTime.Now,
            BettingPhase = BettingPhase
        };

        ActionsThisRound.Add(playerAction);
        PlayerHasActed[player.Name] = true;

        if (amount > 0)
        {
            PlayerBets[player.Name] = PlayerBets.GetValueOrDefault(player.Name, 0) + amount;
            TotalContributions[player.Name] = TotalContributions.GetValueOrDefault(player.Name, 0) + amount;
            TotalPot += amount;
        }

        if (action == ActionType.Fold)
        {
            PlayerHasFolded[player.Name] = true;
        }
    }

    public bool HasPlayerActed(IPlayer player)
    {
        return PlayerHasActed.GetValueOrDefault(player.Name, false);
    }

    public bool HasPlayerFolded(IPlayer player)
    {
        return PlayerHasFolded.GetValueOrDefault(player.Name, false);
    }

    public int GetPlayerBetThisRound(IPlayer player)
    {
        return PlayerBets.GetValueOrDefault(player.Name, 0);
    }
}

public class PlayerAction
{
    public string PlayerId { get; set; } = string.Empty;
    public ActionType Action { get; set; }
    public int Amount { get; set; }
    public DateTime Timestamp { get; set; }
    public BettingPhase BettingPhase { get; set; }
}

public class SidePot
{
    public int Amount { get; set; }
    public List<string> EligiblePlayers { get; set; } = new();
    public string? Winner { get; set; }
}