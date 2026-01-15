using System.Collections.Concurrent;
using TexasHoldem.Network.Messages;

namespace TexasHoldem.Network.Server;

public class LobbyPlayer
{
    public required string Id { get; init; }
    public required string Name { get; set; }
    public bool IsReady { get; set; }
    public bool IsHost { get; set; }
    public bool IsAi { get; set; }
    public ClientConnection? Connection { get; set; }
    public bool IsConnected => IsAi || Connection?.State == ConnectionState.Connected;
    public bool IsBotControlled { get; set; }
}

public class Lobby
{
    public string Code { get; }
    public LobbySettings Settings { get; }
    public DateTime CreatedAt { get; }
    public LobbyState State { get; set; } = LobbyState.Waiting;

    private readonly ConcurrentDictionary<string, LobbyPlayer> _players = new();
    private string? _hostId;

    public IReadOnlyDictionary<string, LobbyPlayer> Players => _players;
    public string? HostId => _hostId;
    public int PlayerCount => _players.Count;
    public bool IsFull => PlayerCount >= Settings.MaxPlayers;
    public bool IsEmpty => PlayerCount == 0;

    public Lobby(string code, LobbySettings settings)
    {
        Code = code;
        Settings = settings;
        CreatedAt = DateTime.UtcNow;
    }

    public bool AddPlayer(LobbyPlayer player)
    {
        if (IsFull || State != LobbyState.Waiting)
            return false;

        if (_players.TryAdd(player.Id, player))
        {
            // First player becomes host
            if (_hostId == null)
            {
                _hostId = player.Id;
                player.IsHost = true;
            }
            return true;
        }
        return false;
    }

    public bool RemovePlayer(string playerId)
    {
        if (_players.TryRemove(playerId, out var player))
        {
            // If host leaves, transfer to next player
            if (player.IsHost && !IsEmpty)
            {
                var newHost = _players.Values.FirstOrDefault(p => !p.IsAi);
                if (newHost != null)
                {
                    _hostId = newHost.Id;
                    newHost.IsHost = true;
                }
                else
                {
                    // All remaining are AI, pick first one
                    var firstPlayer = _players.Values.FirstOrDefault();
                    if (firstPlayer != null)
                    {
                        _hostId = firstPlayer.Id;
                        firstPlayer.IsHost = true;
                    }
                }
            }
            return true;
        }
        return false;
    }

    public LobbyPlayer? GetPlayer(string playerId)
    {
        _players.TryGetValue(playerId, out var player);
        return player;
    }

    public LobbyPlayer? GetHost()
    {
        if (_hostId == null) return null;
        return GetPlayer(_hostId);
    }

    public bool SetPlayerReady(string playerId, bool isReady)
    {
        if (_players.TryGetValue(playerId, out var player))
        {
            player.IsReady = isReady;
            return true;
        }
        return false;
    }

    public bool TransferHost(string newHostId)
    {
        if (!_players.TryGetValue(newHostId, out var newHost))
            return false;

        // Remove host status from current host
        if (_hostId != null && _players.TryGetValue(_hostId, out var currentHost))
        {
            currentHost.IsHost = false;
        }

        _hostId = newHostId;
        newHost.IsHost = true;
        return true;
    }

    public bool AreAllPlayersReady()
    {
        if (PlayerCount < 2) return false;
        return _players.Values.All(p => p.IsReady || p.IsAi);
    }

    public bool CanStart()
    {
        return State == LobbyState.Waiting && PlayerCount >= 2 && AreAllPlayersReady();
    }

    public LobbyInfo ToLobbyInfo()
    {
        var host = GetHost();
        return new LobbyInfo
        {
            LobbyCode = Code,
            Name = Settings.Name,
            HostId = _hostId ?? "",
            HostName = host?.Name ?? "Unknown",
            Players = _players.Values.Select(p => new LobbyPlayerInfo
            {
                Id = p.Id,
                Name = p.Name,
                IsReady = p.IsReady,
                IsHost = p.IsHost,
                IsAi = p.IsAi,
                IsConnected = p.IsConnected,
                IsBotControlled = p.IsBotControlled
            }).ToList(),
            Settings = Settings,
            State = State,
            CreatedAt = CreatedAt
        };
    }

    /// <summary>
    /// Update a player's ID when reconnecting with a new client ID.
    /// </summary>
    public bool UpdatePlayerId(string oldId, string newId)
    {
        if (!_players.TryRemove(oldId, out var player))
            return false;

        // Update host ID if this was the host
        if (_hostId == oldId)
        {
            _hostId = newId;
        }

        _players[newId] = player;
        return true;
    }
}
