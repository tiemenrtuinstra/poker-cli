namespace TexasHoldem.Domain.Enums;

/// <summary>
/// Represents the strength/ranking of poker hands from weakest to strongest.
/// Higher numeric values indicate stronger hands.
/// </summary>
public enum HandStrength
{
    /// <summary>No pair - highest card plays (weakest hand)</summary>
    HighCard = 1,
    
    /// <summary>Two cards of the same rank</summary>
    OnePair = 2,
    
    /// <summary>Two different pairs</summary>
    TwoPair = 3,
    
    /// <summary>Three cards of the same rank</summary>
    ThreeOfAKind = 4,
    
    /// <summary>Five consecutive ranks (any suits)</summary>
    Straight = 5,
    
    /// <summary>Five cards of the same suit (any ranks)</summary>
    Flush = 6,
    
    /// <summary>Three of a kind plus a pair</summary>
    FullHouse = 7,
    
    /// <summary>Four cards of the same rank</summary>
    FourOfAKind = 8,
    
    /// <summary>Five consecutive ranks of the same suit</summary>
    StraightFlush = 9,
    
    /// <summary>A-K-Q-J-10 all of the same suit (strongest hand)</summary>
    RoyalFlush = 10
}