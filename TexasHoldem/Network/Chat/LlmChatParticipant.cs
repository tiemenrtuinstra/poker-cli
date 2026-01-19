using TexasHoldem.Game;
using TexasHoldem.Game.Enums;

namespace TexasHoldem.Network.Chat;

/// <summary>
/// Function delegate for LLM API calls.
/// </summary>
public delegate Task<string> LlmApiCall(string prompt);

/// <summary>
/// AI chat participant that uses LLM for natural language responses.
/// </summary>
public class LlmChatParticipant : IChatParticipant
{
    private readonly LlmApiCall _llmApiCall;
    private readonly PersonalityType _personality;
    private readonly Random _random;
    private readonly double _chattiness;

    public string PlayerId { get; }
    public string PlayerName { get; }

    public LlmChatParticipant(
        string playerId,
        string playerName,
        LlmApiCall llmApiCall,
        PersonalityType personality,
        double chattiness = 0.3)
    {
        PlayerId = playerId;
        PlayerName = playerName;
        _llmApiCall = llmApiCall;
        _personality = personality;
        _chattiness = Math.Clamp(chattiness, 0.0, 1.0);
        _random = new Random();
    }

    public async Task<string?> RespondToMessageAsync(string senderName, string message)
    {
        // Check if we should respond
        var mentionedByName = message.Contains(PlayerName, StringComparison.OrdinalIgnoreCase);
        if (!mentionedByName && _random.NextDouble() > _chattiness)
        {
            return null;
        }

        if (mentionedByName && _random.NextDouble() > _chattiness * 3)
        {
            return null;
        }

        var prompt = $"""
            You are {PlayerName}, a poker player at a Texas Hold'em table.
            Your personality type is: {_personality}

            Personality descriptions:
            - Tight: Conservative, only plays premium hands, patient
            - Loose: Plays many hands, likes action
            - Aggressive: Bets and raises often, puts pressure on opponents
            - Passive: Prefers calling over betting, avoids confrontation
            - Bluffer: Likes to bluff, enjoys mind games
            - Shark: Professional, calculated, emotionally controlled
            - Maniac: Wild, unpredictable, loves chaos

            Another player named {senderName} just said in the chat: "{message}"

            Generate a short, in-character response (1-2 sentences max).
            Stay in character based on your personality.
            Keep it casual and poker-table appropriate.
            Don't use quotation marks in your response.
            """;

        try
        {
            var response = await _llmApiCall(prompt);
            return CleanResponse(response);
        }
        catch
        {
            return null;
        }
    }

    public async Task<string?> CommentOnGameEventAsync(GameEventType eventType, string description)
    {
        // Higher chance to comment on significant events
        var eventChance = eventType switch
        {
            GameEventType.RoyalFlush => 0.9,
            GameEventType.StraightFlush => 0.8,
            GameEventType.FourOfAKind => 0.7,
            GameEventType.AllIn => 0.5,
            GameEventType.BigPot => 0.4,
            GameEventType.BadBeat => 0.6,
            _ => _chattiness
        };

        if (_random.NextDouble() > eventChance)
        {
            return null;
        }

        var prompt = $"""
            You are {PlayerName}, a poker player at a Texas Hold'em table.
            Your personality type is: {_personality}

            A significant event just happened: {description}
            Event type: {eventType}

            Generate a short, in-character reaction (1 sentence max).
            Express emotion appropriate to the event and your personality.
            Don't use quotation marks in your response.
            """;

        try
        {
            var response = await _llmApiCall(prompt);
            return CleanResponse(response);
        }
        catch
        {
            return null;
        }
    }

    public async Task<string?> CommentOnOwnActionAsync(string actionDescription, GameState gameState)
    {
        // 20% chance to comment on own action
        if (_random.NextDouble() > 0.2)
        {
            return null;
        }

        var potSize = gameState.TotalPot;
        var phase = gameState.BettingPhase;

        var prompt = $"""
            You are {PlayerName}, a poker player at a Texas Hold'em table.
            Your personality type is: {_personality}

            You just made this action: {actionDescription}
            Current phase: {phase}
            Pot size: €{potSize}

            Generate a short poker table talk comment about your action (1 sentence max).
            Stay in character. Be casual and natural.
            Don't reveal information about your actual hand strength.
            Don't use quotation marks in your response.
            """;

        try
        {
            var response = await _llmApiCall(prompt);
            return CleanResponse(response);
        }
        catch
        {
            return null;
        }
    }

    private static string? CleanResponse(string? response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return null;

        // Remove quotes, trim, and limit length
        response = response.Trim().Trim('"', '\'');

        // Limit to reasonable length
        if (response.Length > 200)
        {
            response = response[..200].TrimEnd('.', ' ') + "...";
        }

        return response;
    }
}
