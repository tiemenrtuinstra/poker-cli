using Microsoft.VisualStudio.TestTools.UnitTesting;
using TexasHoldem.Network.Messages;
using TexasHoldem.Network.Server;

namespace TexasHoldem.Tests.Network;

[TestClass]
public class LobbyTests
{
    private LobbySettings CreateDefaultSettings() => new()
    {
        Name = "Test Lobby",
        MaxPlayers = 6,
        IsPublic = true,
        StartingChips = 10000,
        SmallBlind = 50,
        BigBlind = 100
    };

    private LobbyPlayer CreatePlayer(string id, string name, bool isAi = false) => new()
    {
        Id = id,
        Name = name,
        IsAi = isAi
    };

    [TestMethod]
    public void Lobby_NewLobby_HasCorrectInitialState()
    {
        // Arrange & Act
        var lobby = new Lobby("ABC123", CreateDefaultSettings());

        // Assert
        Assert.AreEqual("ABC123", lobby.Code);
        Assert.AreEqual(LobbyState.Waiting, lobby.State);
        Assert.AreEqual(0, lobby.PlayerCount);
        Assert.IsTrue(lobby.IsEmpty);
        Assert.IsFalse(lobby.IsFull);
    }

    [TestMethod]
    public void AddPlayer_FirstPlayer_BecomesHost()
    {
        // Arrange
        var lobby = new Lobby("ABC123", CreateDefaultSettings());
        var player = CreatePlayer("player1", "Alice");

        // Act
        var result = lobby.AddPlayer(player);

        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual(1, lobby.PlayerCount);
        Assert.AreEqual("player1", lobby.HostId);
        Assert.IsTrue(player.IsHost);
    }

    [TestMethod]
    public void AddPlayer_SecondPlayer_DoesNotBecomeHost()
    {
        // Arrange
        var lobby = new Lobby("ABC123", CreateDefaultSettings());
        var player1 = CreatePlayer("player1", "Alice");
        var player2 = CreatePlayer("player2", "Bob");
        lobby.AddPlayer(player1);

        // Act
        var result = lobby.AddPlayer(player2);

        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual(2, lobby.PlayerCount);
        Assert.AreEqual("player1", lobby.HostId);
        Assert.IsFalse(player2.IsHost);
    }

    [TestMethod]
    public void AddPlayer_WhenFull_ReturnsFalse()
    {
        // Arrange
        var settings = CreateDefaultSettings();
        settings.MaxPlayers = 2;
        var lobby = new Lobby("ABC123", settings);
        lobby.AddPlayer(CreatePlayer("p1", "Player1"));
        lobby.AddPlayer(CreatePlayer("p2", "Player2"));

        // Act
        var result = lobby.AddPlayer(CreatePlayer("p3", "Player3"));

        // Assert
        Assert.IsFalse(result);
        Assert.AreEqual(2, lobby.PlayerCount);
        Assert.IsTrue(lobby.IsFull);
    }

    [TestMethod]
    public void AddPlayer_WhenGameInProgress_ReturnsFalse()
    {
        // Arrange
        var lobby = new Lobby("ABC123", CreateDefaultSettings());
        lobby.AddPlayer(CreatePlayer("p1", "Player1"));
        lobby.State = LobbyState.InGame;

        // Act
        var result = lobby.AddPlayer(CreatePlayer("p2", "Player2"));

        // Assert
        Assert.IsFalse(result);
        Assert.AreEqual(1, lobby.PlayerCount);
    }

    [TestMethod]
    public void RemovePlayer_HostLeaves_TransfersHostToNextHumanPlayer()
    {
        // Arrange
        var lobby = new Lobby("ABC123", CreateDefaultSettings());
        var host = CreatePlayer("host", "Host");
        var player2 = CreatePlayer("player2", "Player2");
        lobby.AddPlayer(host);
        lobby.AddPlayer(player2);

        // Act
        lobby.RemovePlayer("host");

        // Assert
        Assert.AreEqual(1, lobby.PlayerCount);
        Assert.AreEqual("player2", lobby.HostId);
        Assert.IsTrue(player2.IsHost);
    }

