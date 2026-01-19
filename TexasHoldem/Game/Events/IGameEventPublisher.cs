namespace TexasHoldem.Game.Events;

/// <summary>
/// Interface for publishing game events.
/// </summary>
public interface IGameEventPublisher
{
    /// <summary>
    /// Subscribe an observer to receive all game events.
    /// </summary>
    /// <param name="observer">The observer to subscribe.</param>
    void Subscribe(IGameEventObserver observer);

    /// <summary>
    /// Unsubscribe an observer from receiving game events.
    /// </summary>
    /// <param name="observer">The observer to unsubscribe.</param>
    void Unsubscribe(IGameEventObserver observer);

    /// <summary>
    /// Publish an event to all subscribers.
    /// </summary>
    /// <param name="gameEvent">The event to publish.</param>
    void Publish(GameEvent gameEvent);
}
