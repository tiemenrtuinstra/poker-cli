using System.Text;

using TexasHoldem.Domain.Enums;

namespace TexasHoldem.Domain;

public record Card(Suit Suit, Rank Rank) : IComparable<Card>
{
    public int CompareTo(Card? other)
    {
        if (other == null) return 1;
        
        var rankComparison = Rank.CompareTo(other.Rank);
        return rankComparison != 0 ? rankComparison : Suit.CompareTo(other.Suit);
    }

    public override string ToString()
    {
        return GetDisplayString();
    }

    public string GetDisplayString(bool useUnicode = true)
    {
        if (useUnicode)
        {
            return $"{GetRankSymbol()}{GetSuitSymbol()}";
        }
        return $"{GetRankSymbol()}{GetSuitChar()}";
    }

    public string GetRankSymbol()
    {
        return Rank switch
        {
            Rank.Ace => "A",
            Rank.King => "K",
            Rank.Queen => "Q",
            Rank.Jack => "J",
            Rank.Ten => "10",
            _ => ((int)Rank).ToString()
        };
    }

    public string GetSuitSymbol()
    {
        return Suit switch
        {
            Suit.Hearts => "♥",
            Suit.Diamonds => "♦",
            Suit.Clubs => "♣",
            Suit.Spades => "♠",
            _ => "?"
        };
    }

    public char GetSuitChar()
    {
        return Suit switch
        {
            Suit.Hearts => 'H',
            Suit.Diamonds => 'D',
            Suit.Clubs => 'C',
            Suit.Spades => 'S',
            _ => '?'
        };
    }

    public ConsoleColor GetSuitColor()
    {
        return Suit switch
        {
            Suit.Hearts or Suit.Diamonds => ConsoleColor.Red,
            Suit.Clubs or Suit.Spades => ConsoleColor.Blue,
            _ => ConsoleColor.White
        };
    }

    public static Card Parse(string cardString)
    {
        if (string.IsNullOrWhiteSpace(cardString) || cardString.Length < 2)
            throw new ArgumentException("Invalid card string format");

        var rankStr = cardString[..^1];
        var suitChar = cardString[^1];

        var rank = rankStr.ToUpper() switch
        {
            "A" => Rank.Ace,
            "K" => Rank.King,
            "Q" => Rank.Queen,
            "J" => Rank.Jack,
            "10" => Rank.Ten,
            _ when int.TryParse(rankStr, out var r) && r >= 2 && r <= 10 => (Rank)r,
            _ => throw new ArgumentException($"Invalid rank: {rankStr}")
        };

        var suit = char.ToUpper(suitChar) switch
        {
            'H' => Suit.Hearts,
            'D' => Suit.Diamonds,
            'C' => Suit.Clubs,
            'S' => Suit.Spades,
            _ => throw new ArgumentException($"Invalid suit: {suitChar}")
        };

        return new Card(suit, rank);
    }

    /// <summary>
    /// Returns ASCII art representation of the card as string array (5 lines)
    /// </summary>
    public string[] GetAsciiArt()
    {
        var rank = GetRankSymbol();
        var suit = GetSuitSymbol();

        // Handle 10 specially since it's 2 characters
        if (rank == "10")
        {
            return new[]
            {
                "┌─────┐",
                $"│10   │",
                $"│  {suit}  │",
                $"│   10│",
                "└─────┘"
            };
        }

        return new[]
        {
            "┌─────┐",
            $"│{rank}    │",
            $"│  {suit}  │",
            $"│    {rank}│",
            "└─────┘"
        };
    }

    /// <summary>
    /// Writes ASCII art card to console with colors
    /// </summary>
    public void WriteAsciiArtColored()
    {
        var lines = GetAsciiArt();
        var suitColor = GetSuitColor();
        var originalColor = Console.ForegroundColor;

        foreach (var line in lines)
        {
            // Color the suit symbol
            foreach (char c in line)
            {
                if (c == '♥' || c == '♦' || c == '♣' || c == '♠')
                {
                    Console.ForegroundColor = suitColor;
                    Console.Write(c);
                    Console.ForegroundColor = originalColor;
                }
                else
                {
                    Console.Write(c);
                }
            }
            Console.WriteLine();
        }
    }

    /// <summary>
    /// Returns ASCII art for a face-down card
    /// </summary>
    public static string[] GetHiddenCardAscii()
    {
        return new[]
        {
            "┌─────┐",
            "│░░░░░│",
            "│░░░░░│",
            "│░░░░░│",
            "└─────┘"
        };
    }

    /// <summary>
    /// Returns ASCII art for an empty card slot
    /// </summary>
    public static string[] GetEmptySlotAscii()
    {
        return new[]
        {
            "┌─────┐",
            "│     │",
            "│     │",
            "│     │",
            "└─────┘"
        };
    }

    /// <summary>
    /// Combines multiple cards into a single string array for horizontal display
    /// </summary>
    public static string[] CombineCardsHorizontally(IEnumerable<Card?> cards, bool showHidden = false)
    {
        var cardArts = new List<string[]>();

        foreach (var card in cards)
        {
            if (card == null)
            {
                cardArts.Add(showHidden ? GetHiddenCardAscii() : GetEmptySlotAscii());
            }
            else
            {
                cardArts.Add(card.GetAsciiArt());
            }
        }

        if (!cardArts.Any())
            return Array.Empty<string>();

        var result = new string[5];
        for (int line = 0; line < 5; line++)
        {
            result[line] = string.Join(" ", cardArts.Select(art => art[line]));
        }

        return result;
    }

    /// <summary>
    /// Writes multiple cards horizontally with colors
    /// </summary>
    public static void WriteCardsHorizontallyColored(IEnumerable<Card?> cards, bool showHidden = false)
    {
        var cardList = cards.ToList();
        var cardArts = new List<(string[] lines, ConsoleColor? color)>();

        foreach (var card in cardList)
        {
            if (card == null)
            {
                cardArts.Add((showHidden ? GetHiddenCardAscii() : GetEmptySlotAscii(), null));
            }
            else
            {
                cardArts.Add((card.GetAsciiArt(), card.GetSuitColor()));
            }
        }

        if (!cardArts.Any())
            return;

        var originalColor = Console.ForegroundColor;

        for (int line = 0; line < 5; line++)
        {
            for (int cardIdx = 0; cardIdx < cardArts.Count; cardIdx++)
            {
                var (lines, color) = cardArts[cardIdx];
                var lineText = lines[line];

                foreach (char c in lineText)
                {
                    if ((c == '♥' || c == '♦' || c == '♣' || c == '♠') && color.HasValue)
                    {
                        Console.ForegroundColor = color.Value;
                        Console.Write(c);
                        Console.ForegroundColor = originalColor;
                    }
                    else
                    {
                        Console.Write(c);
                    }
                }

                if (cardIdx < cardArts.Count - 1)
                    Console.Write(" ");
            }
            Console.WriteLine();
        }
    }
}