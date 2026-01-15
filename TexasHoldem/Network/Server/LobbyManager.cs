using System.Collections.Concurrent;
using TexasHoldem.Network.Messages;

namespace TexasHoldem.Network.Server;

public class LobbyManager
{
    private readonly ConcurrentDictionary<string, Lobby> _lobbies = new();
    private readonly ConcurrentDictionary<string, string> _clientLobbyMap = new(); // clientId -> lobbyCode
    private readonly ConcurrentDictionary<string, DisconnectedLobbyPlayer> _disconnectedPlayers = new(); // sessionToken -> player info
    private readonly PokerServer _server;
    private readonly ReconnectionManager? _reconnectionManager;
    private readonly Random _random = new();

    public event Action<Lobby>? OnLobbyCreated;
    public event Action<Lobby>? OnLobbyDeleted;
    public event Action<Lobby, LobbyPlayer>? OnPlayerJoined;
    public event Action<Lobby, LobbyPlayer>? OnPlayerLeft;
    public event Action<Lobby>? OnLobbyStarted;
    public event Action<Lobby, string>? OnHostMigrated; // lobby, newHostId

    public LobbyManager(PokerServer server, ReconnectionManager? reconnectionManager = null)
    {
        _server = server;
        _reconnectionManager = reconnectionManager;
        _server.OnMessageReceived += HandleMessage;
        _server.OnClientDisconnected += HandleClientDisconnected;

        // Subscribe to reconnection events
        if (_reconnectionManager != null)
        {
            _reconnectionManager.OnPlayerReconnected += HandlePlayerReconnected;
            _reconnectionManager.OnBotTakeover += HandleBotTakeover;
        }
    }

