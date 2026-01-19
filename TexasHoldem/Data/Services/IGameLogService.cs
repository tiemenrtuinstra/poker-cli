using TexasHoldem.Game;
using TexasHoldem.Game.Enums;
using TexasHoldem.Players;

namespace TexasHoldem.Data.Services;

public interface IGameLogService
{
    // Session management
    Task<Guid> StartSessionAsync(int startingChips, int smallBlind, int bigBlind);
    Task EndSessionAsync(Guid sessionId, IPlayer? winner);

    // Hand management
    Task<Guid> StartHandAsync(Guid sessionId, int handNumber, int dealerPosition, int smallBlind, int bigBlind, IReadOnlyList<IPlayer> players);
    Task EndHandAsync(Guid handId, int finalPotSize, bool wentToShowdown);

    // Participant tracking
    Task UpdateParticipantHoleCardsAsync(Guid handId, IPlayer player, IReadOnlyList<Card> cards);
    Task UpdateParticipantEndingChipsAsync(Guid handId, IPlayer player, int endingChips, string finalStatus);

    // Action logging
    Task LogActionAsync(Guid handId, IPlayer player, ActionType actionType, int amount, BettingPhase phase, int potSize);

    // Community cards
    Task LogCommunityCardsAsync(Guid handId, BettingPhase phase, IReadOnlyList<Card> cards);

    // Outcomes
    Task LogOutcomeAsync(Guid handId, IPlayer player, int amount, string potType, string? handStrength, string? handDescription, bool wonByFold);

    // Player registration (returns player entity ID)
    Task<Guid> EnsurePlayerRegisteredAsync(IPlayer player);
}
