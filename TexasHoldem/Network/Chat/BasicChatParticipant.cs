using TexasHoldem.Domain;
using TexasHoldem.Domain.Enums;

namespace TexasHoldem.Network.Chat;

/// <summary>
/// AI chat participant that uses predefined phrases based on personality.
/// </summary>
public class BasicChatParticipant : IChatParticipant
{
    private readonly Random _random;
    private readonly PersonalityType _personality;
    private readonly double _chattiness; // 0.0 - 1.0, how often AI responds

    public string PlayerId { get; }
    public string PlayerName { get; }

    private static readonly Dictionary<PersonalityType, string[]> PersonalityPhrases = new()
    {
        [PersonalityType.Tight] = new[]
        {
            "I prefer to wait for premium hands.",
            "Patience is key in poker.",
            "I only play when I'm sure.",
            "Not my kind of hand.",
            "I'll wait for something better."
        },
        [PersonalityType.Loose] = new[]
        {
            "Let's see what happens!",
            "Every hand is a chance to win!",
            "I love playing hands!",
            "You never know what the flop brings!",
            "Life's too short to fold!"
        },
        [PersonalityType.Aggressive] = new[]
        {
            "Let's raise this up!",
            "Time to put some pressure on!",
            "I'm coming after those chips!",
            "Fold or pay up!",
            "No free cards here!"
        },
        [PersonalityType.Passive] = new[]
        {
            "I'll just call.",
            "Let's see what develops.",
            "No need to raise just yet.",
            "I'll play along.",
            "Taking it slow here."
        },
        [PersonalityType.Bluffer] = new[]
        {
            "You sure you want to call that?",
            "I've got something special here...",
            "Feeling confident about this one.",
            "Are you brave enough?",
            "This could be the one..."
        },
        [PersonalityType.Shark] = new[]
        {
            "Interesting spot.",
            "Let me think about this.",
            "The math says...",
            "Position is everything.",
            "Standard play here."
        },
        [PersonalityType.Maniac] = new[]
        {
            "ALL IN! Just kidding... or am I?",
            "LET'S GOOOO!",
            "Chaos is my middle name!",
            "Who wants to gamble?!",
            "YOLO!"
        }
    };

    private static readonly string[] EventComments = new[]
    {
        "Wow, that's a big pot!",
        "Things are heating up!",
        "Nice play!",
        "That was unexpected!",
        "Interesting...",
        "Good hand!",
        "Well played!",
        "That's poker for you!"
    };

    private static readonly string[] ResponsePhrases = new[]
    {
        "Ha! Good one.",
        "We'll see about that!",
        "You might be right...",
        "Let the cards decide!",
        "Fair enough.",
        "I hear you.",
        "That's the spirit!",
        "Keep telling yourself that!"
    };

    public BasicChatParticipant(string playerId, string playerName, PersonalityType personality, double chattiness = 0.3)
    {
        PlayerId = playerId;
        PlayerName = playerName;
        _personality = personality;
        _chattiness = Math.Clamp(chattiness, 0.0, 1.0);
        _random = new Random();
    }

    public Task<string?> RespondToMessageAsync(string senderName, string message)
    {
        // Random chance to respond
        if (_random.NextDouble() > _chattiness)
        {
            return Task.FromResult<string?>(null);
        }

        // Higher chance to respond if mentioned
        var mentionedByName = message.Contains(PlayerName, StringComparison.OrdinalIgnoreCase);
        if (!mentionedByName && _random.NextDouble() > _chattiness * 2)
        {
            return Task.FromResult<string?>(null);
        }

        var response = ResponsePhrases[_random.Next(ResponsePhrases.Length)];
        return Task.FromResult<string?>(response);
    }

    public Task<string?> CommentOnGameEventAsync(GameEventType eventType, string description)
    {
        // Random chance to comment on events
        if (_random.NextDouble() > _chattiness * 1.5)
        {
            return Task.FromResult<string?>(null);
        }

        var comment = eventType switch
        {
            GameEventType.BigPot => new[] { "Now that's a pot worth winning!", "Big money on the table!", "Stakes are getting high!" },
            GameEventType.AllIn => new[] { "All in! This is getting intense!", "Big move!", "Now that's commitment!" },
            GameEventType.BadBeat => new[] { "Ouch! That's brutal!", "The poker gods are cruel!", "Unlucky!" },
            GameEventType.BluffCalled => new[] { "Caught red-handed!", "Nice catch!", "Should've folded!" },
            GameEventType.RoyalFlush => new[] { "ROYAL FLUSH! Incredible!", "The holy grail!", "Unbelievable!" },
            GameEventType.BigWin => new[] { "Congratulations on the big win!", "Well played!", "Taking down a nice pot!" },
            GameEventType.PlayerEliminated => new[] { "Another one bites the dust!", "Good game!", "Better luck next time!" },
            _ => EventComments
        };

        return Task.FromResult<string?>(comment[_random.Next(comment.Length)]);
    }

    public Task<string?> CommentOnOwnActionAsync(string actionDescription, GameState gameState)
    {
        // 20% chance to comment on own action
        if (_random.NextDouble() > 0.2)
        {
            return Task.FromResult<string?>(null);
        }

        if (PersonalityPhrases.TryGetValue(_personality, out var phrases))
        {
            return Task.FromResult<string?>(phrases[_random.Next(phrases.Length)]);
        }

        return Task.FromResult<string?>(null);
    }
}
