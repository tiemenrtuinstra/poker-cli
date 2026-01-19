using Microsoft.VisualStudio.TestTools.UnitTesting;
using TexasHoldem.Network.Messages;
using TexasHoldem.Game.Enums;

namespace TexasHoldem.Tests.Network;

[TestClass]
public class MessageSerializerTests
{
    [TestMethod]
    public void Serialize_ConnectMessage_ReturnsValidJson()
    {
        // Arrange
        var message = new ConnectMessage
        {
            PlayerName = "TestPlayer",
            LobbyCode = "ABC123"
        };

        // Act
        var json = MessageSerializer.Serialize(message);

        // Assert
        Assert.IsNotNull(json);
        Assert.IsTrue(json.Contains("\"type\":\"Connect\""));
        Assert.IsTrue(json.Contains("TestPlayer"));
        Assert.IsTrue(json.Contains("ABC123"));
    }

    [TestMethod]
    public void Deserialize_ValidConnectMessage_ReturnsCorrectType()
    {
        // Arrange
        var message = new ConnectMessage
        {
            PlayerName = "TestPlayer",
            LobbyCode = "ABC123"
        };
        var json = MessageSerializer.Serialize(message);

        // Act
        var result = MessageSerializer.Deserialize(json);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(ConnectMessage));
        var connectMessage = (ConnectMessage)result;
        Assert.AreEqual("TestPlayer", connectMessage.PlayerName);
        Assert.AreEqual("ABC123", connectMessage.LobbyCode);
    }

    [TestMethod]
    public void Deserialize_InvalidJson_ReturnsNull()
    {
        // Arrange
        var invalidJson = "{ invalid json }";

        // Act
        var result = MessageSerializer.Deserialize(invalidJson);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Deserialize_EmptyString_ReturnsNull()
    {
        // Act
        var result = MessageSerializer.Deserialize("");

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Serialize_ActionResponseMessage_IncludesEnumAsString()
    {
        // Arrange
        var message = new ActionResponseMessage
        {
            PlayerId = "player1",
            Action = ActionType.Raise,
            Amount = 100
        };

        // Act
        var json = MessageSerializer.Serialize(message);

        // Assert
        Assert.IsTrue(json.Contains("\"action\":\"Raise\""));
        Assert.IsTrue(json.Contains("\"amount\":100"));
    }

    [TestMethod]
    public void RoundTrip_LobbySettings_PreservesAllProperties()
    {
        // Arrange
        var settings = new LobbySettings
        {
            Name = "Test Lobby",
            MaxPlayers = 6,
            IsPublic = true,
            Password = "secret",
            StartingChips = 10000,
            SmallBlind = 50,
            BigBlind = 100,
            Ante = 10
        };
        var message = new CreateLobbyMessage { Settings = settings };

        // Act
        var json = MessageSerializer.Serialize(message);
        var result = MessageSerializer.Deserialize(json) as CreateLobbyMessage;

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Test Lobby", result.Settings.Name);
        Assert.AreEqual(6, result.Settings.MaxPlayers);
        Assert.IsTrue(result.Settings.IsPublic);
        Assert.AreEqual("secret", result.Settings.Password);
        Assert.AreEqual(10000, result.Settings.StartingChips);
        Assert.AreEqual(50, result.Settings.SmallBlind);
        Assert.AreEqual(100, result.Settings.BigBlind);
        Assert.AreEqual(10, result.Settings.Ante);
    }

    [TestMethod]
    public void Serialize_ChatMessage_IncludesChatType()
    {
        // Arrange
        var message = new ChatMessageMessage
        {
            SenderId = "player1",
            SenderName = "Alice",
            Content = "Hello!",
            IsAi = false,
            ChatType = ChatMessageType.PlayerMessage
        };

        // Act
        var json = MessageSerializer.Serialize(message);

        // Assert
        Assert.IsTrue(json.Contains("\"chatType\":\"PlayerMessage\""));
    }

    [TestMethod]
    public void Deserialize_AllMessageTypes_ReturnsCorrectTypes()
    {
        // Test various message types
        var testCases = new (INetworkMessage message, Type expectedType)[]
        {
            (new ConnectMessage { PlayerName = "Test" }, typeof(ConnectMessage)),
            (new DisconnectMessage { Reason = "Test" }, typeof(DisconnectMessage)),
            (new HeartbeatMessage(), typeof(HeartbeatMessage)),
            (new CreateLobbyMessage { Settings = new LobbySettings { Name = "Test" } }, typeof(CreateLobbyMessage)),
            (new JoinLobbyMessage { LobbyCode = "ABC123" }, typeof(JoinLobbyMessage)),
            (new PlayerReadyMessage { IsReady = true }, typeof(PlayerReadyMessage)),
            (new ErrorMessage { Code = "TEST", Message = "Test error" }, typeof(ErrorMessage))
        };

        foreach (var (message, expectedType) in testCases)
        {
            var json = MessageSerializer.Serialize(message);
            var result = MessageSerializer.Deserialize(json);

            Assert.IsNotNull(result, $"Failed to deserialize {expectedType.Name}");
            Assert.IsInstanceOfType(result, expectedType, $"Wrong type for {expectedType.Name}");
        }
    }

    [TestMethod]
    public void Serialize_NullProperties_AreOmitted()
    {
        // Arrange
        var message = new ConnectMessage
        {
            PlayerName = "Test",
            LobbyCode = null,
            SessionToken = null
        };

        // Act
        var json = MessageSerializer.Serialize(message);

        // Assert
        Assert.IsFalse(json.Contains("lobbyCode"));
        Assert.IsFalse(json.Contains("sessionToken"));
    }

    [TestMethod]
    public void RoundTrip_ReconnectMessage_PreservesSessionToken()
    {
        // Arrange
        var message = new ReconnectMessage
        {
            PlayerName = "ReconnectingPlayer",
            SessionToken = "abc123-token"
        };

        // Act
        var json = MessageSerializer.Serialize(message);
        var result = MessageSerializer.Deserialize(json) as ReconnectMessage;

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("ReconnectingPlayer", result.PlayerName);
        Assert.AreEqual("abc123-token", result.SessionToken);
    }

    [TestMethod]
    public void RoundTrip_PlayerDisconnectedMessage_PreservesBotTakeover()
    {
        // Arrange
        var message = new PlayerDisconnectedMessage
        {
            PlayerId = "player1",
            PlayerName = "Alice",
            BotTakeover = true
        };

        // Act
        var json = MessageSerializer.Serialize(message);
        var result = MessageSerializer.Deserialize(json) as PlayerDisconnectedMessage;

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("player1", result.PlayerId);
        Assert.AreEqual("Alice", result.PlayerName);
        Assert.IsTrue(result.BotTakeover);
    }
}
