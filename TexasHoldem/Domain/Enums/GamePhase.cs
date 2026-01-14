namespace TexasHoldem.Domain.Enums;

/// <summary>
/// Represents the overall phases of a poker game from setup to completion.
/// Used to track the current state of the entire game session.
/// </summary>
public enum GamePhase
{
    /// <summary>Game initialization and configuration phase</summary>
    Setup,
    
    /// <summary>Pre-flop betting phase - hole cards dealt</summary>
    PreFlop,
    
    /// <summary>Flop phase - first 3 community cards revealed</summary>
    Flop,
    
    /// <summary>Turn phase - 4th community card revealed</summary>
    Turn,
    
    /// <summary>River phase - 5th community card revealed</summary>
    River,
    
    /// <summary>Showdown phase - revealing and comparing hands</summary>
    Showdown,
    
    /// <summary>Hand completed - preparing for next hand</summary>
    HandComplete
}