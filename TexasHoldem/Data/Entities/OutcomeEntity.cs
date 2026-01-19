namespace TexasHoldem.Data.Entities;

public class OutcomeEntity
{
    public Guid Id { get; set; }
    public Guid HandId { get; set; }
    public Guid PlayerId { get; set; }
    public int Amount { get; set; }
    public required string PotType { get; set; } // Main, Side
    public string? HandStrength { get; set; } // e.g., "Pair", "Flush", "FullHouse"
    public string? HandDescription { get; set; } // e.g., "Pair of Aces with King kicker"
    public bool WonByFold { get; set; }

    // Navigation properties
    public HandEntity? Hand { get; set; }
    public PlayerEntity? Player { get; set; }
}
