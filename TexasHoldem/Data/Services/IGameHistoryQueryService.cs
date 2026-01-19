using TexasHoldem.Game.Enums;

namespace TexasHoldem.Data.Services;

public interface IGameHistoryQueryService
{
    // Player overview
    Task<IReadOnlyList<PlayerOverview>> GetAllPlayersWithStatsAsync();

    // Player statistics
    Task<PlayerStatistics?> GetPlayerStatisticsAsync(string playerName);
    Task<OpponentProfile?> GetOpponentProfileAsync(string opponentName);

    // Hand history
    Task<IReadOnlyList<HandSummary>> GetRecentHandsAsync(string playerName, int count = 20);
    Task<IReadOnlyList<HandSummary>> GetHandsWithHoleCardsAsync(string playerName, string card1, string card2, int count = 50);
    Task<HandDetail?> GetHandDetailAsync(Guid handId);

    // Opponent tendencies
    Task<double> GetOpponentFoldFrequencyAsync(string opponentName, BettingPhase phase);
    Task<double> GetOpponentAggressionFactorAsync(string opponentName);
}

// DTOs for query results
public record PlayerOverview
{
    public required string PlayerName { get; init; }
    public required string PlayerType { get; init; }
    public int TotalHands { get; init; }
    public int HandsWon { get; init; }
    public double WinRate => TotalHands > 0 ? (double)HandsWon / TotalHands : 0;
    public int NetChips { get; init; }
    public DateTime? LastPlayed { get; init; }
}

public record PlayerStatistics
{
    public required string PlayerName { get; init; }
    public required string PlayerType { get; init; }
    public int TotalHands { get; init; }
    public int HandsWon { get; init; }
    public double WinRate => TotalHands > 0 ? (double)HandsWon / TotalHands : 0;
    public int TotalChipsWon { get; init; }
    public int TotalChipsLost { get; init; }
    public int NetChips => TotalChipsWon - TotalChipsLost;
    public double VpipPercent { get; init; } // Voluntarily Put $ In Pot
    public double PfrPercent { get; init; } // Pre-Flop Raise %
    public double AggressionFactor { get; init; }
    public int ShowdownsReached { get; init; }
    public int ShowdownsWon { get; init; }
    public double ShowdownWinRate => ShowdownsReached > 0 ? (double)ShowdownsWon / ShowdownsReached : 0;
}

public record OpponentProfile
{
    public required string PlayerName { get; init; }
    public required string PlayerType { get; init; }
    public string? Personality { get; init; }
    public int HandsSampled { get; init; }
    public double PreFlopFoldRate { get; init; }
    public double FlopFoldRate { get; init; }
    public double TurnFoldRate { get; init; }
    public double RiverFoldRate { get; init; }
    public double OverallFoldRate { get; init; }
    public double AggressionFactor { get; init; }
    public double BluffFrequency { get; init; } // Ratio of bets/raises on river when losing at showdown
    public double ContinuationBetRate { get; init; }
    public string TendencyDescription => CategorizeTendency();

    private string CategorizeTendency()
    {
        var tight = PreFlopFoldRate > 0.7;
        var aggressive = AggressionFactor > 2.0;

        return (tight, aggressive) switch
        {
            (true, true) => "Tight-Aggressive (TAG)",
            (true, false) => "Tight-Passive (Rock)",
            (false, true) => "Loose-Aggressive (LAG)",
            (false, false) => "Loose-Passive (Calling Station)"
        };
    }
}

public record HandSummary
{
    public Guid HandId { get; init; }
    public int HandNumber { get; init; }
    public DateTime PlayedAt { get; init; }
    public string? HoleCards { get; init; }
    public int StartingChips { get; init; }
    public int EndingChips { get; init; }
    public int ChipsWonOrLost => EndingChips - StartingChips;
    public required string FinalStatus { get; init; }
    public int PotSize { get; init; }
    public bool WentToShowdown { get; init; }
    public string? WinningHand { get; init; }
}

public record HandDetail
{
    public Guid HandId { get; init; }
    public int HandNumber { get; init; }
    public DateTime StartedAt { get; init; }
    public int SmallBlind { get; init; }
    public int BigBlind { get; init; }
    public int FinalPotSize { get; init; }
    public bool WentToShowdown { get; init; }
    public required IReadOnlyList<HandDetailParticipant> Participants { get; init; }
    public required IReadOnlyList<HandDetailAction> Actions { get; init; }
    public required IReadOnlyList<string> CommunityCards { get; init; }
    public required IReadOnlyList<HandDetailOutcome> Outcomes { get; init; }
}

public record HandDetailParticipant
{
    public required string PlayerName { get; init; }
    public int SeatPosition { get; init; }
    public int StartingChips { get; init; }
    public int EndingChips { get; init; }
    public string? HoleCards { get; init; }
    public required string FinalStatus { get; init; }
}

public record HandDetailAction
{
    public required string PlayerName { get; init; }
    public required string Phase { get; init; }
    public required string ActionType { get; init; }
    public int Amount { get; init; }
    public int PotSizeAtAction { get; init; }
    public int ActionOrder { get; init; }
}

public record HandDetailOutcome
{
    public required string PlayerName { get; init; }
    public int Amount { get; init; }
    public required string PotType { get; init; }
    public string? HandDescription { get; init; }
    public bool WonByFold { get; init; }
}
