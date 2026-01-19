namespace TexasHoldem.Game.Events;

/// <summary>
/// Interface for observing game events.
/// Implement this interface to receive notifications about game state changes.
/// </summary>
public interface IGameEventObserver
{
    /// <summary>
    /// Called when a game event occurs.
    /// </summary>
    /// <param name="gameEvent">The event that occurred.</param>
    void OnGameEvent(GameEvent gameEvent);
}

/// <summary>
/// Interface for typed game event observers.
/// Use this when you only want to handle specific event types.
/// </summary>
/// <typeparam name="TEvent">The specific event type to observe.</typeparam>
public interface IGameEventObserver<in TEvent> where TEvent : GameEvent
{
    /// <summary>
    /// Called when a specific game event occurs.
    /// </summary>
    /// <param name="gameEvent">The event that occurred.</param>
    void OnGameEvent(TEvent gameEvent);
}
