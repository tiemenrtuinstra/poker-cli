using System.ComponentModel.DataAnnotations;

namespace TexasHoldem.Data.Entities;

/// <summary>
/// Default game configuration settings.
/// These are the defaults used when starting a new game.
/// Stored as a single row in the database.
/// </summary>
public class GameDefaultsEntity
{
    [Key]
    public int Id { get; set; } = 1; // Always 1, single row

    // Player Settings
    public int DefaultHumanPlayers { get; set; } = 1;
    public int DefaultAiPlayers { get; set; } = 5;

    // Chip Settings
    public int DefaultStartingChips { get; set; } = 10000;
    public int DefaultSmallBlind { get; set; } = 50;
    public int DefaultBigBlind { get; set; } = 100;
    public int DefaultAnte { get; set; } = 0;

    // Tournament Settings
    public bool EnableBlindIncrease { get; set; } = true;
    public int BlindIncreaseInterval { get; set; } = 10;
    public double BlindIncreaseMultiplier { get; set; } = 1.5;
    public int MaxHands { get; set; } = 0; // 0 = unlimited

    // Rebuy Settings
    public bool AllowRebuys { get; set; } = true;
    public int RebuyAmount { get; set; } = 0; // 0 = same as starting chips

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
