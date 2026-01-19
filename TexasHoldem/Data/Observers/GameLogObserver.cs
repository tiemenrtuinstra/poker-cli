using System.Threading.Channels;
using TexasHoldem.Data.Services;
using TexasHoldem.Game;
using TexasHoldem.Game.Enums;
using TexasHoldem.Game.Events;
using TexasHoldem.Players;

namespace TexasHoldem.Data.Observers;

public class GameLogObserver : IGameEventObserver, IDisposable
{
    private readonly IGameLogService _logService;
    private readonly Channel<Func<Task>> _eventQueue;
    private readonly Task _processorTask;
    private readonly CancellationTokenSource _cts;

    private Guid _currentSessionId;
    private Guid _currentHandId;
    private int _currentPotSize;
    private bool _disposed;

    public GameLogObserver(IGameLogService logService)
    {
        _logService = logService;
        _cts = new CancellationTokenSource();

        // Unbounded channel to avoid blocking game flow
        _eventQueue = Channel.CreateUnbounded<Func<Task>>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        // Start background processor
        _processorTask = ProcessEventsAsync(_cts.Token);
    }

    public async Task InitializeSessionAsync(int startingChips, int smallBlind, int bigBlind)
    {
        _currentSessionId = await _logService.StartSessionAsync(startingChips, smallBlind, bigBlind);
    }

    public void OnGameEvent(GameEvent gameEvent)
    {
        // Enqueue work without blocking
        var work = CreateWork(gameEvent);
        if (work != null)
        {
            _eventQueue.Writer.TryWrite(work);
        }
    }

    private Func<Task>? CreateWork(GameEvent gameEvent)
    {
        return gameEvent switch
        {
            HandStartedEvent e => () => HandleHandStartedAsync(e),
            HoleCardsDealtEvent e => () => HandleHoleCardsDealtAsync(e),
            CommunityCardsRevealedEvent e => () => HandleCommunityCardsAsync(e),
            BettingRoundStartedEvent e => () => HandleBettingRoundStartedAsync(e),
            PlayerActionEvent e => () => HandlePlayerActionAsync(e),
            PotWonEvent e => () => HandlePotWonAsync(e),
            HandEndedEvent e => () => HandleHandEndedAsync(e),
            GameEndedEvent e => () => HandleGameEndedAsync(e),
            _ => null
        };
    }

    private async Task HandleHandStartedAsync(HandStartedEvent e)
    {
        if (_currentSessionId == Guid.Empty) return;

        _currentHandId = await _logService.StartHandAsync(
            _currentSessionId,
            e.HandNumber,
            e.DealerPosition,
            e.SmallBlind,
            e.BigBlind,
            e.Players
        );
        _currentPotSize = 0;
    }

    private async Task HandleHoleCardsDealtAsync(HoleCardsDealtEvent e)
    {
        if (_currentHandId == Guid.Empty) return;

        await _logService.UpdateParticipantHoleCardsAsync(_currentHandId, e.Player, e.Cards);
    }

    private async Task HandleCommunityCardsAsync(CommunityCardsRevealedEvent e)
    {
        if (_currentHandId == Guid.Empty) return;

        await _logService.LogCommunityCardsAsync(_currentHandId, e.Phase, e.NewCards);
    }

    private Task HandleBettingRoundStartedAsync(BettingRoundStartedEvent e)
    {
        _currentPotSize = e.PotSize;
        return Task.CompletedTask;
    }

    private async Task HandlePlayerActionAsync(PlayerActionEvent e)
    {
        if (_currentHandId == Guid.Empty) return;

        await _logService.LogActionAsync(
            _currentHandId,
            e.Player,
            e.Action,
            e.Amount,
            e.Phase,
            _currentPotSize
        );

        // Update pot size estimate (not exact but good enough for logging)
        if (e.Action != ActionType.Fold)
        {
            _currentPotSize += e.Amount;
        }
    }

    private async Task HandlePotWonAsync(PotWonEvent e)
    {
        if (_currentHandId == Guid.Empty) return;

        var handDescription = e.WinningHand;
        string? handStrength = null;

        // Parse hand strength from description if available
        if (!string.IsNullOrEmpty(handDescription))
        {
            handStrength = ParseHandStrength(handDescription);
        }

        await _logService.LogOutcomeAsync(
            _currentHandId,
            e.Winner,
            e.Amount,
            "Main", // Simplified - real implementation could track side pots
            handStrength,
            handDescription,
            !e.IsShowdown
        );
    }

    private async Task HandleHandEndedAsync(HandEndedEvent e)
    {
        if (_currentHandId == Guid.Empty) return;

        var totalPot = e.Winners.Sum(w => w.Amount);
        var wentToShowdown = e.Winners.Any(w => !string.IsNullOrEmpty(w.HandDescription) && w.HandDescription != "Everyone else folded");

        await _logService.EndHandAsync(_currentHandId, totalPot, wentToShowdown);

        // Update participant final status
        foreach (var winner in e.Winners)
        {
            await _logService.UpdateParticipantEndingChipsAsync(
                _currentHandId,
                winner.Player,
                winner.Player.Chips,
                "Won"
            );
        }

        foreach (var player in e.RemainingPlayers)
        {
            if (!e.Winners.Any(w => w.Player.Name == player.Name))
            {
                var status = player.HasFolded ? "Folded" : (player.IsAllIn ? "AllIn" : "Lost");
                await _logService.UpdateParticipantEndingChipsAsync(
                    _currentHandId,
                    player,
                    player.Chips,
                    status
                );
            }
        }

        _currentHandId = Guid.Empty;
    }

    private async Task HandleGameEndedAsync(GameEndedEvent e)
    {
        if (_currentSessionId == Guid.Empty) return;

        await _logService.EndSessionAsync(_currentSessionId, e.Winner);
        _currentSessionId = Guid.Empty;
    }

    private static string? ParseHandStrength(string description)
    {
        if (string.IsNullOrEmpty(description)) return null;

        var lowered = description.ToLowerInvariant();
        if (lowered.Contains("royal flush")) return "RoyalFlush";
        if (lowered.Contains("straight flush")) return "StraightFlush";
        if (lowered.Contains("four of a kind")) return "FourOfAKind";
        if (lowered.Contains("full house")) return "FullHouse";
        if (lowered.Contains("flush")) return "Flush";
        if (lowered.Contains("straight")) return "Straight";
        if (lowered.Contains("three of a kind")) return "ThreeOfAKind";
        if (lowered.Contains("two pair")) return "TwoPair";
        if (lowered.Contains("pair")) return "Pair";
        if (lowered.Contains("high card")) return "HighCard";

        return null;
    }

    private async Task ProcessEventsAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var work in _eventQueue.Reader.ReadAllAsync(ct))
            {
                try
                {
                    await work();
                }
                catch (Exception ex)
                {
                    // Log error but continue processing
                    Console.Error.WriteLine($"[GameLogObserver] Error processing event: {ex.Message}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown
        }
    }

    public async Task FlushAsync()
    {
        // Complete the writer and wait for all items to be processed
        _eventQueue.Writer.Complete();
        await _processorTask;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _cts.Cancel();
        _eventQueue.Writer.Complete();

        try
        {
            _processorTask.Wait(TimeSpan.FromSeconds(5));
        }
        catch
        {
            // Ignore timeout
        }

        _cts.Dispose();
    }
}
