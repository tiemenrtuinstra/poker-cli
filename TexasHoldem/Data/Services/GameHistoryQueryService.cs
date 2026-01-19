using Microsoft.EntityFrameworkCore;
using TexasHoldem.Game.Enums;

namespace TexasHoldem.Data.Services;

public class GameHistoryQueryService : IGameHistoryQueryService
{
    private readonly GameLogDbContext _context;

    public GameHistoryQueryService(GameLogDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<PlayerOverview>> GetAllPlayersWithStatsAsync()
    {
        var players = await _context.Players.ToListAsync();

        if (!players.Any())
            return Array.Empty<PlayerOverview>();

        var result = new List<PlayerOverview>();

        foreach (var player in players)
        {
            var participations = await _context.HandParticipants
                .Include(hp => hp.Hand)
                .Where(hp => hp.PlayerId == player.Id)
                .ToListAsync();

            if (!participations.Any())
            {
                result.Add(new PlayerOverview
                {
                    PlayerName = player.Name,
                    PlayerType = player.PlayerType,
                    TotalHands = 0,
                    HandsWon = 0,
                    NetChips = 0,
                    LastPlayed = null
                });
                continue;
            }

            var handIds = participations.Select(p => p.HandId).ToHashSet();
            var handsWon = await _context.Outcomes
                .CountAsync(o => o.PlayerId == player.Id && handIds.Contains(o.HandId));

            var totalChipsWon = await _context.Outcomes
                .Where(o => o.PlayerId == player.Id && handIds.Contains(o.HandId))
                .SumAsync(o => o.Amount);

            var totalChipsLost = participations.Sum(p => Math.Max(0, p.StartingChips - p.EndingChips));
            var lastPlayed = participations.Max(p => p.Hand?.StartedAt);

            result.Add(new PlayerOverview
            {
                PlayerName = player.Name,
                PlayerType = player.PlayerType,
                TotalHands = participations.Count,
                HandsWon = handsWon,
                NetChips = totalChipsWon - totalChipsLost,
                LastPlayed = lastPlayed
            });
        }

        return result.OrderByDescending(p => p.TotalHands).ThenBy(p => p.PlayerName).ToList();
    }

    public async Task<PlayerStatistics?> GetPlayerStatisticsAsync(string playerName)
    {
        var player = await _context.Players.FirstOrDefaultAsync(p => p.Name == playerName);
        if (player == null) return null;

        var participations = await _context.HandParticipants
            .Where(hp => hp.PlayerId == player.Id)
            .ToListAsync();

        if (!participations.Any())
        {
            return new PlayerStatistics
            {
                PlayerName = playerName,
                PlayerType = player.PlayerType
            };
        }

        var handIds = participations.Select(p => p.HandId).ToHashSet();

        var actions = await _context.Actions
            .Where(a => a.PlayerId == player.Id && handIds.Contains(a.HandId))
            .ToListAsync();

        var outcomes = await _context.Outcomes
            .Where(o => o.PlayerId == player.Id && handIds.Contains(o.HandId))
            .ToListAsync();

        var hands = await _context.Hands
            .Where(h => handIds.Contains(h.Id))
            .ToListAsync();

        var handsWon = outcomes.Count;
        var totalChipsWon = outcomes.Sum(o => o.Amount);
        var totalChipsLost = participations.Sum(p => Math.Max(0, p.StartingChips - p.EndingChips));

        // VPIP: Voluntary Put in Pot - called or raised preflop
        var preFlopActions = actions.Where(a => a.BettingPhase == "PreFlop").ToList();
        var vpipHands = preFlopActions
            .GroupBy(a => a.HandId)
            .Count(g => g.Any(a => a.ActionType is "Call" or "Raise" or "Bet" or "AllIn"));
        var vpipPercent = participations.Count > 0 ? (double)vpipHands / participations.Count : 0;

        // PFR: Pre-Flop Raise
        var pfrHands = preFlopActions
            .GroupBy(a => a.HandId)
            .Count(g => g.Any(a => a.ActionType is "Raise" or "Bet"));
        var pfrPercent = participations.Count > 0 ? (double)pfrHands / participations.Count : 0;

        // Aggression Factor: (Bets + Raises) / Calls
        var betsAndRaises = actions.Count(a => a.ActionType is "Bet" or "Raise");
        var calls = actions.Count(a => a.ActionType == "Call");
        var aggressionFactor = calls > 0 ? (double)betsAndRaises / calls : betsAndRaises;

        var showdownHands = hands.Where(h => h.WentToShowdown).Select(h => h.Id).ToHashSet();
        var showdownsReached = participations.Count(p => showdownHands.Contains(p.HandId) && p.FinalStatus != "Folded");
        var showdownsWon = outcomes.Count(o => showdownHands.Contains(o.HandId) && !o.WonByFold);

        return new PlayerStatistics
        {
            PlayerName = playerName,
            PlayerType = player.PlayerType,
            TotalHands = participations.Count,
            HandsWon = handsWon,
            TotalChipsWon = totalChipsWon,
            TotalChipsLost = totalChipsLost,
            VpipPercent = vpipPercent * 100,
            PfrPercent = pfrPercent * 100,
            AggressionFactor = aggressionFactor,
            ShowdownsReached = showdownsReached,
            ShowdownsWon = showdownsWon
        };
    }

    public async Task<OpponentProfile?> GetOpponentProfileAsync(string opponentName)
    {
        var player = await _context.Players.FirstOrDefaultAsync(p => p.Name == opponentName);
        if (player == null) return null;

        var actions = await _context.Actions
            .Where(a => a.PlayerId == player.Id)
            .ToListAsync();

        if (!actions.Any())
        {
            return new OpponentProfile
            {
                PlayerName = opponentName,
                PlayerType = player.PlayerType,
                Personality = player.Personality
            };
        }

        var handIds = actions.Select(a => a.HandId).Distinct().ToList();

        double CalculateFoldRate(string phase)
        {
            var phaseActions = actions.Where(a => a.BettingPhase == phase).ToList();
            var phaseHands = phaseActions.GroupBy(a => a.HandId).ToList();
            if (!phaseHands.Any()) return 0;

            var folds = phaseHands.Count(g => g.Any(a => a.ActionType == "Fold"));
            return (double)folds / phaseHands.Count;
        }

        var preFlopFoldRate = CalculateFoldRate("PreFlop");
        var flopFoldRate = CalculateFoldRate("Flop");
        var turnFoldRate = CalculateFoldRate("Turn");
        var riverFoldRate = CalculateFoldRate("River");

        var totalFolds = actions.Count(a => a.ActionType == "Fold");
        var overallFoldRate = (double)totalFolds / handIds.Count;

        var betsAndRaises = actions.Count(a => a.ActionType is "Bet" or "Raise");
        var calls = actions.Count(a => a.ActionType == "Call");
        var aggressionFactor = calls > 0 ? (double)betsAndRaises / calls : betsAndRaises;

        // C-bet rate: bet on flop after raising preflop
        var preFlopRaiseHands = actions
            .Where(a => a.BettingPhase == "PreFlop" && a.ActionType is "Raise" or "Bet")
            .Select(a => a.HandId)
            .Distinct()
            .ToHashSet();

        var flopBetsAfterPfr = actions
            .Count(a => a.BettingPhase == "Flop" && a.ActionType is "Bet" or "Raise" && preFlopRaiseHands.Contains(a.HandId));

        var cbetRate = preFlopRaiseHands.Count > 0 ? (double)flopBetsAfterPfr / preFlopRaiseHands.Count : 0;

        return new OpponentProfile
        {
            PlayerName = opponentName,
            PlayerType = player.PlayerType,
            Personality = player.Personality,
            HandsSampled = handIds.Count,
            PreFlopFoldRate = preFlopFoldRate,
            FlopFoldRate = flopFoldRate,
            TurnFoldRate = turnFoldRate,
            RiverFoldRate = riverFoldRate,
            OverallFoldRate = overallFoldRate,
            AggressionFactor = aggressionFactor,
            BluffFrequency = 0, // Would require more complex analysis
            ContinuationBetRate = cbetRate
        };
    }

    public async Task<IReadOnlyList<HandSummary>> GetRecentHandsAsync(string playerName, int count = 20)
    {
        var player = await _context.Players.FirstOrDefaultAsync(p => p.Name == playerName);
        if (player == null) return Array.Empty<HandSummary>();

        var participations = await _context.HandParticipants
            .Include(hp => hp.Hand)
            .Where(hp => hp.PlayerId == player.Id)
            .OrderByDescending(hp => hp.Hand!.StartedAt)
            .Take(count)
            .ToListAsync();

        var handIds = participations.Select(p => p.HandId).ToHashSet();

        var outcomes = await _context.Outcomes
            .Where(o => o.PlayerId == player.Id && handIds.Contains(o.HandId))
            .ToDictionaryAsync(o => o.HandId);

        return participations.Select(p => new HandSummary
        {
            HandId = p.HandId,
            HandNumber = p.Hand!.HandNumber,
            PlayedAt = p.Hand.StartedAt,
            HoleCards = p.HoleCards,
            StartingChips = p.StartingChips,
            EndingChips = p.EndingChips,
            FinalStatus = p.FinalStatus,
            PotSize = p.Hand.FinalPotSize,
            WentToShowdown = p.Hand.WentToShowdown,
            WinningHand = outcomes.TryGetValue(p.HandId, out var outcome) ? outcome.HandDescription : null
        }).ToList();
    }

    public async Task<IReadOnlyList<HandSummary>> GetHandsWithHoleCardsAsync(string playerName, string card1, string card2, int count = 50)
    {
        var player = await _context.Players.FirstOrDefaultAsync(p => p.Name == playerName);
        if (player == null) return Array.Empty<HandSummary>();

        // Normalize card search (order doesn't matter)
        var searchPattern1 = $"{card1},{card2}";
        var searchPattern2 = $"{card2},{card1}";

        var participations = await _context.HandParticipants
            .Include(hp => hp.Hand)
            .Where(hp => hp.PlayerId == player.Id &&
                         (hp.HoleCards == searchPattern1 || hp.HoleCards == searchPattern2))
            .OrderByDescending(hp => hp.Hand!.StartedAt)
            .Take(count)
            .ToListAsync();

        var handIds = participations.Select(p => p.HandId).ToHashSet();

        var outcomes = await _context.Outcomes
            .Where(o => o.PlayerId == player.Id && handIds.Contains(o.HandId))
            .ToDictionaryAsync(o => o.HandId);

        return participations.Select(p => new HandSummary
        {
            HandId = p.HandId,
            HandNumber = p.Hand!.HandNumber,
            PlayedAt = p.Hand.StartedAt,
            HoleCards = p.HoleCards,
            StartingChips = p.StartingChips,
            EndingChips = p.EndingChips,
            FinalStatus = p.FinalStatus,
            PotSize = p.Hand.FinalPotSize,
            WentToShowdown = p.Hand.WentToShowdown,
            WinningHand = outcomes.TryGetValue(p.HandId, out var outcome) ? outcome.HandDescription : null
        }).ToList();
    }

    public async Task<HandDetail?> GetHandDetailAsync(Guid handId)
    {
        var hand = await _context.Hands
            .Include(h => h.Participants)
                .ThenInclude(p => p.Player)
            .Include(h => h.Actions)
                .ThenInclude(a => a.Player)
            .Include(h => h.CommunityCards)
            .Include(h => h.Outcomes)
                .ThenInclude(o => o.Player)
            .FirstOrDefaultAsync(h => h.Id == handId);

        if (hand == null) return null;

        return new HandDetail
        {
            HandId = hand.Id,
            HandNumber = hand.HandNumber,
            StartedAt = hand.StartedAt,
            SmallBlind = hand.SmallBlind,
            BigBlind = hand.BigBlind,
            FinalPotSize = hand.FinalPotSize,
            WentToShowdown = hand.WentToShowdown,
            Participants = hand.Participants
                .OrderBy(p => p.SeatPosition)
                .Select(p => new HandDetailParticipant
                {
                    PlayerName = p.Player?.Name ?? "Unknown",
                    SeatPosition = p.SeatPosition,
                    StartingChips = p.StartingChips,
                    EndingChips = p.EndingChips,
                    HoleCards = p.HoleCards,
                    FinalStatus = p.FinalStatus
                }).ToList(),
            Actions = hand.Actions
                .OrderBy(a => a.ActionOrder)
                .Select(a => new HandDetailAction
                {
                    PlayerName = a.Player?.Name ?? "Unknown",
                    Phase = a.BettingPhase,
                    ActionType = a.ActionType,
                    Amount = a.Amount,
                    PotSizeAtAction = a.PotSizeAtAction,
                    ActionOrder = a.ActionOrder
                }).ToList(),
            CommunityCards = hand.CommunityCards
                .OrderBy(c => c.CardPosition)
                .Select(c => $"{c.CardRank[0]}{c.CardSuit[0].ToString().ToLower()}")
                .ToList(),
            Outcomes = hand.Outcomes
                .Select(o => new HandDetailOutcome
                {
                    PlayerName = o.Player?.Name ?? "Unknown",
                    Amount = o.Amount,
                    PotType = o.PotType,
                    HandDescription = o.HandDescription,
                    WonByFold = o.WonByFold
                }).ToList()
        };
    }

    public async Task<double> GetOpponentFoldFrequencyAsync(string opponentName, BettingPhase phase)
    {
        var player = await _context.Players.FirstOrDefaultAsync(p => p.Name == opponentName);
        if (player == null) return 0;

        var phaseString = phase.ToString();
        var phaseActions = await _context.Actions
            .Where(a => a.PlayerId == player.Id && a.BettingPhase == phaseString)
            .ToListAsync();

        if (!phaseActions.Any()) return 0;

        var hands = phaseActions.GroupBy(a => a.HandId).ToList();
        var folds = hands.Count(g => g.Any(a => a.ActionType == "Fold"));

        return (double)folds / hands.Count;
    }

    public async Task<double> GetOpponentAggressionFactorAsync(string opponentName)
    {
        var player = await _context.Players.FirstOrDefaultAsync(p => p.Name == opponentName);
        if (player == null) return 0;

        var actions = await _context.Actions
            .Where(a => a.PlayerId == player.Id)
            .ToListAsync();

        var betsAndRaises = actions.Count(a => a.ActionType is "Bet" or "Raise");
        var calls = actions.Count(a => a.ActionType == "Call");

        return calls > 0 ? (double)betsAndRaises / calls : betsAndRaises;
    }
}
