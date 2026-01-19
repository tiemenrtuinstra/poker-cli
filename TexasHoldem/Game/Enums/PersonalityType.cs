namespace TexasHoldem.Game.Enums;

/// <summary>
/// Represents different AI personality types that determine playing style and decision-making.
/// Each personality has distinct behavioral patterns for betting, bluffing, and hand selection.
/// </summary>
public enum PersonalityType
{
    /// <summary>Conservative player - plays few hands but plays them strong</summary>
    Tight,
    
    /// <summary>Plays many hands - more willing to gamble</summary>
    Loose,
    
    /// <summary>Bets and raises frequently - applies pressure</summary>
    Aggressive,
    
    /// <summary>Calls more than betting/raising - less aggressive</summary>
    Passive,
    
    /// <summary>Frequently attempts deception - bets with weak hands</summary>
    Bluffer,
    
    /// <summary>Unpredictable play style - mixed strategies</summary>
    Random,
    
    /// <summary>Weak player - makes poor decisions, calls too much</summary>
    Fish,
    
    /// <summary>Expert player - strong strategic decisions</summary>
    Shark,
    
    /// <summary>Calls almost everything - rarely folds or raises</summary>
    CallingStation,
    
    /// <summary>Extremely aggressive - bets and raises wildly</summary>
    Maniac,
    
    /// <summary>Ultra-conservative - only plays premium hands</summary>
    Nit
}