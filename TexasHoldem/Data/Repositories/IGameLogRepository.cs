using TexasHoldem.Data.Entities;

namespace TexasHoldem.Data.Repositories;

public interface IGameLogRepository
{
    // Session operations
    Task<SessionEntity> CreateSessionAsync(SessionEntity session);
    Task UpdateSessionAsync(SessionEntity session);
    Task<SessionEntity?> GetSessionAsync(Guid sessionId);

    // Player operations
    Task<PlayerEntity?> GetPlayerByNameAsync(string name);
    Task<PlayerEntity> GetOrCreatePlayerAsync(string name, string playerType, string? personality, string? aiProvider);

    // Hand operations
    Task<HandEntity> CreateHandAsync(HandEntity hand);
    Task UpdateHandAsync(HandEntity hand);
    Task<HandEntity?> GetHandAsync(Guid handId);
    Task<HandEntity?> GetHandWithDetailsAsync(Guid handId);

    // Participant operations
    Task<HandParticipantEntity> CreateParticipantAsync(HandParticipantEntity participant);
    Task UpdateParticipantAsync(HandParticipantEntity participant);
    Task<HandParticipantEntity?> GetParticipantAsync(Guid handId, Guid playerId);

    // Action operations
    Task<ActionEntity> CreateActionAsync(ActionEntity action);
    Task<int> GetActionCountForHandAsync(Guid handId);

    // Community card operations
    Task CreateCommunityCardsAsync(IEnumerable<CommunityCardEntity> cards);

    // Outcome operations
    Task<OutcomeEntity> CreateOutcomeAsync(OutcomeEntity outcome);

    // Save changes
    Task SaveChangesAsync();
}
