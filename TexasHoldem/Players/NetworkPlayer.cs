using TexasHoldem.Domain;
using TexasHoldem.Domain.Enums;
using TexasHoldem.Network.Messages;
using TexasHoldem.Network.Server;

namespace TexasHoldem.Players;

/// <summary>
/// Represents a remote player connected over the network.
/// Used on the server-side to handle remote player actions.
/// </summary>
public class NetworkPlayer : IPlayer
{
    public string Name { get; private set; }
    public int Chips { get; set; }
    public List<Card> HoleCards { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public bool IsAllIn { get; set; }
    public bool HasFolded { get; set; }
    public PersonalityType? Personality => null; // Network players don't have AI personalities

    public string ClientId { get; }
    public ClientConnection? Connection { get; set; }
    public bool IsConnected => Connection?.State == ConnectionState.Connected;
    public bool IsBotControlled { get; private set; }
    public string? SessionToken { get; set; }

    private PlayerAction? _pendingAction;
    private readonly SemaphoreSlim _actionSemaphore = new(0, 1);
    private readonly object _actionLock = new();
    private readonly int _actionTimeoutMs;
    private readonly Random _random = new();

    public NetworkPlayer(string clientId, string name, int startingChips, int actionTimeoutMs = 60000)
    {
        ClientId = clientId;
        Name = name;
        Chips = startingChips;
        _actionTimeoutMs = actionTimeoutMs;
    }

    /// <summary>
    /// Enable bot control for this player (when disconnected).
    /// </summary>
    public void EnableBotControl()
    {
        IsBotControlled = true;
    }

    /// <summary>
    /// Disable bot control (when player reconnects).
    /// </summary>
    public void DisableBotControl()
    {
        IsBotControlled = false;
    }

    public PlayerAction TakeTurn(GameState gameState)
    {
        // If bot controlled, use basic AI logic
        if (IsBotControlled)
        {
            return MakeBotDecision(gameState);
        }

        // Clear any pending action
        lock (_actionLock)
        {
            _pendingAction = null;
        }

        // Reset semaphore
        while (_actionSemaphore.CurrentCount > 0)
        {
            _actionSemaphore.Wait(0);
        }

        // Wait for the action with timeout
        var gotAction = _actionSemaphore.Wait(_actionTimeoutMs);

        if (!gotAction)
        {
            // Timeout - auto-fold or check
            var callAmount = Math.Max(0, gameState.CurrentBet - gameState.GetPlayerBetThisRound(this));
            if (callAmount == 0)
            {
                return new PlayerAction
                {
                    PlayerId = Name,
                    Action = ActionType.Check,
                    Amount = 0,
                    Timestamp = DateTime.Now,
                    BettingPhase = gameState.BettingPhase
                };
            }
            else
            {
                return new PlayerAction
                {
                    PlayerId = Name,
                    Action = ActionType.Fold,
                    Amount = 0,
                    Timestamp = DateTime.Now,
                    BettingPhase = gameState.BettingPhase
                };
            }
        }

        lock (_actionLock)
        {
            if (_pendingAction != null)
            {
                return _pendingAction;
            }
        }

        // Fallback - should never reach here
        return new PlayerAction
        {
            PlayerId = Name,
            Action = ActionType.Fold,
            Amount = 0,
            Timestamp = DateTime.Now,
            BettingPhase = gameState.BettingPhase
        };
    }

    /// <summary>
    /// Makes a basic AI decision when the player is disconnected.
    /// Uses conservative play to preserve the player's chips.
    /// </summary>
    private PlayerAction MakeBotDecision(GameState gameState)
    {
        // Add thinking delay for realism
        Thread.Sleep(_random.Next(1000, 2000));

        var validActions = GetValidActions(gameState);
        var callAmount = Math.Max(0, gameState.CurrentBet - gameState.GetPlayerBetThisRound(this));

        // Conservative bot: check when possible, call small bets, fold to large bets
        ActionType action;
        int amount = 0;

        if (validActions.Contains(ActionType.Check))
        {
            action = ActionType.Check;
        }
        else if (callAmount <= gameState.BigBlindAmount * 2 && validActions.Contains(ActionType.Call))
        {
            // Call small bets (up to 2x big blind)
            action = ActionType.Call;
            amount = callAmount;
        }
        else if (callAmount <= Chips * 0.1 && validActions.Contains(ActionType.Call))
        {
            // Call if it's less than 10% of our stack
            action = ActionType.Call;
            amount = callAmount;
        }
        else
        {
            // Fold to larger bets
            action = ActionType.Fold;
        }

        return new PlayerAction
        {
            PlayerId = Name,
            Action = action,
            Amount = amount,
            Timestamp = DateTime.Now,
            BettingPhase = gameState.BettingPhase
        };
    }

    /// <summary>
    /// Called when an action is received from the remote client.
    /// </summary>
    public void ReceiveAction(ActionType action, int amount, BettingPhase bettingPhase)
    {
        lock (_actionLock)
        {
            _pendingAction = new PlayerAction
            {
                PlayerId = Name,
                Action = action,
                Amount = amount,
                Timestamp = DateTime.Now,
                BettingPhase = bettingPhase
            };
        }

        try
        {
            _actionSemaphore.Release();
        }
        catch (SemaphoreFullException)
        {
            // Already released, ignore
        }
    }

    /// <summary>
    /// Get the request message to send to the client asking for their action.
    /// </summary>
    public ActionRequestMessage CreateActionRequest(GameState gameState)
    {
        var validActions = GetValidActions(gameState);
        var callAmount = Math.Max(0, gameState.CurrentBet - gameState.GetPlayerBetThisRound(this));
        var minBet = gameState.BigBlindAmount;
        var maxBet = Chips;

        return new ActionRequestMessage
        {
            PlayerId = ClientId,
            ValidActions = validActions,
            MinBet = minBet,
            MaxBet = maxBet,
            AmountToCall = callAmount,
            TimeoutMs = _actionTimeoutMs
        };
    }

    private List<ActionType> GetValidActions(GameState gameState)
    {
        var actions = new List<ActionType>();

        if (!HasFolded)
        {
            actions.Add(ActionType.Fold);
        }

        if (IsAllIn || Chips <= 0)
        {
            return actions;
        }

        var callAmount = Math.Max(0, gameState.CurrentBet - gameState.GetPlayerBetThisRound(this));

        if (callAmount == 0)
        {
            actions.Add(ActionType.Check);
        }

        if (callAmount > 0 && callAmount <= Chips)
        {
            actions.Add(ActionType.Call);
        }

        if (gameState.CurrentBet == 0 && Chips > 0)
        {
            actions.Add(ActionType.Bet);
        }

        if (gameState.CurrentBet > 0 && Chips > callAmount)
        {
            actions.Add(ActionType.Raise);
        }

        if (Chips > 0)
        {
            actions.Add(ActionType.AllIn);
        }

        return actions;
    }

    public void ReceiveCards(List<Card> cards)
    {
        HoleCards.Clear();
        HoleCards.AddRange(cards);
    }

    public void AddChips(int amount)
    {
        Chips += amount;
    }

    public bool RemoveChips(int amount)
    {
        if (amount > Chips) return false;
        Chips -= amount;
        return true;
    }

    public void Reset()
    {
        HoleCards.Clear();
        IsAllIn = false;
        HasFolded = false;
        IsActive = Chips > 0;

        // Clear any pending action
        lock (_actionLock)
        {
            _pendingAction = null;
        }
        while (_actionSemaphore.CurrentCount > 0)
        {
            _actionSemaphore.Wait(0);
        }
    }

    public void ResetForNewGame()
    {
        Reset();
        // Network players have no stats to reset
    }

    public void ShowCards()
    {
        // Network players' cards are shown via network messages
    }

    public void HideCards()
    {
        // Network players' cards visibility is managed via network messages
    }

    public void UpdateName(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Convert player state to network-serializable format.
    /// </summary>
    public NetworkPlayerInfo ToNetworkPlayerInfo(bool includeHoleCards = false)
    {
        return new NetworkPlayerInfo
        {
            Id = ClientId,
            Name = Name,
            Chips = Chips,
            CurrentBet = 0, // Will be set from GameState
            IsActive = IsActive,
            IsAllIn = IsAllIn,
            HasFolded = HasFolded,
            IsConnected = IsConnected,
            HoleCards = includeHoleCards ? HoleCards.Select(c => c.GetDisplayString()).ToList() : null
        };
    }

    public override string ToString()
    {
        var status = IsConnected ? "Connected" : "Disconnected";
        return $"{Name} ({status}) - €{Chips}";
    }
}
