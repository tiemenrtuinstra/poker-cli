namespace TexasHoldem.Data.Entities;

public class CommunityCardEntity
{
    public Guid Id { get; set; }
    public Guid HandId { get; set; }
    public required string BettingPhase { get; set; } // Flop, Turn, River
    public required string CardSuit { get; set; } // Hearts, Diamonds, Clubs, Spades
    public required string CardRank { get; set; } // Two through Ace
    public int CardPosition { get; set; } // 0-4 for board position

    // Navigation properties
    public HandEntity? Hand { get; set; }
}
