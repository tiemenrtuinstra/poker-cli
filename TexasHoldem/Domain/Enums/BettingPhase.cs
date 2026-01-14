namespace TexasHoldem.Domain.Enums;

/// <summary>
/// Represents the different phases of betting in a Texas Hold'em hand.
/// </summary>
public enum BettingPhase
{
    /// <summary>First betting round - before community cards are dealt</summary>
    PreFlop,
    
    /// <summary>Second betting round - after first 3 community cards</summary>
    Flop,
    
    /// <summary>Third betting round - after 4th community card</summary>
    Turn,
    
    /// <summary>Final betting round - after 5th community card</summary>
    River,
    
    /// <summary>Reveal and compare hands to determine winner</summary>
    Showdown
}