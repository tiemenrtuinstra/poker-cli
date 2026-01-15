using System.Collections.Concurrent;
using TexasHoldem.Network.Messages;

namespace TexasHoldem.Network.Server;

/// <summary>
/// Manages player disconnections, reconnections, and bot takeover.
/// </summary>
public class ReconnectionManager
{
    private readonly PokerServer _server;
    private readonly ConcurrentDictionary<string, DisconnectedPlayerInfo> _disconnectedPlayers = new();
    private readonly ConcurrentDictionary<string, string> _sessionToClientMap = new(); // sessionToken -> clientId
    private readonly TimeSpan _reconnectWindow;
    private readonly TimeSpan _botTakeoverDelay;
    private readonly CancellationTokenSource _cts = new();

    public event Action<string, string>? OnPlayerDisconnected; // clientId, playerName
    public event Action<string, string>? OnPlayerReconnected; // clientId, playerName
    public event Action<string, string>? OnBotTakeover; // clientId, playerName

    public ReconnectionManager(
        PokerServer server,
        TimeSpan? reconnectWindow = null,
        TimeSpan? botTakeoverDelay = null)
    {
        _server = server;
        _reconnectWindow = reconnectWindow ?? TimeSpan.FromMinutes(5);
        _botTakeoverDelay = botTakeoverDelay ?? TimeSpan.FromSeconds(30);

        _server.OnClientConnected += HandleClientConnected;
        _server.OnClientDisconnected += HandleClientDisconnected;
        _server.OnMessageReceived += HandleMessage;

        // Start cleanup task
        _ = CleanupExpiredSessionsAsync(_cts.Token);
    }

    private void HandleClientConnected(ClientConnection client)
    {
        // Track session token
        if (!string.IsNullOrEmpty(client.SessionToken))
        {
            _sessionToClientMap[client.SessionToken] = client.Id;
        }
    }

    private void HandleClientDisconnected(ClientConnection client)
    {
        if (string.IsNullOrEmpty(client.SessionToken))
            return;

        // Store disconnected player info for potential reconnection
        var info = new DisconnectedPlayerInfo
        {
            ClientId = client.Id,
            PlayerName = client.PlayerName,
            SessionToken = client.SessionToken,
            LobbyId = client.CurrentLobbyId,
            DisconnectedAt = DateTime.UtcNow,
            BotTakeoverScheduled = false
        };

        _disconnectedPlayers[client.SessionToken] = info;

        OnPlayerDisconnected?.Invoke(client.Id, client.PlayerName);

        // Schedule bot takeover
        _ = ScheduleBotTakeoverAsync(client.SessionToken);
    }

    private void HandleMessage(string clientId, INetworkMessage message)
    {
        if (message is ReconnectMessage reconnect)
        {
            Task.Run(async () => await HandleReconnectAsync(clientId, reconnect));
        }
    }

    private async Task HandleReconnectAsync(string clientId, ReconnectMessage message)
    {
        var client = _server.GetClient(clientId);
        if (client == null) return;

        // Check if we have info for this session
        if (!_disconnectedPlayers.TryRemove(message.SessionToken, out var info))
        {
            await _server.SendToClientAsync(clientId, new ReconnectResponseMessage
            {
                Success = false,
                Error = "Session expired or not found"
            });
            return;
        }

        // Check if session is still valid
        if (DateTime.UtcNow - info.DisconnectedAt > _reconnectWindow)
        {
            await _server.SendToClientAsync(clientId, new ReconnectResponseMessage
            {
                Success = false,
                Error = "Reconnection window expired"
            });
            return;
        }

        // Update client with restored info
        client.PlayerName = info.PlayerName;
        client.CurrentLobbyId = info.LobbyId;

        // Update session mapping
        _sessionToClientMap[message.SessionToken] = clientId;

        // Get current lobby and game state if applicable
        LobbyInfo? lobbyInfo = null;
        NetworkGameState? gameState = null;

        // Note: The actual lobby and game state restoration would be handled
        // by the LobbyManager and NetworkGameManager which should listen
        // to the OnPlayerReconnected event

        await _server.SendToClientAsync(clientId, new ReconnectResponseMessage
        {
            Success = true,
            ClientId = clientId,
            LobbyCode = info.LobbyId,
            Lobby = lobbyInfo,
            GameState = gameState
        });

        OnPlayerReconnected?.Invoke(clientId, info.PlayerName);

        // Broadcast reconnection to lobby
        if (!string.IsNullOrEmpty(info.LobbyId))
        {
            await _server.BroadcastToLobbyAsync(info.LobbyId, new PlayerReconnectedMessage
            {
                PlayerId = clientId,
                PlayerName = info.PlayerName
            }, excludeClientId: clientId);
        }
    }

    private async Task ScheduleBotTakeoverAsync(string sessionToken)
    {
        await Task.Delay(_botTakeoverDelay);

        // Check if player reconnected
        if (!_disconnectedPlayers.TryGetValue(sessionToken, out var info))
            return; // Player reconnected

        if (info.BotTakeoverScheduled)
            return; // Already taken over

        info.BotTakeoverScheduled = true;

        OnBotTakeover?.Invoke(info.ClientId, info.PlayerName);

        // Broadcast bot takeover to lobby
        if (!string.IsNullOrEmpty(info.LobbyId))
        {
            await _server.BroadcastToLobbyAsync(info.LobbyId, new PlayerDisconnectedMessage
            {
                PlayerId = info.ClientId,
                PlayerName = info.PlayerName,
                BotTakeover = true
            });
        }
    }

    private async Task CleanupExpiredSessionsAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(1), ct);

            var expiredSessions = _disconnectedPlayers
                .Where(kvp => DateTime.UtcNow - kvp.Value.DisconnectedAt > _reconnectWindow)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var sessionToken in expiredSessions)
            {
                _disconnectedPlayers.TryRemove(sessionToken, out _);
                _sessionToClientMap.TryRemove(sessionToken, out _);
            }
        }
    }

    /// <summary>
    /// Check if a session token is valid for reconnection.
    /// </summary>
    public bool IsSessionValid(string sessionToken)
    {
        if (_disconnectedPlayers.TryGetValue(sessionToken, out var info))
        {
            return DateTime.UtcNow - info.DisconnectedAt <= _reconnectWindow;
        }
        return false;
    }

    /// <summary>
    /// Get info about a disconnected player by session token.
    /// </summary>
    public DisconnectedPlayerInfo? GetDisconnectedPlayerInfo(string sessionToken)
    {
        _disconnectedPlayers.TryGetValue(sessionToken, out var info);
        return info;
    }

    /// <summary>
    /// Check if a player has been taken over by a bot.
    /// </summary>
    public bool HasBotTakenOver(string sessionToken)
    {
        if (_disconnectedPlayers.TryGetValue(sessionToken, out var info))
        {
            return info.BotTakeoverScheduled;
        }
        return false;
    }

    /// <summary>
    /// Cancel bot takeover if player reconnects in time.
    /// </summary>
    public void CancelBotTakeover(string sessionToken)
    {
        _disconnectedPlayers.TryRemove(sessionToken, out _);
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}

public class DisconnectedPlayerInfo
{
    public required string ClientId { get; init; }
    public required string PlayerName { get; init; }
    public required string SessionToken { get; init; }
    public string? LobbyId { get; init; }
    public DateTime DisconnectedAt { get; init; }
    public bool BotTakeoverScheduled { get; set; }
}
