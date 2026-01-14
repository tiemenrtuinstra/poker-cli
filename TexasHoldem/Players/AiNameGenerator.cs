using TexasHoldem.Domain;
using TexasHoldem.Domain.Enums;

namespace TexasHoldem.Players;

public static class AiNameGenerator
{
    private static readonly Dictionary<PersonalityType, List<string>> _personalityNames = new()
    {
        [PersonalityType.Tight] = new List<string>
        {
            "Cautious Carl", "Conservative Chris", "Careful Kate", "Prudent Paul",
            "Safe Sally", "Steady Steve", "Methodical Mike", "Calculating Cal"
        },
        
        [PersonalityType.Loose] = new List<string>
        {
            "Loose Lucy", "Reckless Rick", "Wild Will", "Crazy Casey",
            "Gambler Gary", "Risky Rita", "Daring Dave", "Impulsive Ivy"
        },
        
        [PersonalityType.Aggressive] = new List<string>
        {
            "Aggressive Al", "Fierce Fiona", "Bold Bob", "Ruthless Ruth",
            "Warrior Wayne", "Hostile Holly", "Fierce Frank", "Savage Sam"
        },
        
        [PersonalityType.Passive] = new List<string>
        {
            "Passive Pete", "Gentle Grace", "Mild Mike", "Timid Tim",
            "Quiet Quinn", "Meek Mary", "Soft Susan", "Calm Carl"
        },
        
        [PersonalityType.Bluffer] = new List<string>
        {
            "Bluffing Bill", "Deceptive Diana", "Tricky Tom", "Sneaky Sue",
            "Poker Face Pat", "Cunning Cindy", "Sly Simon", "Crafty Carol"
        },
        
        [PersonalityType.Random] = new List<string>
        {
            "Random Rob", "Chaotic Charlie", "Unpredictable Uma", "Erratic Eric",
            "Wild Card Will", "Spontaneous Sam", "Crazy Carla", "Zany Zoe"
        },
        
        [PersonalityType.Fish] = new List<string>
        {
            "Fishy Fred", "Rookie Rachel", "Newbie Nick", "Amateur Amy",
            "Beginner Ben", "Novice Nancy", "Learner Larry", "Greenhorn Greg"
        },
        
        [PersonalityType.Shark] = new List<string>
        {
            "The Shark", "Predator Phil", "Hunter Hank", "Killer Kate",
            "Apex Anna", "Destroyer Dan", "Dominator Diane", "Crusher Chris"
        },
        
        [PersonalityType.CallingStation] = new List<string>
        {
            "Calling Station Carl", "Never Fold Nick", "Sticky Steve", "Glue Gary",
            "Magnet Mike", "Velcro Vicky", "Persistent Pete", "Stubborn Sue"
        },
        
        [PersonalityType.Maniac] = new List<string>
        {
            "Maniac Max", "Psycho Paul", "Insane Ivan", "Mad Mike",
            "Berserk Betty", "Frantic Frank", "Hyper Henry", "Manic Mary"
        },
        
        [PersonalityType.Nit] = new List<string>
        {
            "Nitty Nancy", "Rock Rick", "Tight Tom", "Stone Steve",
            "Boulder Bob", "Granite Grace", "Fortress Fran", "Bunker Bill"
        }
    };

    private static readonly List<string> _funnyNames = new List<string>
    {
        "Poker McPokerface", "Chips McGillicuddy", "All-In Annie", "Fold Ferguson",
        "Royal Flush Ruby", "Two Pair Tony", "Full House Felix", "Straight Sam",
        "Flush Frank", "High Card Harry", "Bluff Betty", "Check Charlie",
        "Raise Rachel", "Call Carl", "Bet Bob", "Deal Donna",
        "Shuffle Shane", "Cut Catherine", "Ante Andy", "Blind Barry",
        "Button Brenda", "River Rita", "Flop Fred", "Turn Terry",
        "Showdown Sally", "Muck Mike", "Pot Pete", "Side Sally",
        "Main Martha", "Kicker Kevin", "Nuts Nancy", "Cooler Carl"
    };

    private static readonly Random _random = new Random();
    private static readonly HashSet<string> _usedNames = new HashSet<string>();

    public static string GenerateName(PersonalityType personality, bool useFunnyNames = false)
    {
        List<string> namePool;
        
        if (useFunnyNames && _random.NextDouble() < 0.3) // 30% chance for funny names
        {
            namePool = _funnyNames;
        }
        else
        {
            namePool = _personalityNames.GetValueOrDefault(personality, _funnyNames);
        }

        // Try to get an unused name
        var availableNames = namePool.Where(name => !_usedNames.Contains(name)).ToList();
        
        if (!availableNames.Any())
        {
            // If all names are used, generate a numbered variant
            var baseName = namePool[_random.Next(namePool.Count)];
            var counter = 2;
            string numberedName;
            
            do
            {
                numberedName = $"{baseName} {counter}";
                counter++;
            } while (_usedNames.Contains(numberedName));
            
            _usedNames.Add(numberedName);
            return numberedName;
        }

        var selectedName = availableNames[_random.Next(availableNames.Count)];
        _usedNames.Add(selectedName);
        return selectedName;
    }

    public static void ResetUsedNames()
    {
        _usedNames.Clear();
    }

    public static string GenerateRandomName()
    {
        var allNames = _personalityNames.Values.SelectMany(x => x).Concat(_funnyNames).ToList();
        var availableNames = allNames.Where(name => !_usedNames.Contains(name)).ToList();
        
        if (!availableNames.Any())
        {
            var baseName = allNames[_random.Next(allNames.Count)];
            var counter = 2;
            string numberedName;
            
            do
            {
                numberedName = $"{baseName} {counter}";
                counter++;
            } while (_usedNames.Contains(numberedName));
            
            _usedNames.Add(numberedName);
            return numberedName;
        }

        var selectedName = availableNames[_random.Next(availableNames.Count)];
        _usedNames.Add(selectedName);
        return selectedName;
    }

    public static List<string> GetPersonalityNames(PersonalityType personality)
    {
        return _personalityNames.GetValueOrDefault(personality, new List<string>());
    }

    public static PersonalityType GetRandomPersonality()
    {
        var personalities = Enum.GetValues<PersonalityType>();
        return personalities[_random.Next(personalities.Length)];
    }
}