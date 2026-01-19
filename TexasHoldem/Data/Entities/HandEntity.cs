namespace TexasHoldem.Data.Entities;

public class HandEntity
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public int HandNumber { get; set; }
    public int DealerPosition { get; set; }
    public int SmallBlind { get; set; }
    public int BigBlind { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int FinalPotSize { get; set; }
    public bool WentToShowdown { get; set; }

    // Navigation properties
    public SessionEntity? Session { get; set; }
    public ICollection<HandParticipantEntity> Participants { get; set; } = new List<HandParticipantEntity>();
    public ICollection<ActionEntity> Actions { get; set; } = new List<ActionEntity>();
    public ICollection<CommunityCardEntity> CommunityCards { get; set; } = new List<CommunityCardEntity>();
    public ICollection<OutcomeEntity> Outcomes { get; set; } = new List<OutcomeEntity>();
}
