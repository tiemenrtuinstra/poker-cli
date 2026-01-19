using TexasHoldem.Game.Enums;

namespace TexasHoldem.Data.Services;

/// <summary>
/// Implementation of opponent profiler that uses historical game data.
/// </summary>
public class OpponentProfiler : IOpponentProfiler
{
    private readonly IGameHistoryQueryService _queryService;

    public OpponentProfiler(IGameHistoryQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<double> GetFoldFrequencyAsync(string opponentName, BettingPhase phase)
    {
        return await _queryService.GetOpponentFoldFrequencyAsync(opponentName, phase);
    }

    public async Task<double> GetAggressionFactorAsync(string opponentName)
    {
        return await _queryService.GetOpponentAggressionFactorAsync(opponentName);
    }

    public async Task<OpponentProfileSummary?> GetProfileSummaryAsync(string opponentName)
    {
        var profile = await _queryService.GetOpponentProfileAsync(opponentName);
        if (profile == null || profile.HandsSampled < 5)
        {
            return null;
        }

        var postFlopFoldRate = (profile.FlopFoldRate + profile.TurnFoldRate + profile.RiverFoldRate) / 3.0;

        return new OpponentProfileSummary
        {
            PlayerName = profile.PlayerName,
            PlayerType = profile.PlayerType,
            Personality = profile.Personality,
            HandsAnalyzed = profile.HandsSampled,
            PreFlopFoldRate = profile.PreFlopFoldRate,
            PostFlopFoldRate = postFlopFoldRate,
            AggressionFactor = profile.AggressionFactor,
            ContinuationBetRate = profile.ContinuationBetRate,
            TendencyDescription = profile.TendencyDescription
        };
    }

    public async Task<bool> HasSufficientDataAsync(string opponentName, int minimumHands = 10)
    {
        var profile = await _queryService.GetOpponentProfileAsync(opponentName);
        return profile != null && profile.HandsSampled >= minimumHands;
    }
}
