using System.Collections.Concurrent;
using TexasHoldem.Game;
using TexasHoldem.Network.Messages;
using TexasHoldem.Network.Server;

namespace TexasHoldem.Network.Chat;

/// <summary>
/// Manages chat messages and AI chat participants in a lobby.
/// </summary>
public class ChatManager
{
    private readonly PokerServer _server;
    private readonly string _lobbyCode;
    private readonly ConcurrentDictionary<string, IChatParticipant> _aiParticipants = new();
    private readonly List<ChatMessageMessage> _messageHistory = new();
    private readonly int _maxHistorySize;
    private readonly object _historyLock = new();

    // Rate limiting
    private readonly ConcurrentDictionary<string, RateLimitInfo> _rateLimits = new();
    private readonly int _maxMessagesPerWindow;
    private readonly TimeSpan _rateLimitWindow;
    private readonly int _maxMessageLength;

    public event Action<ChatMessageMessage>? OnMessageReceived;
    public event Action<string, string>? OnRateLimitExceeded; // clientId, reason

    public ChatManager(
        PokerServer server,
        string lobbyCode,
        int maxHistorySize = 100,
        int maxMessagesPerWindow = 5,
        int rateLimitWindowSeconds = 10,
        int maxMessageLength = 500)
    {
        _server = server;
        _lobbyCode = lobbyCode;
        _maxHistorySize = maxHistorySize;
        _maxMessagesPerWindow = maxMessagesPerWindow;
        _rateLimitWindow = TimeSpan.FromSeconds(rateLimitWindowSeconds);
        _maxMessageLength = maxMessageLength;

        _server.OnMessageReceived += HandleMessage;
    }

    /// <summary>
    /// Register an AI participant for chat.
    /// </summary>
    public void RegisterAiParticipant(IChatParticipant participant)
    {
        _aiParticipants[participant.PlayerId] = participant;
    }

    /// <summary>
    /// Unregister an AI participant.
    /// </summary>
    public void UnregisterAiParticipant(string participantId)
    {
        _aiParticipants.TryRemove(participantId, out _);
    }

    private void HandleMessage(string clientId, INetworkMessage message)
    {
        if (message is ChatMessageMessage chatMessage)
        {
            Task.Run(async () => await ProcessChatMessageAsync(clientId, chatMessage));
        }
    }

    private async Task ProcessChatMessageAsync(string clientId, ChatMessageMessage message)
    {
        // Skip rate limiting for AI and system messages
        if (!message.IsAi && message.ChatType == ChatMessageType.PlayerMessage)
        {
            // Check rate limit
            if (!CheckRateLimit(clientId))
            {
                OnRateLimitExceeded?.Invoke(clientId, "Too many messages. Please wait before sending more.");
                await _server.SendToClientAsync(clientId, new ErrorMessage
                {
                    Code = "RATE_LIMIT",
                    Message = "Too many messages. Please wait a few seconds before sending more."
                });
                return;
            }

            // Validate and sanitize message
            message = SanitizeMessage(message);
            if (string.IsNullOrWhiteSpace(message.Content))
            {
                return; // Empty message after sanitization
            }
        }

        // Add to history
        AddToHistory(message);

        // Broadcast the message to all clients in the lobby
        await _server.BroadcastToLobbyAsync(_lobbyCode, message);

        // Notify local handlers
        OnMessageReceived?.Invoke(message);

        // Let AI participants potentially respond
        if (message.ChatType == ChatMessageType.PlayerMessage && !message.IsAi)
        {
            await ProcessAiResponsesAsync(message);
        }
    }

    /// <summary>
    /// Check and update rate limit for a client.
    /// Returns true if the message is allowed.
    /// </summary>
    private bool CheckRateLimit(string clientId)
    {
        var now = DateTime.UtcNow;
        var info = _rateLimits.GetOrAdd(clientId, _ => new RateLimitInfo());

        lock (info)
        {
            // Remove old timestamps
            info.MessageTimestamps.RemoveAll(t => now - t > _rateLimitWindow);

            // Check if under limit
            if (info.MessageTimestamps.Count >= _maxMessagesPerWindow)
            {
                return false;
            }

            // Add new timestamp
            info.MessageTimestamps.Add(now);
            return true;
        }
    }

    /// <summary>
    /// Sanitize a chat message (trim, truncate, etc.)
    /// </summary>
    private ChatMessageMessage SanitizeMessage(ChatMessageMessage message)
    {
        var content = message.Content?.Trim() ?? "";

        // Truncate if too long
        if (content.Length > _maxMessageLength)
        {
            content = content[.._maxMessageLength] + "...";
        }

        return new ChatMessageMessage
        {
            SenderId = message.SenderId,
            SenderName = message.SenderName,
            Content = content,
            IsAi = message.IsAi,
            ChatType = message.ChatType
        };
    }

