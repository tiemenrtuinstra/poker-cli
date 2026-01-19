namespace TexasHoldem.Data.Entities;

public class SessionEntity
{
    public Guid Id { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int StartingChips { get; set; }
    public int SmallBlind { get; set; }
    public int BigBlind { get; set; }
    public Guid? WinnerId { get; set; }

    // Navigation properties
    public PlayerEntity? Winner { get; set; }
    public ICollection<HandEntity> Hands { get; set; } = new List<HandEntity>();
}