    private void HandleMessage(string clientId, INetworkMessage message)
    {
        Task.Run(async () =>
        {
            try
            {
                switch (message)
                {
                    case CreateLobbyMessage createLobby:
                        await HandleCreateLobbyAsync(clientId, createLobby);
                        break;
                    case JoinLobbyMessage joinLobby:
                        await HandleJoinLobbyAsync(clientId, joinLobby);
                        break;
                    case LeaveLobbyMessage:
                        await HandleLeaveLobbyAsync(clientId);
                        break;
                    case PlayerReadyMessage ready:
                        await HandlePlayerReadyAsync(clientId, ready);
                        break;
                    case StartGameMessage:
                        await HandleStartGameAsync(clientId);
                        break;
                    case ListLobbiesMessage:
                        await HandleListLobbiesAsync(clientId);
                        break;
                    case KickPlayerMessage kick:
                        await HandleKickPlayerAsync(clientId, kick);
                        break;
                    case TransferHostMessage transfer:
                        await HandleTransferHostAsync(clientId, transfer);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling message from {clientId}: {ex.Message}");
            }
        });
    }

    private async Task HandleCreateLobbyAsync(string clientId, CreateLobbyMessage message)
    {
        var client = _server.GetClient(clientId);
        if (client == null) return;

        // Generate unique lobby code
        var code = GenerateLobbyCode();
        while (_lobbies.ContainsKey(code))
        {
            code = GenerateLobbyCode();
        }

        var lobby = new Lobby(code, message.Settings);

        var player = new LobbyPlayer
        {
            Id = clientId,
            Name = client.PlayerName,
            IsReady = false,
            IsHost = true,
            IsAi = false,
            Connection = client
        };

        lobby.AddPlayer(player);
        _lobbies[code] = lobby;
        _clientLobbyMap[clientId] = code;
        client.CurrentLobbyId = code;

        OnLobbyCreated?.Invoke(lobby);

        await _server.SendToClientAsync(clientId, new CreateLobbyResponseMessage
        {
            Success = true,
            LobbyCode = code
        });

        await BroadcastLobbyUpdateAsync(lobby);
    }

    private async Task HandleJoinLobbyAsync(string clientId, JoinLobbyMessage message)
    {
        var client = _server.GetClient(clientId);
        if (client == null) return;

        if (!_lobbies.TryGetValue(message.LobbyCode.ToUpperInvariant(), out var lobby))
        {
            await _server.SendToClientAsync(clientId, new JoinLobbyResponseMessage
            {
                Success = false,
                Error = "Lobby not found"
            });
            return;
        }

        if (lobby.IsFull)
        {
            await _server.SendToClientAsync(clientId, new JoinLobbyResponseMessage
            {
                Success = false,
                Error = "Lobby is full"
            });
            return;
        }

        if (lobby.State != LobbyState.Waiting)
        {
            await _server.SendToClientAsync(clientId, new JoinLobbyResponseMessage
            {
                Success = false,
                Error = "Game already in progress"
            });
            return;
        }

        // Check password if set
        if (!string.IsNullOrEmpty(lobby.Settings.Password) &&
            lobby.Settings.Password != message.Password)
        {
            await _server.SendToClientAsync(clientId, new JoinLobbyResponseMessage
            {
                Success = false,
                Error = "Incorrect password"
            });
            return;
        }

        var player = new LobbyPlayer
        {
            Id = clientId,
            Name = client.PlayerName,
            IsReady = false,
            IsHost = false,
            IsAi = false,
            Connection = client
        };

        if (!lobby.AddPlayer(player))
        {
            await _server.SendToClientAsync(clientId, new JoinLobbyResponseMessage
            {
                Success = false,
                Error = "Failed to join lobby"
            });
            return;
        }

        _clientLobbyMap[clientId] = lobby.Code;
        client.CurrentLobbyId = lobby.Code;

        OnPlayerJoined?.Invoke(lobby, player);

        await _server.SendToClientAsync(clientId, new JoinLobbyResponseMessage
        {
            Success = true,
            Lobby = lobby.ToLobbyInfo()
        });

        await BroadcastLobbyUpdateAsync(lobby);
    }

    private async Task HandleLeaveLobbyAsync(string clientId)
    {
        if (!_clientLobbyMap.TryRemove(clientId, out var lobbyCode))
            return;

        if (!_lobbies.TryGetValue(lobbyCode, out var lobby))
            return;

        var player = lobby.GetPlayer(clientId);
        if (player == null) return;

        lobby.RemovePlayer(clientId);

        var client = _server.GetClient(clientId);
        if (client != null)
        {
            client.CurrentLobbyId = null;
        }

        OnPlayerLeft?.Invoke(lobby, player);

        if (lobby.IsEmpty)
        {
            _lobbies.TryRemove(lobbyCode, out _);
            OnLobbyDeleted?.Invoke(lobby);
        }
        else
        {
            await BroadcastLobbyUpdateAsync(lobby);
        }
    }

    private async Task HandlePlayerReadyAsync(string clientId, PlayerReadyMessage message)
    {
        if (!_clientLobbyMap.TryGetValue(clientId, out var lobbyCode))
            return;

        if (!_lobbies.TryGetValue(lobbyCode, out var lobby))
            return;

        lobby.SetPlayerReady(clientId, message.IsReady);
        await BroadcastLobbyUpdateAsync(lobby);
    }

    private async Task HandleStartGameAsync(string clientId)
    {
        if (!_clientLobbyMap.TryGetValue(clientId, out var lobbyCode))
            return;

        if (!_lobbies.TryGetValue(lobbyCode, out var lobby))
            return;

        // Only host can start
        if (lobby.HostId != clientId)
        {
            await _server.SendToClientAsync(clientId, new ErrorMessage
            {
                Code = "NOT_HOST",
                Message = "Only the host can start the game"
            });
            return;
        }

        if (!lobby.CanStart())
        {
            await _server.SendToClientAsync(clientId, new ErrorMessage
            {
                Code = "CANNOT_START",
                Message = "Not all players are ready or not enough players"
            });
            return;
        }

        lobby.State = LobbyState.Starting;
        await BroadcastLobbyUpdateAsync(lobby);

        OnLobbyStarted?.Invoke(lobby);
    }

    private async Task HandleListLobbiesAsync(string clientId)
    {
        var publicLobbies = _lobbies.Values
            .Where(l => l.Settings.IsPublic && l.State == LobbyState.Waiting && !l.IsFull)
            .Select(l => l.ToLobbyInfo())
            .ToList();

        await _server.SendToClientAsync(clientId, new ListLobbiesResponseMessage
        {
            Lobbies = publicLobbies
        });
    }

    private async Task HandleKickPlayerAsync(string clientId, KickPlayerMessage message)
    {
        if (!_clientLobbyMap.TryGetValue(clientId, out var lobbyCode))
            return;

        if (!_lobbies.TryGetValue(lobbyCode, out var lobby))
            return;

        // Only host can kick
        if (lobby.HostId != clientId)
        {
            await _server.SendToClientAsync(clientId, new ErrorMessage
            {
                Code = "NOT_HOST",
                Message = "Only the host can kick players"
            });
            return;
        }

        // Cannot kick self
        if (message.PlayerId == clientId)
            return;

        var targetPlayer = lobby.GetPlayer(message.PlayerId);
        if (targetPlayer == null) return;

        lobby.RemovePlayer(message.PlayerId);
        _clientLobbyMap.TryRemove(message.PlayerId, out _);

        // Disconnect the kicked player
        await _server.DisconnectClientAsync(message.PlayerId, message.Reason ?? "Kicked by host");

        await BroadcastLobbyUpdateAsync(lobby);
    }

    private async Task HandleTransferHostAsync(string clientId, TransferHostMessage message)
    {
        if (!_clientLobbyMap.TryGetValue(clientId, out var lobbyCode))
            return;

        if (!_lobbies.TryGetValue(lobbyCode, out var lobby))
            return;

        // Only host can transfer
        if (lobby.HostId != clientId)
        {
            await _server.SendToClientAsync(clientId, new ErrorMessage
            {
                Code = "NOT_HOST",
                Message = "Only the host can transfer host"
            });
            return;
        }

        if (lobby.TransferHost(message.NewHostId))
        {
            await BroadcastLobbyUpdateAsync(lobby);
        }
    }

    private void HandleClientDisconnected(ClientConnection client)
    {
        // If reconnection manager exists, don't remove player immediately
        // Just mark them as disconnected in the lobby
        if (_reconnectionManager != null && !string.IsNullOrEmpty(client.SessionToken))
        {
            Task.Run(() => HandlePlayerTemporarilyDisconnectedAsync(client));
        }
        else
        {
            // No reconnection support, remove player immediately
            Task.Run(() => HandleLeaveLobbyAsync(client.Id));
        }
    }

    private async Task HandlePlayerTemporarilyDisconnectedAsync(ClientConnection client)
    {
        if (!_clientLobbyMap.TryGetValue(client.Id, out var lobbyCode))
            return;

        if (!_lobbies.TryGetValue(lobbyCode, out var lobby))
            return;

        var player = lobby.GetPlayer(client.Id);
        if (player == null) return;

        // Store disconnected player info for reconnection
        _disconnectedPlayers[client.SessionToken!] = new DisconnectedLobbyPlayer
        {
            ClientId = client.Id,
            PlayerName = player.Name,
            LobbyCode = lobbyCode,
            WasHost = player.IsHost,
            SessionToken = client.SessionToken!
        };

        // If host disconnects, transfer host to another player
        if (player.IsHost)
        {
            var newHost = lobby.Players.Values.FirstOrDefault(p => !p.IsAi && p.Id != client.Id && p.Connection?.State == ConnectionState.Connected);
            if (newHost != null)
            {
                lobby.TransferHost(newHost.Id);
                OnHostMigrated?.Invoke(lobby, newHost.Id);

                // Notify all players about host migration
                await _server.BroadcastToLobbyAsync(lobbyCode, new LobbyUpdateMessage
                {
                    Lobby = lobby.ToLobbyInfo()
                });
            }
        }

        // Broadcast disconnection to lobby
        await _server.BroadcastToLobbyAsync(lobbyCode, new PlayerDisconnectedMessage
        {
            PlayerId = client.Id,
            PlayerName = player.Name,
            BotTakeover = false
        }, excludeClientId: client.Id);
    }

    private void HandlePlayerReconnected(string clientId, string playerName)
    {
        Task.Run(async () => await HandlePlayerReconnectionAsync(clientId, playerName));
    }

    private async Task HandlePlayerReconnectionAsync(string newClientId, string playerName)
    {
        var client = _server.GetClient(newClientId);
        if (client == null || string.IsNullOrEmpty(client.SessionToken)) return;

        // Find disconnected player info
        if (!_disconnectedPlayers.TryRemove(client.SessionToken, out var disconnectedInfo))
            return;

        if (!_lobbies.TryGetValue(disconnectedInfo.LobbyCode, out var lobby))
            return;

        // Update the player's connection in the lobby
        var player = lobby.GetPlayer(disconnectedInfo.ClientId);
        if (player != null)
        {
            // If the client ID changed, we need to update the mapping
            if (disconnectedInfo.ClientId != newClientId)
            {
                _clientLobbyMap.TryRemove(disconnectedInfo.ClientId, out _);
                _clientLobbyMap[newClientId] = disconnectedInfo.LobbyCode;
            }

            player.Connection = client;
            client.CurrentLobbyId = disconnectedInfo.LobbyCode;

            // Notify all players about reconnection
            await _server.BroadcastToLobbyAsync(disconnectedInfo.LobbyCode, new PlayerReconnectedMessage
            {
                PlayerId = newClientId,
                PlayerName = playerName
            }, excludeClientId: newClientId);

            // Send current lobby state to reconnected player
            await _server.SendToClientAsync(newClientId, new LobbyUpdateMessage
            {
                Lobby = lobby.ToLobbyInfo()
            });
        }
    }

    private void HandleBotTakeover(string clientId, string playerName)
    {
        // When bot takes over, we can optionally remove the player from the lobby
        // For now, we'll keep them in the lobby but mark them as bot-controlled
        // The actual bot takeover is handled by NetworkGameManager
    }

    private async Task BroadcastLobbyUpdateAsync(Lobby lobby)
    {
        var updateMessage = new LobbyUpdateMessage
        {
            Lobby = lobby.ToLobbyInfo()
        };

        await _server.BroadcastToLobbyAsync(lobby.Code, updateMessage);
    }

    private string GenerateLobbyCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Removed confusing chars (I, O, 0, 1)
        return new string(Enumerable.Range(0, 6).Select(_ => chars[_random.Next(chars.Length)]).ToArray());
    }