    /// <summary>
    /// Send a system message (join/leave notifications, etc.)
    /// </summary>
    public async Task SendSystemMessageAsync(string content)
    {
        var message = new ChatMessageMessage
        {
            SenderId = "system",
            SenderName = "System",
            Content = content,
            IsAi = false,
            ChatType = ChatMessageType.SystemMessage
        };

        AddToHistory(message);
        await _server.BroadcastToLobbyAsync(_lobbyCode, message);
        OnMessageReceived?.Invoke(message);
    }

    /// <summary>
    /// Send a game event message (significant game events)
    /// </summary>
    public async Task SendGameEventMessageAsync(string content)
    {
        var message = new ChatMessageMessage
        {
            SenderId = "game",
            SenderName = "Game",
            Content = content,
            IsAi = false,
            ChatType = ChatMessageType.GameEvent
        };

        AddToHistory(message);
        await _server.BroadcastToLobbyAsync(_lobbyCode, message);
        OnMessageReceived?.Invoke(message);
    }

    /// <summary>
    /// Trigger AI commentary on a game event.
    /// </summary>
    public async Task TriggerGameEventCommentaryAsync(GameEventType eventType, string description)
    {
        foreach (var participant in _aiParticipants.Values)
        {
            try
            {
                var comment = await participant.CommentOnGameEventAsync(eventType, description);
                if (!string.IsNullOrEmpty(comment))
                {
                    var message = new ChatMessageMessage
                    {
                        SenderId = participant.PlayerId,
                        SenderName = participant.PlayerName,
                        Content = comment,
                        IsAi = true,
                        ChatType = ChatMessageType.AiCommentary
                    };

                    AddToHistory(message);
                    await _server.BroadcastToLobbyAsync(_lobbyCode, message);
                    OnMessageReceived?.Invoke(message);

                    // Only let one AI comment per event to avoid spam
                    break;
                }
            }
            catch
            {
                // Ignore AI errors
            }
        }
    }

    /// <summary>
    /// Trigger AI commentary on their own action.
    /// </summary>
    public async Task TriggerActionCommentaryAsync(string playerId, string actionDescription, GameState gameState)
    {
        if (!_aiParticipants.TryGetValue(playerId, out var participant))
            return;

        try
        {
            var comment = await participant.CommentOnOwnActionAsync(actionDescription, gameState);
            if (!string.IsNullOrEmpty(comment))
            {
                var message = new ChatMessageMessage
                {
                    SenderId = participant.PlayerId,
                    SenderName = participant.PlayerName,
                    Content = comment,
                    IsAi = true,
                    ChatType = ChatMessageType.AiCommentary
                };

                AddToHistory(message);
                await _server.BroadcastToLobbyAsync(_lobbyCode, message);
                OnMessageReceived?.Invoke(message);
            }
        }
        catch
        {
            // Ignore AI errors
        }
    }

    private async Task ProcessAiResponsesAsync(ChatMessageMessage playerMessage)
    {
        // Small delay to make responses feel more natural
        await Task.Delay(500);

        foreach (var participant in _aiParticipants.Values)
        {
            try
            {
                var response = await participant.RespondToMessageAsync(
                    playerMessage.SenderName,
                    playerMessage.Content);

                if (!string.IsNullOrEmpty(response))
                {
                    // Add random delay between AI responses
                    await Task.Delay(new Random().Next(500, 2000));

                    var message = new ChatMessageMessage
                    {
                        SenderId = participant.PlayerId,
                        SenderName = participant.PlayerName,
                        Content = response,
                        IsAi = true,
                        ChatType = ChatMessageType.AiResponse
                    };

                    AddToHistory(message);
                    await _server.BroadcastToLobbyAsync(_lobbyCode, message);
                    OnMessageReceived?.Invoke(message);

                    // Only one AI responds at a time
                    break;
                }
            }
            catch
            {
                // Ignore AI errors
            }
        }
    }

    private void AddToHistory(ChatMessageMessage message)
    {
        lock (_historyLock)
        {
            _messageHistory.Add(message);

            // Trim history if too large
            while (_messageHistory.Count > _maxHistorySize)
            {
                _messageHistory.RemoveAt(0);
            }
        }
    }

    /// <summary>
    /// Get chat history for a newly joined player.
    /// </summary>
    public ChatHistoryMessage GetChatHistory(int maxMessages = 50)
    {
        lock (_historyLock)
        {
            var messages = _messageHistory
                .TakeLast(maxMessages)
                .ToList();

            return new ChatHistoryMessage { Messages = messages };
        }
    }

    /// <summary>
    /// Clear chat history (e.g., when a new game starts).
    /// </summary>
    public void ClearHistory()
    {
        lock (_historyLock)
        {
            _messageHistory.Clear();
        }
    }

    /// <summary>
    /// Clear rate limit data for a client (e.g., when they disconnect).
    /// </summary>
    public void ClearRateLimit(string clientId)
    {
        _rateLimits.TryRemove(clientId, out _);
    }
}

/// <summary>
/// Rate limiting information for a single client.
/// </summary>
internal class RateLimitInfo
{
    public List<DateTime> MessageTimestamps { get; } = new();
}
