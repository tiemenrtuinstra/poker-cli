namespace TexasHoldem.Game.Events;

/// <summary>
/// Default implementation of IGameEventPublisher.
/// Manages subscriptions and publishes events to all registered observers.
/// </summary>
public class GameEventPublisher : IGameEventPublisher
{
    private readonly List<IGameEventObserver> _observers = [];
    private readonly object _lock = new();

    /// <inheritdoc />
    public void Subscribe(IGameEventObserver observer)
    {
        lock (_lock)
        {
            if (!_observers.Contains(observer))
            {
                _observers.Add(observer);
            }
        }
    }

    /// <inheritdoc />
    public void Unsubscribe(IGameEventObserver observer)
    {
        lock (_lock)
        {
            _observers.Remove(observer);
        }
    }

    /// <inheritdoc />
    public void Publish(GameEvent gameEvent)
    {
        List<IGameEventObserver> observersCopy;

        lock (_lock)
        {
            observersCopy = [.. _observers];
        }

        foreach (var observer in observersCopy)
        {
            try
            {
                observer.OnGameEvent(gameEvent);
            }
            catch (Exception)
            {
                // Observers should not throw, but if they do, don't break the game
                // In production, this would be logged
            }
        }
    }

    /// <summary>
    /// Gets the number of subscribed observers.
    /// </summary>
    public int ObserverCount
    {
        get
        {
            lock (_lock)
            {
                return _observers.Count;
            }
        }
    }
}