    [TestMethod]
    public void RemovePlayer_HostLeaves_TransfersToAiIfNoHumans()
    {
        // Arrange
        var lobby = new Lobby("ABC123", CreateDefaultSettings());
        var host = CreatePlayer("host", "Host");
        var aiPlayer = CreatePlayer("ai1", "Bot", isAi: true);
        lobby.AddPlayer(host);
        lobby.AddPlayer(aiPlayer);

        // Act
        lobby.RemovePlayer("host");

        // Assert
        Assert.AreEqual(1, lobby.PlayerCount);
        Assert.AreEqual("ai1", lobby.HostId);
    }

    [TestMethod]
    public void SetPlayerReady_ValidPlayer_UpdatesState()
    {
        // Arrange
        var lobby = new Lobby("ABC123", CreateDefaultSettings());
        var player = CreatePlayer("player1", "Alice");
        lobby.AddPlayer(player);

        // Act
        var result = lobby.SetPlayerReady("player1", true);

        // Assert
        Assert.IsTrue(result);
        Assert.IsTrue(player.IsReady);
    }

    [TestMethod]
    public void SetPlayerReady_InvalidPlayer_ReturnsFalse()
    {
        // Arrange
        var lobby = new Lobby("ABC123", CreateDefaultSettings());

        // Act
        var result = lobby.SetPlayerReady("nonexistent", true);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void AreAllPlayersReady_AllReady_ReturnsTrue()
    {
        // Arrange
        var lobby = new Lobby("ABC123", CreateDefaultSettings());
        var player1 = CreatePlayer("p1", "Player1");
        var player2 = CreatePlayer("p2", "Player2");
        lobby.AddPlayer(player1);
        lobby.AddPlayer(player2);
        lobby.SetPlayerReady("p1", true);
        lobby.SetPlayerReady("p2", true);

        // Act & Assert
        Assert.IsTrue(lobby.AreAllPlayersReady());
    }

    [TestMethod]
    public void AreAllPlayersReady_NotAllReady_ReturnsFalse()
    {
        // Arrange
        var lobby = new Lobby("ABC123", CreateDefaultSettings());
        var player1 = CreatePlayer("p1", "Player1");
        var player2 = CreatePlayer("p2", "Player2");
        lobby.AddPlayer(player1);
        lobby.AddPlayer(player2);
        lobby.SetPlayerReady("p1", true);

        // Act & Assert
        Assert.IsFalse(lobby.AreAllPlayersReady());
    }

    [TestMethod]
    public void AreAllPlayersReady_AiPlayersCountAsReady()
    {
        // Arrange
        var lobby = new Lobby("ABC123", CreateDefaultSettings());
        var player1 = CreatePlayer("p1", "Player1");
        var aiPlayer = CreatePlayer("ai", "Bot", isAi: true);
        lobby.AddPlayer(player1);
        lobby.AddPlayer(aiPlayer);
        lobby.SetPlayerReady("p1", true);

        // Act & Assert
        Assert.IsTrue(lobby.AreAllPlayersReady());
    }

    [TestMethod]
    public void CanStart_ValidConditions_ReturnsTrue()
    {
        // Arrange
        var lobby = new Lobby("ABC123", CreateDefaultSettings());
        var player1 = CreatePlayer("p1", "Player1");
        var player2 = CreatePlayer("p2", "Player2");
        lobby.AddPlayer(player1);
        lobby.AddPlayer(player2);
        lobby.SetPlayerReady("p1", true);
        lobby.SetPlayerReady("p2", true);

        // Act & Assert
        Assert.IsTrue(lobby.CanStart());
    }

    [TestMethod]
    public void CanStart_OnlyOnePlayer_ReturnsFalse()
    {
        // Arrange
        var lobby = new Lobby("ABC123", CreateDefaultSettings());
        var player = CreatePlayer("p1", "Player1");
        lobby.AddPlayer(player);
        lobby.SetPlayerReady("p1", true);

        // Act & Assert
        Assert.IsFalse(lobby.CanStart());
    }

    [TestMethod]
    public void CanStart_NotAllReady_ReturnsFalse()
    {
        // Arrange
        var lobby = new Lobby("ABC123", CreateDefaultSettings());
        lobby.AddPlayer(CreatePlayer("p1", "Player1"));
        lobby.AddPlayer(CreatePlayer("p2", "Player2"));
        lobby.SetPlayerReady("p1", true);

        // Act & Assert
        Assert.IsFalse(lobby.CanStart());
    }

    [TestMethod]
    public void CanStart_GameAlreadyStarting_ReturnsFalse()
    {
        // Arrange
        var lobby = new Lobby("ABC123", CreateDefaultSettings());
        lobby.AddPlayer(CreatePlayer("p1", "Player1"));
        lobby.AddPlayer(CreatePlayer("p2", "Player2"));
        lobby.SetPlayerReady("p1", true);
        lobby.SetPlayerReady("p2", true);
        lobby.State = LobbyState.Starting;

        // Act & Assert
        Assert.IsFalse(lobby.CanStart());
    }

    [TestMethod]
    public void TransferHost_ValidPlayer_TransfersHost()
    {
        // Arrange
        var lobby = new Lobby("ABC123", CreateDefaultSettings());
        var player1 = CreatePlayer("p1", "Player1");
        var player2 = CreatePlayer("p2", "Player2");
        lobby.AddPlayer(player1);
        lobby.AddPlayer(player2);

        // Act
        var result = lobby.TransferHost("p2");

        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual("p2", lobby.HostId);
        Assert.IsFalse(player1.IsHost);
        Assert.IsTrue(player2.IsHost);
    }

    [TestMethod]
    public void TransferHost_InvalidPlayer_ReturnsFalse()
    {
        // Arrange
        var lobby = new Lobby("ABC123", CreateDefaultSettings());
        lobby.AddPlayer(CreatePlayer("p1", "Player1"));

        // Act
        var result = lobby.TransferHost("nonexistent");

        // Assert
        Assert.IsFalse(result);
        Assert.AreEqual("p1", lobby.HostId);
    }

    [TestMethod]
    public void ToLobbyInfo_ReturnsCorrectData()
    {
        // Arrange
        var lobby = new Lobby("ABC123", CreateDefaultSettings());
        var player1 = CreatePlayer("p1", "Alice");
        var player2 = CreatePlayer("p2", "Bob");
        lobby.AddPlayer(player1);
        lobby.AddPlayer(player2);
        lobby.SetPlayerReady("p1", true);

        // Act
        var info = lobby.ToLobbyInfo();

        // Assert
        Assert.AreEqual("ABC123", info.LobbyCode);
        Assert.AreEqual("Test Lobby", info.Name);
        Assert.AreEqual("p1", info.HostId);
        Assert.AreEqual("Alice", info.HostName);
        Assert.AreEqual(2, info.Players.Count);
        Assert.AreEqual(LobbyState.Waiting, info.State);

        var aliceInfo = info.Players.First(p => p.Name == "Alice");
        Assert.IsTrue(aliceInfo.IsReady);
        Assert.IsTrue(aliceInfo.IsHost);
    }

    [TestMethod]
    public void GetPlayer_ExistingPlayer_ReturnsPlayer()
    {
        // Arrange
        var lobby = new Lobby("ABC123", CreateDefaultSettings());
        var player = CreatePlayer("p1", "Alice");
        lobby.AddPlayer(player);

        // Act
        var result = lobby.GetPlayer("p1");

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Alice", result.Name);
    }

    [TestMethod]
    public void GetPlayer_NonExistingPlayer_ReturnsNull()
    {
        // Arrange
        var lobby = new Lobby("ABC123", CreateDefaultSettings());

        // Act
        var result = lobby.GetPlayer("nonexistent");

        // Assert
        Assert.IsNull(result);
    }
}
