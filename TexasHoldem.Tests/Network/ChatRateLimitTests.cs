using Microsoft.VisualStudio.TestTools.UnitTesting;
using TexasHoldem.Network.Messages;

namespace TexasHoldem.Tests.Network;

[TestClass]
public class ChatRateLimitTests
{
    [TestMethod]
    public void ChatMessageMessage_Serialization_RoundTrip()
    {
        // Arrange
        var message = new ChatMessageMessage
        {
            SenderId = "player1",
            SenderName = "Alice",
            Content = "Hello, world!",
            IsAi = false,
            ChatType = ChatMessageType.PlayerMessage
        };

        // Act
        var json = MessageSerializer.Serialize(message);
        var result = MessageSerializer.Deserialize(json) as ChatMessageMessage;

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("player1", result.SenderId);
        Assert.AreEqual("Alice", result.SenderName);
        Assert.AreEqual("Hello, world!", result.Content);
        Assert.IsFalse(result.IsAi);
        Assert.AreEqual(ChatMessageType.PlayerMessage, result.ChatType);
    }

    [TestMethod]
    public void ChatMessageMessage_AiCommentary_SerializesCorrectly()
    {
        // Arrange
        var message = new ChatMessageMessage
        {
            SenderId = "ai_bot",
            SenderName = "Bot Alice",
            Content = "Nice hand!",
            IsAi = true,
            ChatType = ChatMessageType.AiCommentary
        };

        // Act
        var json = MessageSerializer.Serialize(message);
        var result = MessageSerializer.Deserialize(json) as ChatMessageMessage;

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.IsAi);
        Assert.AreEqual(ChatMessageType.AiCommentary, result.ChatType);
    }

    [TestMethod]
    public void ChatHistoryMessage_SerializesMultipleMessages()
    {
        // Arrange
        var message = new ChatHistoryMessage
        {
            Messages = new List<ChatMessageMessage>
            {
                new() { SenderId = "p1", SenderName = "Alice", Content = "Hi!", IsAi = false, ChatType = ChatMessageType.PlayerMessage },
                new() { SenderId = "p2", SenderName = "Bob", Content = "Hello!", IsAi = false, ChatType = ChatMessageType.PlayerMessage },
                new() { SenderId = "system", SenderName = "System", Content = "Game starting", IsAi = false, ChatType = ChatMessageType.SystemMessage }
            }
        };

        // Act
        var json = MessageSerializer.Serialize(message);
        var result = MessageSerializer.Deserialize(json) as ChatHistoryMessage;

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(3, result.Messages.Count);
    }

    [TestMethod]
    public void ErrorMessage_RateLimitCode_SerializesCorrectly()
    {
        // Arrange
        var message = new ErrorMessage
        {
            Code = "RATE_LIMIT",
            Message = "Too many messages. Please wait a few seconds."
        };

        // Act
        var json = MessageSerializer.Serialize(message);
        var result = MessageSerializer.Deserialize(json) as ErrorMessage;

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("RATE_LIMIT", result.Code);
        Assert.IsTrue(result.Message.Contains("Too many messages"));
    }

    [TestMethod]
    public void ChatMessageType_AllTypes_SerializeCorrectly()
    {
        // Test all chat message types
        var types = new[]
        {
            ChatMessageType.PlayerMessage,
            ChatMessageType.AiCommentary,
            ChatMessageType.AiResponse,
            ChatMessageType.SystemMessage,
            ChatMessageType.GameEvent
        };

        foreach (var chatType in types)
        {
            var message = new ChatMessageMessage
            {
                SenderId = "test",
                SenderName = "Test",
                Content = "Test message",
                IsAi = false,
                ChatType = chatType
            };

            var json = MessageSerializer.Serialize(message);
            var result = MessageSerializer.Deserialize(json) as ChatMessageMessage;

            Assert.IsNotNull(result, $"Failed to deserialize ChatMessageType.{chatType}");
            Assert.AreEqual(chatType, result.ChatType, $"Wrong ChatMessageType for {chatType}");
        }
    }
}
