namespace TexasHoldem.Data.Entities;

public class HandParticipantEntity
{
    public Guid Id { get; set; }
    public Guid HandId { get; set; }
    public Guid PlayerId { get; set; }
    public int SeatPosition { get; set; }
    public int StartingChips { get; set; }
    public int EndingChips { get; set; }
    public string? HoleCards { get; set; } // Serialized as "Ah,Kh"
    public required string FinalStatus { get; set; } // Won, Lost, Folded, AllIn

    // Navigation properties
    public HandEntity? Hand { get; set; }
    public PlayerEntity? Player { get; set; }
}
