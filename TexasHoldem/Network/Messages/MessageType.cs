namespace TexasHoldem.Network.Messages;

public enum MessageType
{
    // Connection
    Connect,
    ConnectResponse,
    Disconnect,
    Heartbeat,
    HeartbeatResponse,
    Reconnect,
    ReconnectResponse,
    PlayerDisconnected,
    PlayerReconnected,

    // Lobby
    CreateLobby,
    CreateLobbyResponse,
    JoinLobby,
    JoinLobbyResponse,
    LeaveLobby,
    LobbyUpdate,
    PlayerReady,
    StartGame,
    ListLobbies,
    ListLobbiesResponse,

    // Game State
    GameStateSync,
    ActionRequest,
    ActionResponse,
    PhaseChange,
    HandComplete,
    GameOver,

    // Chat
    ChatMessage,
    ChatHistory,

    // Admin
    KickPlayer,
    TransferHost,

    // Error
    Error
}
