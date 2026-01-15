using System.Text.Json.Serialization;
using TexasHoldem.Domain.Enums;

namespace TexasHoldem.Network.Messages;

// Base class for all messages
public abstract class NetworkMessage : INetworkMessage
{
    public abstract MessageType Type { get; }
    public string MessageId { get; init; } = Guid.NewGuid().ToString("N")[..8];
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

#region Connection Messages

public class ConnectMessage : NetworkMessage
{
    public override MessageType Type => MessageType.Connect;
    public required string PlayerName { get; init; }
    public string? LobbyCode { get; init; }
    public string? SessionToken { get; init; } // For reconnection
}

public class ConnectResponseMessage : NetworkMessage
{
    public override MessageType Type => MessageType.ConnectResponse;
    public required bool Success { get; init; }
    public string? ClientId { get; init; }
    public string? SessionToken { get; init; }
    public string? Error { get; init; }
}

public class DisconnectMessage : NetworkMessage
{
    public override MessageType Type => MessageType.Disconnect;
    public string? Reason { get; init; }
}

public class HeartbeatMessage : NetworkMessage
{
    public override MessageType Type => MessageType.Heartbeat;
}

public class HeartbeatResponseMessage : NetworkMessage
{
    public override MessageType Type => MessageType.HeartbeatResponse;
}

public class ReconnectMessage : NetworkMessage
{
    public override MessageType Type => MessageType.Reconnect;
    public required string SessionToken { get; init; }
    public required string PlayerName { get; init; }
}

public class ReconnectResponseMessage : NetworkMessage
{
    public override MessageType Type => MessageType.ReconnectResponse;
    public required bool Success { get; init; }
    public string? ClientId { get; init; }
    public string? LobbyCode { get; init; }
    public LobbyInfo? Lobby { get; init; }
    public NetworkGameState? GameState { get; init; }
    public string? Error { get; init; }
}

public class PlayerDisconnectedMessage : NetworkMessage
{
    public override MessageType Type => MessageType.PlayerDisconnected;
    public required string PlayerId { get; init; }
    public required string PlayerName { get; init; }
    public bool BotTakeover { get; init; }
}

public class PlayerReconnectedMessage : NetworkMessage
{
    public override MessageType Type => MessageType.PlayerReconnected;
    public required string PlayerId { get; init; }
    public required string PlayerName { get; init; }
}

#endregion

#region Lobby Messages

public class CreateLobbyMessage : NetworkMessage
{
    public override MessageType Type => MessageType.CreateLobby;
    public required LobbySettings Settings { get; init; }
}

public class CreateLobbyResponseMessage : NetworkMessage
{
    public override MessageType Type => MessageType.CreateLobbyResponse;
    public required bool Success { get; init; }
    public string? LobbyCode { get; init; }
    public string? Error { get; init; }
}

public class JoinLobbyMessage : NetworkMessage
{
    public override MessageType Type => MessageType.JoinLobby;
    public required string LobbyCode { get; init; }
    public string? Password { get; init; }
}

public class JoinLobbyResponseMessage : NetworkMessage
{
    public override MessageType Type => MessageType.JoinLobbyResponse;
    public required bool Success { get; init; }
    public LobbyInfo? Lobby { get; init; }
    public string? Error { get; init; }
}

public class LeaveLobbyMessage : NetworkMessage
{
    public override MessageType Type => MessageType.LeaveLobby;
}

public class LobbyUpdateMessage : NetworkMessage
{
    public override MessageType Type => MessageType.LobbyUpdate;
    public required LobbyInfo Lobby { get; init; }
}

public class PlayerReadyMessage : NetworkMessage
{
    public override MessageType Type => MessageType.PlayerReady;
    public required bool IsReady { get; init; }
}

public class StartGameMessage : NetworkMessage
{
    public override MessageType Type => MessageType.StartGame;
}

public class ListLobbiesMessage : NetworkMessage
{
    public override MessageType Type => MessageType.ListLobbies;
}

public class ListLobbiesResponseMessage : NetworkMessage
{
    public override MessageType Type => MessageType.ListLobbiesResponse;
    public required List<LobbyInfo> Lobbies { get; init; }
}

#endregion

#region Game Messages

public class GameStateSyncMessage : NetworkMessage
{
    public override MessageType Type => MessageType.GameStateSync;
    public required NetworkGameState State { get; init; }
}

public class ActionRequestMessage : NetworkMessage
{
    public override MessageType Type => MessageType.ActionRequest;
    public required string PlayerId { get; init; }
    public required List<ActionType> ValidActions { get; init; }
    public required int MinBet { get; init; }
    public required int MaxBet { get; init; }
    public required int AmountToCall { get; init; }
    public int TimeoutMs { get; init; } = 60000;
}

public class ActionResponseMessage : NetworkMessage
{
    public override MessageType Type => MessageType.ActionResponse;
    public required string PlayerId { get; init; }
    public required ActionType Action { get; init; }
    public int Amount { get; init; }
}

public class PhaseChangeMessage : NetworkMessage
{
    public override MessageType Type => MessageType.PhaseChange;
    public required string Phase { get; init; }
    public List<string>? NewCommunityCards { get; init; }
}

public class HandCompleteMessage : NetworkMessage
{
    public override MessageType Type => MessageType.HandComplete;
    public required List<WinnerInfo> Winners { get; init; }
    public required string Summary { get; init; }
}

public class GameOverMessage : NetworkMessage
{
    public override MessageType Type => MessageType.GameOver;
    public required List<PlayerRanking> Rankings { get; init; }
}

#endregion

#region Chat Messages

public class ChatMessageMessage : NetworkMessage
{
    public override MessageType Type => MessageType.ChatMessage;
    public required string SenderId { get; init; }
    public required string SenderName { get; init; }
    public required string Content { get; init; }
    public bool IsAi { get; init; }
    public ChatMessageType ChatType { get; init; } = ChatMessageType.PlayerMessage;
}

public class ChatHistoryMessage : NetworkMessage
{
    public override MessageType Type => MessageType.ChatHistory;
    public required List<ChatMessageMessage> Messages { get; init; }
}

public enum ChatMessageType
{
    PlayerMessage,
    AiCommentary,
    AiResponse,
    SystemMessage,
    GameEvent
}

#endregion

#region Admin Messages

public class KickPlayerMessage : NetworkMessage
{
    public override MessageType Type => MessageType.KickPlayer;
    public required string PlayerId { get; init; }
    public string? Reason { get; init; }
}

public class TransferHostMessage : NetworkMessage
{
    public override MessageType Type => MessageType.TransferHost;
    public required string NewHostId { get; init; }
}

#endregion

#region Error Messages

public class ErrorMessage : NetworkMessage
{
    public override MessageType Type => MessageType.Error;
    public required string Code { get; init; }
    public required string Message { get; init; }
    public string? Details { get; init; }
}

#endregion

#region Supporting Types

public class LobbySettings
{
    public string Name { get; set; } = "Poker Game";
    public int MaxPlayers { get; set; } = 8;
    public bool IsPublic { get; set; } = true;
    public string? Password { get; set; }
    public int StartingChips { get; set; } = 10000;
    public int SmallBlind { get; set; } = 50;
    public int BigBlind { get; set; } = 100;
    public int Ante { get; set; } = 0;
    public int AiPlayerCount { get; set; } = 0;
    public List<string> EnabledAiProviders { get; set; } = new();
}

public class LobbyInfo
{
    public required string LobbyCode { get; init; }
    public required string Name { get; init; }
    public required string HostId { get; init; }
    public required string HostName { get; init; }
    public required List<LobbyPlayerInfo> Players { get; init; }
    public required LobbySettings Settings { get; init; }
    public required LobbyState State { get; init; }
    public required DateTime CreatedAt { get; init; }
}

public class LobbyPlayerInfo
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required bool IsReady { get; init; }
    public required bool IsHost { get; init; }
    public required bool IsAi { get; init; }
    public bool IsConnected { get; init; } = true;
    public bool IsBotControlled { get; init; }
}

