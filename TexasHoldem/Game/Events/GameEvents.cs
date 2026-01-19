using TexasHoldem.Game.Enums;
using TexasHoldem.Players;

namespace TexasHoldem.Game.Events;

/// <summary>
/// Base record for all game events.
/// </summary>
public abstract record GameEvent
{
    public DateTime Timestamp { get; init; } = DateTime.Now;
}

/// <summary>
/// Raised when a new hand begins.
/// </summary>
public record HandStartedEvent(
    int HandNumber,
    IReadOnlyList<IPlayer> Players,
    int DealerPosition,
    int SmallBlind,
    int BigBlind
) : GameEvent;

/// <summary>
/// Raised when hole cards are dealt to a player.
/// </summary>
public record HoleCardsDealtEvent(
    IPlayer Player,
    IReadOnlyList<Card> Cards
) : GameEvent;

/// <summary>
/// Raised when community cards are revealed.
/// </summary>
public record CommunityCardsRevealedEvent(
    BettingPhase Phase,
    IReadOnlyList<Card> NewCards,
    IReadOnlyList<Card> AllCommunityCards
) : GameEvent;

/// <summary>
/// Raised when a betting round begins.
/// </summary>
public record BettingRoundStartedEvent(
    BettingPhase Phase,
    int PotSize
) : GameEvent;

/// <summary>
/// Raised when a player takes an action.
/// </summary>
public record PlayerActionEvent(
    IPlayer Player,
    ActionType Action,
    int Amount,
    BettingPhase Phase
) : GameEvent;

/// <summary>
/// Raised when it's a player's turn to act.
/// </summary>
public record PlayerTurnEvent(
    IPlayer Player,
    int ChipsRemaining
) : GameEvent;

/// <summary>
/// Raised when a betting round completes.
/// </summary>
public record BettingRoundCompletedEvent(
    BettingPhase Phase,
    int PotSize
) : GameEvent;

/// <summary>
/// Raised when a player wins a pot.
/// </summary>
public record PotWonEvent(
    IPlayer Winner,
    int Amount,
    string? WinningHand,
    bool IsShowdown
) : GameEvent;

/// <summary>
/// Raised when a hand ends.
/// </summary>
public record HandEndedEvent(
    int HandNumber,
    IReadOnlyList<PotWinner> Winners,
    IReadOnlyList<IPlayer> RemainingPlayers
) : GameEvent;

/// <summary>
/// Raised when the game ends.
/// </summary>
public record GameEndedEvent(
    IPlayer? Winner,
    int TotalHands,
    string Reason
) : GameEvent;

/// <summary>
/// Raised when blinds are posted.
/// </summary>
public record BlindsPostedEvent(
    IPlayer SmallBlindPlayer,
    int SmallBlindAmount,
    IPlayer BigBlindPlayer,
    int BigBlindAmount
) : GameEvent;

/// <summary>
/// Raised when a player is eliminated (runs out of chips).
/// </summary>
public record PlayerEliminatedEvent(
    IPlayer Player,
    int FinishPosition
) : GameEvent;
