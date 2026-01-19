using TexasHoldem.Data.Entities;
using TexasHoldem.Data.Repositories;
using TexasHoldem.Game;
using TexasHoldem.Game.Enums;
using TexasHoldem.Players;

namespace TexasHoldem.Data.Services;

public class GameLogService : IGameLogService
{
    private readonly IGameLogRepository _repository;
    private readonly Dictionary<string, Guid> _playerIdCache = new();

    public GameLogService(IGameLogRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> StartSessionAsync(int startingChips, int smallBlind, int bigBlind)
    {
        var session = new SessionEntity
        {
            Id = Guid.NewGuid(),
            StartedAt = DateTime.UtcNow,
            StartingChips = startingChips,
            SmallBlind = smallBlind,
            BigBlind = bigBlind
        };

        await _repository.CreateSessionAsync(session);
        return session.Id;
    }

    public async Task EndSessionAsync(Guid sessionId, IPlayer? winner)
    {
        var session = await _repository.GetSessionAsync(sessionId);
        if (session == null) return;

        session.EndedAt = DateTime.UtcNow;
        if (winner != null)
        {
            session.WinnerId = await EnsurePlayerRegisteredAsync(winner);
        }

        await _repository.UpdateSessionAsync(session);
    }

    public async Task<Guid> StartHandAsync(Guid sessionId, int handNumber, int dealerPosition, int smallBlind, int bigBlind, IReadOnlyList<IPlayer> players)
    {
        var hand = new HandEntity
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            HandNumber = handNumber,
            DealerPosition = dealerPosition,
            SmallBlind = smallBlind,
            BigBlind = bigBlind,
            StartedAt = DateTime.UtcNow
        };

        await _repository.CreateHandAsync(hand);

        // Create participants for all players
        for (int i = 0; i < players.Count; i++)
        {
            var player = players[i];
            var playerId = await EnsurePlayerRegisteredAsync(player);

            var participant = new HandParticipantEntity
            {
                Id = Guid.NewGuid(),
                HandId = hand.Id,
                PlayerId = playerId,
                SeatPosition = i,
                StartingChips = player.Chips,
                EndingChips = player.Chips, // Will be updated at hand end
                FinalStatus = "Playing"
            };

            await _repository.CreateParticipantAsync(participant);
        }

        return hand.Id;
    }

    public async Task EndHandAsync(Guid handId, int finalPotSize, bool wentToShowdown)
    {
        var hand = await _repository.GetHandAsync(handId);
        if (hand == null) return;

        hand.EndedAt = DateTime.UtcNow;
        hand.FinalPotSize = finalPotSize;
        hand.WentToShowdown = wentToShowdown;

        await _repository.UpdateHandAsync(hand);
    }

    public async Task UpdateParticipantHoleCardsAsync(Guid handId, IPlayer player, IReadOnlyList<Card> cards)
    {
        var playerId = await EnsurePlayerRegisteredAsync(player);
        var participant = await _repository.GetParticipantAsync(handId, playerId);
        if (participant == null) return;

        participant.HoleCards = SerializeCards(cards);
        await _repository.UpdateParticipantAsync(participant);
    }

    public async Task UpdateParticipantEndingChipsAsync(Guid handId, IPlayer player, int endingChips, string finalStatus)
    {
        var playerId = await EnsurePlayerRegisteredAsync(player);
        var participant = await _repository.GetParticipantAsync(handId, playerId);
        if (participant == null) return;

        participant.EndingChips = endingChips;
        participant.FinalStatus = finalStatus;
        await _repository.UpdateParticipantAsync(participant);
    }

    public async Task LogActionAsync(Guid handId, IPlayer player, ActionType actionType, int amount, BettingPhase phase, int potSize)
    {
        var playerId = await EnsurePlayerRegisteredAsync(player);
        var actionOrder = await _repository.GetActionCountForHandAsync(handId);

        var action = new ActionEntity
        {
            Id = Guid.NewGuid(),
            HandId = handId,
            PlayerId = playerId,
            BettingPhase = phase.ToString(),
            ActionType = actionType.ToString(),
            Amount = amount,
            PotSizeAtAction = potSize,
            ActionOrder = actionOrder,
            Timestamp = DateTime.UtcNow
        };

        await _repository.CreateActionAsync(action);
    }

    public async Task LogCommunityCardsAsync(Guid handId, BettingPhase phase, IReadOnlyList<Card> cards)
    {
        var cardEntities = cards.Select((card, index) => new CommunityCardEntity
        {
            Id = Guid.NewGuid(),
            HandId = handId,
            BettingPhase = phase.ToString(),
            CardSuit = card.Suit.ToString(),
            CardRank = card.Rank.ToString(),
            CardPosition = phase == BettingPhase.Flop ? index : (phase == BettingPhase.Turn ? 3 : 4)
        }).ToList();

        await _repository.CreateCommunityCardsAsync(cardEntities);
    }

    public async Task LogOutcomeAsync(Guid handId, IPlayer player, int amount, string potType, string? handStrength, string? handDescription, bool wonByFold)
    {
        var playerId = await EnsurePlayerRegisteredAsync(player);

        var outcome = new OutcomeEntity
        {
            Id = Guid.NewGuid(),
            HandId = handId,
            PlayerId = playerId,
            Amount = amount,
            PotType = potType,
            HandStrength = handStrength,
            HandDescription = handDescription,
            WonByFold = wonByFold
        };

        await _repository.CreateOutcomeAsync(outcome);
    }

    public async Task<Guid> EnsurePlayerRegisteredAsync(IPlayer player)
    {
        if (_playerIdCache.TryGetValue(player.Name, out var cachedId))
        {
            return cachedId;
        }

        var (playerType, aiProvider) = GetPlayerTypeInfo(player);
        var personality = player.Personality?.ToString();

        var playerEntity = await _repository.GetOrCreatePlayerAsync(
            player.Name,
            playerType,
            personality,
            aiProvider
        );

        _playerIdCache[player.Name] = playerEntity.Id;
        return playerEntity.Id;
    }

    private static (string PlayerType, string? AiProvider) GetPlayerTypeInfo(IPlayer player)
    {
        return player switch
        {
            ClaudeAiPlayer => ("LlmAI", "Claude"),
            OpenAiPlayer => ("LlmAI", "OpenAI"),
            GeminiAiPlayer => ("LlmAI", "Gemini"),
            LlmAiPlayer => ("LlmAI", "Unknown"),
            BasicAiPlayer => ("BasicAI", null),
            HumanPlayer => ("Human", null),
            NetworkPlayer => ("Human", null), // Remote human
            _ => ("Unknown", null)
        };
    }

    private static string SerializeCards(IReadOnlyList<Card> cards)
    {
        return string.Join(",", cards.Select(c => $"{GetRankChar(c.Rank)}{GetSuitChar(c.Suit)}"));
    }

    private static char GetRankChar(Rank rank)
    {
        return rank switch
        {
            Rank.Two => '2',
            Rank.Three => '3',
            Rank.Four => '4',
            Rank.Five => '5',
            Rank.Six => '6',
            Rank.Seven => '7',
            Rank.Eight => '8',
            Rank.Nine => '9',
            Rank.Ten => 'T',
            Rank.Jack => 'J',
            Rank.Queen => 'Q',
            Rank.King => 'K',
            Rank.Ace => 'A',
            _ => '?'
        };
    }

    private static char GetSuitChar(Suit suit)
    {
        return suit switch
        {
            Suit.Hearts => 'h',
            Suit.Diamonds => 'd',
            Suit.Clubs => 'c',
            Suit.Spades => 's',
            _ => '?'
        };
    }
}