public enum LobbyState
{
    Waiting,
    Starting,
    InGame,
    Finished
}

public class NetworkGameState
{
    public int HandNumber { get; set; }
    public string Phase { get; set; } = "";
    public string BettingPhase { get; set; } = "";
    public List<NetworkPlayerInfo> Players { get; set; } = new();
    public int DealerPosition { get; set; }
    public int SmallBlindPosition { get; set; }
    public int BigBlindPosition { get; set; }
    public int CurrentPlayerIndex { get; set; }
    public List<string> CommunityCards { get; set; } = new();
    public int TotalPot { get; set; }
    public int CurrentBet { get; set; }
    public int SmallBlindAmount { get; set; }
    public int BigBlindAmount { get; set; }
    public int AnteAmount { get; set; }

    // Per-client: only includes that client's hole cards
    public List<string>? MyHoleCards { get; set; }
    public string? MyPlayerId { get; set; }
}

public class NetworkPlayerInfo
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required int Chips { get; init; }
    public required int CurrentBet { get; init; }
    public required bool IsActive { get; init; }
    public required bool IsAllIn { get; init; }
    public required bool HasFolded { get; init; }
    public required bool IsConnected { get; init; }
    public bool IsCurrentPlayer { get; init; }
    public List<string>? HoleCards { get; init; } // Only shown at showdown or to owner
}

public class WinnerInfo
{
    public required string PlayerId { get; init; }
    public required string PlayerName { get; init; }
    public required int Amount { get; init; }
    public required string HandDescription { get; init; }
    public List<string>? WinningCards { get; init; }
}

public class PlayerRanking
{
    public required int Rank { get; init; }
    public required string PlayerId { get; init; }
    public required string PlayerName { get; init; }
    public required int FinalChips { get; init; }
}

#endregion
