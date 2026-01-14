using TexasHoldem.Domain.Enums;
using TexasHoldem.Players;

namespace TexasHoldem.Domain;

public class PotWinner
{
    public required IPlayer Player { get; init; }
    public int Amount { get; init; }
    public HandStrength HandStrength { get; init; }
    public string HandDescription { get; init; } = string.Empty;
    public List<Card> WinningCards { get; init; } = new();
    public bool IsMainPot { get; init; }
    public string PotType => IsMainPot ? "Main Pot" : "Side Pot";
}

public class GameStatistics
{
    public int TotalHands { get; set; }
    public int TotalPots { get; set; }
    public Dictionary<string, PlayerStats> PlayerStatistics { get; } = new();
    public int LargestPot { get; set; }
    public HandStrength BestHandSeen { get; set; } = HandStrength.HighCard;
    public TimeSpan GameDuration { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int HandsPlayed => TotalHands;
    public int PlayersRemaining => PlayerStatistics.Count(p => p.Value.TotalWinnings > 0);
    public string CurrentBlinds { get; set; } = "50/100";
    public List<RoundSummary> RoundHistory { get; set; } = new();

    public void AddPlayerStat(string playerName, PlayerStats stats)
    {
        PlayerStatistics[playerName] = stats;
    }

    public PlayerStats GetPlayerStats(string playerName)
    {
        PlayerStatistics.TryGetValue(playerName, out var stats);
        return stats ?? new PlayerStats { PlayerName = playerName };
    }
}

public class PlayerStats
{
    public required string PlayerName { get; init; }
    public int HandsPlayed { get; set; }
    public int HandsWon { get; set; }
    public int TotalWinnings { get; set; }
    public int TotalBet { get; set; }
    public int ShowdownsWon { get; set; }
    public int ShowdownsReached { get; set; }
    public HandStrength BestHand { get; set; } = HandStrength.HighCard;
    public int BiggestPotWon { get; set; }
    public double WinRate => HandsPlayed > 0 ? (double)HandsWon / HandsPlayed : 0.0;
    public int NetWinnings => TotalWinnings - TotalBet;
    public double ShowdownWinRate => ShowdownsReached > 0 ? (double)ShowdownsWon / ShowdownsReached : 0.0;
}

public class RoundSummary
{
    public int HandNumber { get; init; }
    public int TotalPot { get; init; }
    public string Winner { get; init; } = string.Empty;
    public HandStrength WinningHand { get; init; }
    public DateTime Timestamp { get; init; }
}