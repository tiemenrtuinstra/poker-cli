using Microsoft.EntityFrameworkCore;
using TexasHoldem.Data.Entities;

namespace TexasHoldem.Data.Repositories;

public class GameLogRepository : IGameLogRepository
{
    private readonly GameLogDbContext _context;

    public GameLogRepository(GameLogDbContext context)
    {
        _context = context;
    }

    // Session operations
    public async Task<SessionEntity> CreateSessionAsync(SessionEntity session)
    {
        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();
        return session;
    }

    public async Task UpdateSessionAsync(SessionEntity session)
    {
        _context.Sessions.Update(session);
        await _context.SaveChangesAsync();
    }

    public async Task<SessionEntity?> GetSessionAsync(Guid sessionId)
    {
        return await _context.Sessions.FindAsync(sessionId);
    }

    // Player operations
    public async Task<PlayerEntity?> GetPlayerByNameAsync(string name)
    {
        return await _context.Players.FirstOrDefaultAsync(p => p.Name == name);
    }

    public async Task<PlayerEntity> GetOrCreatePlayerAsync(string name, string playerType, string? personality, string? aiProvider)
    {
        var player = await GetPlayerByNameAsync(name);
        if (player != null)
        {
            // Update player info if changed
            if (player.PlayerType != playerType || player.Personality != personality || player.AiProvider != aiProvider)
            {
                player.PlayerType = playerType;
                player.Personality = personality;
                player.AiProvider = aiProvider;
                await _context.SaveChangesAsync();
            }
            return player;
        }

        player = new PlayerEntity
        {
            Id = Guid.NewGuid(),
            Name = name,
            PlayerType = playerType,
            Personality = personality,
            AiProvider = aiProvider
        };

        _context.Players.Add(player);
        await _context.SaveChangesAsync();
        return player;
    }

    // Hand operations
    public async Task<HandEntity> CreateHandAsync(HandEntity hand)
    {
        _context.Hands.Add(hand);
        await _context.SaveChangesAsync();
        return hand;
    }

    public async Task UpdateHandAsync(HandEntity hand)
    {
        _context.Hands.Update(hand);
        await _context.SaveChangesAsync();
    }

    public async Task<HandEntity?> GetHandAsync(Guid handId)
    {
        return await _context.Hands.FindAsync(handId);
    }

    public async Task<HandEntity?> GetHandWithDetailsAsync(Guid handId)
    {
        return await _context.Hands
            .Include(h => h.Participants)
                .ThenInclude(p => p.Player)
            .Include(h => h.Actions)
                .ThenInclude(a => a.Player)
            .Include(h => h.CommunityCards)
            .Include(h => h.Outcomes)
                .ThenInclude(o => o.Player)
            .FirstOrDefaultAsync(h => h.Id == handId);
    }

    // Participant operations
    public async Task<HandParticipantEntity> CreateParticipantAsync(HandParticipantEntity participant)
    {
        _context.HandParticipants.Add(participant);
        await _context.SaveChangesAsync();
        return participant;
    }

    public async Task UpdateParticipantAsync(HandParticipantEntity participant)
    {
        _context.HandParticipants.Update(participant);
        await _context.SaveChangesAsync();
    }

    public async Task<HandParticipantEntity?> GetParticipantAsync(Guid handId, Guid playerId)
    {
        return await _context.HandParticipants
            .FirstOrDefaultAsync(p => p.HandId == handId && p.PlayerId == playerId);
    }

    // Action operations
    public async Task<ActionEntity> CreateActionAsync(ActionEntity action)
    {
        _context.Actions.Add(action);
        await _context.SaveChangesAsync();
        return action;
    }

    public async Task<int> GetActionCountForHandAsync(Guid handId)
    {
        return await _context.Actions.CountAsync(a => a.HandId == handId);
    }

    // Community card operations
    public async Task CreateCommunityCardsAsync(IEnumerable<CommunityCardEntity> cards)
    {
        _context.CommunityCards.AddRange(cards);
        await _context.SaveChangesAsync();
    }

    // Outcome operations
    public async Task<OutcomeEntity> CreateOutcomeAsync(OutcomeEntity outcome)
    {
        _context.Outcomes.Add(outcome);
        await _context.SaveChangesAsync();
        return outcome;
    }

    // Save changes
    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
