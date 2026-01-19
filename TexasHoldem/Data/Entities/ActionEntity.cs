namespace TexasHoldem.Data.Entities;

public class ActionEntity
{
    public Guid Id { get; set; }
    public Guid HandId { get; set; }
    public Guid PlayerId { get; set; }
    public required string BettingPhase { get; set; } // PreFlop, Flop, Turn, River
    public required string ActionType { get; set; } // Fold, Check, Call, Bet, Raise, AllIn
    public int Amount { get; set; }
    public int PotSizeAtAction { get; set; }
    public int ActionOrder { get; set; }
    public DateTime Timestamp { get; set; }

    // Navigation properties
    public HandEntity? Hand { get; set; }
    public PlayerEntity? Player { get; set; }
}
