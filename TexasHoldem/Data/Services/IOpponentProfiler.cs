using TexasHoldem.Game.Enums;

namespace TexasHoldem.Data.Services;

/// <summary>
/// Provides opponent profiling data for AI decision making.
/// Combines historical database data with current session observations.
/// </summary>
public interface IOpponentProfiler
{
    /// <summary>
    /// Get fold frequency for an opponent at a specific betting phase.
    /// Returns value between 0 and 1.
    /// </summary>
    Task<double> GetFoldFrequencyAsync(string opponentName, BettingPhase phase);

    /// <summary>
    /// Get aggression factor for an opponent.
    /// AF = (Bets + Raises) / Calls. Higher = more aggressive.
    /// </summary>
    Task<double> GetAggressionFactorAsync(string opponentName);

    /// <summary>
    /// Get a summary profile for an opponent, useful for LLM prompts.
    /// </summary>
    Task<OpponentProfileSummary?> GetProfileSummaryAsync(string opponentName);

    /// <summary>
    /// Check if we have enough historical data on an opponent to make reliable predictions.
    /// </summary>
    Task<bool> HasSufficientDataAsync(string opponentName, int minimumHands = 10);
}

/// <summary>
/// Condensed opponent profile for AI decision making.
/// </summary>
public record OpponentProfileSummary
{
    public required string PlayerName { get; init; }
    public string? PlayerType { get; init; }
    public string? Personality { get; init; }
    public int HandsAnalyzed { get; init; }
    public double PreFlopFoldRate { get; init; }
    public double PostFlopFoldRate { get; init; }
    public double AggressionFactor { get; init; }
    public double ContinuationBetRate { get; init; }
    public required string TendencyDescription { get; init; }

    /// <summary>
    /// Returns a formatted string suitable for including in LLM prompts.
    /// </summary>
    public string ToPromptString()
    {
        return $@"- {PlayerName} ({TendencyDescription}):
  * Hands analyzed: {HandsAnalyzed}
  * PreFlop fold rate: {PreFlopFoldRate:P0}
  * PostFlop fold rate: {PostFlopFoldRate:P0}
  * Aggression Factor: {AggressionFactor:F1}
  * C-Bet rate: {ContinuationBetRate:P0}";
    }
}
