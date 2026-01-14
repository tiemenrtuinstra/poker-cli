namespace TexasHoldem.Domain.Enums;

/// <summary>
/// Represents the different actions a player can take during a betting round.
/// </summary>
public enum ActionType
{
    /// <summary>Player folds their hand and exits the current hand</summary>
    Fold,
    
    /// <summary>Player checks (no bet required, passes action)</summary>
    Check,
    
    /// <summary>Player calls the current bet amount</summary>
    Call,
    
    /// <summary>Player makes an initial bet</summary>
    Bet,
    
    /// <summary>Player raises the current bet</summary>
    Raise,
    
    /// <summary>Player bets all remaining chips</summary>
    AllIn
}