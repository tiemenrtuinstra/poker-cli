using System.Text.Json;
using System.Text.Json.Serialization;

namespace TexasHoldem.Network.Messages;

public static class MessageSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string Serialize(INetworkMessage message)
    {
        var wrapper = new MessageWrapper
        {
            Type = message.Type,
            Payload = JsonSerializer.SerializeToElement(message, message.GetType(), Options)
        };
        return JsonSerializer.Serialize(wrapper, Options);
    }

    public static INetworkMessage? Deserialize(string json)
    {
        try
        {
            var wrapper = JsonSerializer.Deserialize<MessageWrapper>(json, Options);
            if (wrapper == null) return null;

            var targetType = GetMessageType(wrapper.Type);
            if (targetType == null) return null;

            return JsonSerializer.Deserialize(wrapper.Payload.GetRawText(), targetType, Options) as INetworkMessage;
        }
        catch
        {
            return null;
        }
    }

    public static T? Deserialize<T>(string json) where T : INetworkMessage
    {
        return (T?)Deserialize(json);
    }

    private static Type? GetMessageType(MessageType type) => type switch
    {
        // Connection
        MessageType.Connect => typeof(ConnectMessage),
        MessageType.ConnectResponse => typeof(ConnectResponseMessage),
        MessageType.Disconnect => typeof(DisconnectMessage),
        MessageType.Heartbeat => typeof(HeartbeatMessage),
        MessageType.HeartbeatResponse => typeof(HeartbeatResponseMessage),
        MessageType.Reconnect => typeof(ReconnectMessage),
        MessageType.ReconnectResponse => typeof(ReconnectResponseMessage),
        MessageType.PlayerDisconnected => typeof(PlayerDisconnectedMessage),
        MessageType.PlayerReconnected => typeof(PlayerReconnectedMessage),

        // Lobby
        MessageType.CreateLobby => typeof(CreateLobbyMessage),
        MessageType.CreateLobbyResponse => typeof(CreateLobbyResponseMessage),
        MessageType.JoinLobby => typeof(JoinLobbyMessage),
        MessageType.JoinLobbyResponse => typeof(JoinLobbyResponseMessage),
        MessageType.LeaveLobby => typeof(LeaveLobbyMessage),
        MessageType.LobbyUpdate => typeof(LobbyUpdateMessage),
        MessageType.PlayerReady => typeof(PlayerReadyMessage),
        MessageType.StartGame => typeof(StartGameMessage),
        MessageType.ListLobbies => typeof(ListLobbiesMessage),
        MessageType.ListLobbiesResponse => typeof(ListLobbiesResponseMessage),

        // Game
        MessageType.GameStateSync => typeof(GameStateSyncMessage),
        MessageType.ActionRequest => typeof(ActionRequestMessage),
        MessageType.ActionResponse => typeof(ActionResponseMessage),
        MessageType.PhaseChange => typeof(PhaseChangeMessage),
        MessageType.HandComplete => typeof(HandCompleteMessage),
        MessageType.GameOver => typeof(GameOverMessage),

        // Chat
        MessageType.ChatMessage => typeof(ChatMessageMessage),
        MessageType.ChatHistory => typeof(ChatHistoryMessage),

        // Admin
        MessageType.KickPlayer => typeof(KickPlayerMessage),
        MessageType.TransferHost => typeof(TransferHostMessage),

        // Error
        MessageType.Error => typeof(ErrorMessage),

        _ => null
    };

    private class MessageWrapper
    {
        public MessageType Type { get; set; }
        public JsonElement Payload { get; set; }
    }
}
