using TexasHoldem.Game;

namespace TexasHoldem.Network.Chat;

/// <summary>
/// Interface for AI chat participants that can respond to messages and game events.
/// </summary>
public interface IChatParticipant
{
    string PlayerId { get; }
    string PlayerName { get; }

    /// <summary>
    /// Decide whether to respond to a player message and generate a response.
    /// </summary>
    Task<string?> RespondToMessageAsync(string senderName, string message);

    /// <summary>
    /// Generate commentary on a game event.
    /// </summary>
    Task<string?> CommentOnGameEventAsync(GameEventType eventType, string description);

    /// <summary>
    /// Generate commentary on the AI's own action.
    /// </summary>
    Task<string?> CommentOnOwnActionAsync(string actionDescription, GameState gameState);
}

public enum GameEventType
{
    BigPot,
    AllIn,
    BadBeat,
    BluffCalled,
    StraightMade,
    FlushMade,
    FullHouseMade,
    FourOfAKind,
    StraightFlush,
    RoyalFlush,
    BigWin,
    PlayerEliminated
}