    public Lobby? GetLobby(string code)
    {
        _lobbies.TryGetValue(code.ToUpperInvariant(), out var lobby);
        return lobby;
    }

    public Lobby? GetClientLobby(string clientId)
    {
        if (_clientLobbyMap.TryGetValue(clientId, out var code))
        {
            return GetLobby(code);
        }
        return null;
    }

    public IReadOnlyList<Lobby> GetAllLobbies()
    {
        return _lobbies.Values.ToList();
    }

    public bool AddAiPlayerToLobby(string lobbyCode, string aiName, string aiId)
    {
        if (!_lobbies.TryGetValue(lobbyCode, out var lobby))
            return false;

        var aiPlayer = new LobbyPlayer
        {
            Id = aiId,
            Name = aiName,
            IsReady = true,
            IsHost = false,
            IsAi = true,
            Connection = null
        };

        if (lobby.AddPlayer(aiPlayer))
        {
            Task.Run(() => BroadcastLobbyUpdateAsync(lobby));
            return true;
        }
        return false;
    }

    public bool RemoveAiPlayerFromLobby(string lobbyCode, string aiId)
    {
        if (!_lobbies.TryGetValue(lobbyCode, out var lobby))
            return false;

        if (lobby.RemovePlayer(aiId))
        {
            Task.Run(() => BroadcastLobbyUpdateAsync(lobby));
            return true;
        }
        return false;
    }
}

public class DisconnectedLobbyPlayer
{
    public required string ClientId { get; init; }
    public required string PlayerName { get; init; }
    public required string LobbyCode { get; init; }
    public required string SessionToken { get; init; }
    public bool WasHost { get; init; }
}
