using TexasHoldem.Domain;
using TexasHoldem.Domain.Enums;

namespace TexasHoldem.Players;

public class BasicAiPlayer : AiPlayer
{
    public BasicAiPlayer(string name, int startingChips, PersonalityType personality, Random? random = null)
        : base(name, startingChips, personality, random)
    {
    }

    public override PlayerAction TakeTurn(GameState gameState)
    {
        // Add some thinking delay for realism
        Thread.Sleep(_random.Next(500, 2000));
        
        UpdateOpponentStats(gameState);
        
        var action = AiPersonality.MakeDecision(this, gameState, Personality!.Value, _random);
        
        // Add some poker talk occasionally
        if (_random.NextDouble() < 0.2) // 20% chance
        {
            ShowPokerTalk(action, gameState);
        }
        
        return action;
    }

    private void ShowPokerTalk(PlayerAction action, GameState gameState)
    {
        var phrases = action.Action switch
        {
            ActionType.Fold => new[]
            {
                "Not this time...",
                "I'll wait for a better spot.",
                "These cards aren't worth it.",
                "Folding is the right play here."
            },
            ActionType.Check => new[]
            {
                "I'll check and see what happens.",
                "Let's see what you've got.",
                "Checking to you.",
                "No bet from me."
            },
            ActionType.Call => new[]
            {
                "I'll call that.",
                "You're on!",
                "I'm in.",
                "Worth a call."
            },
            ActionType.Bet => new[]
            {
                "Let's make this interesting!",
                "I'm betting here.",
                "Time to put some pressure on.",
                "This hand has potential."
            },
            ActionType.Raise => new[]
            {
                "I raise!",
                "Let's up the stakes!",
                "Time to turn up the heat!",
                "I'm raising the pressure!"
            },
            ActionType.AllIn => new[]
            {
                "All in! Let's do this!",
                "I'm putting it all on the line!",
                "Time for the big move!",
                "All my chips are going in!"
            },
            _ => new[] { "Hmm..." }
        };

        var personalityPhrases = Personality switch
        {
            PersonalityType.Aggressive => new[]
            {
                "Time to be aggressive!",
                "No mercy!",
                "Attack mode activated!",
                "Let's dominate this hand!"
            },
            PersonalityType.Bluffer => new[]
            {
                "Can you read my poker face?",
                "This could be anything...",
                "What do you think I have?",
                "Are you feeling lucky?"
            },
            PersonalityType.Tight => new[]
            {
                "Playing it safe here.",
                "Only the premium hands for me.",
                "Conservative is the way to go.",
                "Patience pays off."
            },
            PersonalityType.Loose => new[]
            {
                "Why not? Life's a gamble!",
                "Let's have some fun!",
                "You never know what might hit!",
                "Taking a chance here!"
            },
            _ => phrases
        };

        var allPhrases = phrases.Concat(personalityPhrases).ToArray();
        var selectedPhrase = allPhrases[_random.Next(allPhrases.Length)];
        
        Console.WriteLine($"💬 {Name}: \"{selectedPhrase}\"");
    }

    public static BasicAiPlayer CreateRandomAiPlayer(int startingChips, Random? random = null)
    {
        random ??= new Random();
        var personality = AiNameGenerator.GetRandomPersonality();
        var name = AiNameGenerator.GenerateName(personality, true);
        
        return new BasicAiPlayer(name, startingChips, personality, random);
    }

    public override string ToString()
    {
        var personalityStr = Personality?.ToString() ?? "Unknown";
        var statusStr = IsActive ? "Active" : "Inactive";
        var chipsStr = Chips > 0 ? $"€{Chips}" : "Busted";
        
        return $"{Name} ({personalityStr}) - {chipsStr} [{statusStr}]";
    }
}