using TexasHoldem.Game.Enums;

namespace TexasHoldem.Game;

public class HandEvaluator
{
    public static HandResult EvaluateHand(IEnumerable<Card> cards)
    {
        var cardList = cards.OrderByDescending(c => c.Rank).ToList();
        
        if (cardList.Count < 5)
            throw new ArgumentException("Hand evaluation requires at least 5 cards");

        // For hands with more than 5 cards (e.g., 7 in Texas Hold'em), find the best 5-card combination
        if (cardList.Count > 5)
        {
            return FindBestFiveCardHand(cardList);
        }

        return EvaluateFiveCardHand(cardList);
    }

    private static HandResult FindBestFiveCardHand(List<Card> allCards)
    {
        var bestHand = new HandResult { Strength = HandStrength.HighCard, Score = 0 };
        
        // Generate all possible 5-card combinations
        var combinations = GetCombinations(allCards, 5);
        
        foreach (var combination in combinations)
        {
            var handResult = EvaluateFiveCardHand(combination.OrderByDescending(c => c.Rank).ToList());
            if (handResult.Score > bestHand.Score)
            {
                bestHand = handResult;
            }
        }
        
        return bestHand;
    }

    private static IEnumerable<List<Card>> GetCombinations(List<Card> cards, int count)
    {
        if (count == 0)
            yield return new List<Card>();
        else if (cards.Count >= count)
        {
            var first = cards[0];
            var rest = cards.Skip(1).ToList();
            
            foreach (var combination in GetCombinations(rest, count - 1))
            {
                yield return new List<Card> { first }.Concat(combination).ToList();
            }
            
            foreach (var combination in GetCombinations(rest, count))
            {
                yield return combination;
            }
        }
    }

    private static HandResult EvaluateFiveCardHand(List<Card> cards)
    {
        if (cards.Count != 5)
            throw new ArgumentException("Exactly 5 cards required for hand evaluation");

        cards = cards.OrderByDescending(c => c.Rank).ToList();

        // Check for flush
        bool isFlush = cards.All(c => c.Suit == cards[0].Suit);
        
        // Check for straight
        bool isStraight = IsStraight(cards);
        
        // Special case: A-2-3-4-5 straight (wheel)
        bool isWheel = cards.Select(c => c.Rank).SequenceEqual(new[] { Rank.Ace, Rank.Five, Rank.Four, Rank.Three, Rank.Two });
        if (isWheel) isStraight = true;

        // Group cards by rank
        var rankGroups = cards.GroupBy(c => c.Rank).OrderByDescending(g => g.Count()).ThenByDescending(g => g.Key).ToList();

        // Determine hand strength
        if (isFlush && isStraight)
        {
            if (IsRoyalFlush(cards))
            {
                return new HandResult 
                { 
                    Strength = HandStrength.RoyalFlush, 
                    Cards = cards,
                    Score = CalculateScore(HandStrength.RoyalFlush, cards, isWheel)
                };
            }
            return new HandResult 
            { 
                Strength = HandStrength.StraightFlush, 
                Cards = cards,
                Score = CalculateScore(HandStrength.StraightFlush, cards, isWheel)
            };
        }

        if (rankGroups[0].Count() == 4)
        {
            return new HandResult 
            { 
                Strength = HandStrength.FourOfAKind, 
                Cards = cards,
                Score = CalculateScore(HandStrength.FourOfAKind, cards, false, rankGroups)
            };
        }

        if (rankGroups[0].Count() == 3 && rankGroups[1].Count() == 2)
        {
            return new HandResult 
            { 
                Strength = HandStrength.FullHouse, 
                Cards = cards,
                Score = CalculateScore(HandStrength.FullHouse, cards, false, rankGroups)
            };
        }

        if (isFlush)
        {
            return new HandResult 
            { 
                Strength = HandStrength.Flush, 
                Cards = cards,
                Score = CalculateScore(HandStrength.Flush, cards)
            };
        }

        if (isStraight)
        {
            return new HandResult 
            { 
                Strength = HandStrength.Straight, 
                Cards = cards,
                Score = CalculateScore(HandStrength.Straight, cards, isWheel)
            };
        }

        if (rankGroups[0].Count() == 3)
        {
            return new HandResult 
            { 
                Strength = HandStrength.ThreeOfAKind, 
                Cards = cards,
                Score = CalculateScore(HandStrength.ThreeOfAKind, cards, false, rankGroups)
            };
        }

        if (rankGroups[0].Count() == 2 && rankGroups[1].Count() == 2)
        {
            return new HandResult 
            { 
                Strength = HandStrength.TwoPair, 
                Cards = cards,
                Score = CalculateScore(HandStrength.TwoPair, cards, false, rankGroups)
            };
        }

        if (rankGroups[0].Count() == 2)
        {
            return new HandResult 
            { 
                Strength = HandStrength.OnePair, 
                Cards = cards,
                Score = CalculateScore(HandStrength.OnePair, cards, false, rankGroups)
            };
        }

        return new HandResult 
        { 
            Strength = HandStrength.HighCard, 
            Cards = cards,
            Score = CalculateScore(HandStrength.HighCard, cards)
        };
    }

    private static bool IsStraight(List<Card> cards)
    {
        var ranks = cards.Select(c => (int)c.Rank).OrderDescending().ToList();
        
        for (int i = 0; i < ranks.Count - 1; i++)
        {
            if (ranks[i] - ranks[i + 1] != 1)
                return false;
        }
        return true;
    }

    private static bool IsRoyalFlush(List<Card> cards)
    {
        var ranks = cards.Select(c => c.Rank).OrderByDescending(r => r).ToList();
        return ranks.SequenceEqual(new[] { Rank.Ace, Rank.King, Rank.Queen, Rank.Jack, Rank.Ten });
    }

    private static long CalculateScore(HandStrength strength, List<Card> cards, bool isWheel = false, 
        List<IGrouping<Rank, Card>>? rankGroups = null)
    {
        // Base score based on hand strength (multiply by large number to ensure proper ranking)
        long baseScore = (long)strength * 100_000_000_000L;

        switch (strength)
        {
            case HandStrength.RoyalFlush:
                return baseScore; // All royal flushes are equal

            case HandStrength.StraightFlush:
            case HandStrength.Straight:
                if (isWheel)
                    return baseScore + (int)Rank.Five; // A-2-3-4-5 straight ranks by the 5
                return baseScore + (int)cards.Max(c => c.Rank);

            case HandStrength.FourOfAKind:
                if (rankGroups != null)
                {
                    var quadRank = rankGroups.First(g => g.Count() == 4).Key;
                    var kicker = rankGroups.First(g => g.Count() == 1).Key;
                    return baseScore + ((int)quadRank * 15) + (int)kicker;
                }
                break;

            case HandStrength.FullHouse:
                if (rankGroups != null)
                {
                    var tripRank = rankGroups.First(g => g.Count() == 3).Key;
                    var pairRank = rankGroups.First(g => g.Count() == 2).Key;
                    return baseScore + ((int)tripRank * 15) + (int)pairRank;
                }
                break;

            case HandStrength.Flush:
            case HandStrength.HighCard:
                // Rank by all five cards in order of importance
                var orderedRanks = cards.OrderByDescending(c => c.Rank).Select(c => (int)c.Rank).ToList();
                long score = baseScore;
                long multiplier = 15L * 15L * 15L * 15L; // 15^4
                
                foreach (var rank in orderedRanks)
                {
                    score += rank * multiplier;
                    multiplier /= 15L;
                }
                return score;

            case HandStrength.ThreeOfAKind:
                if (rankGroups != null)
                {
                    var tripRank = rankGroups.First(g => g.Count() == 3).Key;
                    var kickers = rankGroups.Where(g => g.Count() == 1).OrderByDescending(g => g.Key).Select(g => (int)g.Key).ToList();
                    return baseScore + ((int)tripRank * 15 * 15) + (kickers[0] * 15) + kickers[1];
                }
                break;

            case HandStrength.TwoPair:
                if (rankGroups != null)
                {
                    var pairs = rankGroups.Where(g => g.Count() == 2).OrderByDescending(g => g.Key).Select(g => (int)g.Key).ToList();
                    var kicker = rankGroups.First(g => g.Count() == 1).Key;
                    return baseScore + (pairs[0] * 15 * 15) + (pairs[1] * 15) + (int)kicker;
                }
                break;

            case HandStrength.OnePair:
                if (rankGroups != null)
                {
                    var pairRank = rankGroups.First(g => g.Count() == 2).Key;
                    var kickers = rankGroups.Where(g => g.Count() == 1).OrderByDescending(g => g.Key).Select(g => (int)g.Key).ToList();
                    long pairScore = baseScore + ((int)pairRank * 15L * 15L * 15L);
                    long kickerMultiplier = 15L * 15L;
                    
                    foreach (var kicker in kickers)
                    {
                        pairScore += kicker * kickerMultiplier;
                        kickerMultiplier /= 15L;
                    }
                    return pairScore;
                }
                break;
        }

        return baseScore;
    }

    public static int CompareHands(HandResult hand1, HandResult hand2)
    {
        return hand1.Score.CompareTo(hand2.Score);
    }

    public static string GetHandDescription(HandResult hand)
    {
        var cards = hand.Cards.OrderByDescending(c => c.Rank).ToList();
        
        return hand.Strength switch
        {
            HandStrength.RoyalFlush => "Royal Flush",
            HandStrength.StraightFlush => $"Straight Flush, {GetHighCardName(cards[0].Rank)} high",
            HandStrength.FourOfAKind => $"Four of a Kind, {GetRankName(cards.GroupBy(c => c.Rank).First(g => g.Count() == 4).Key)}s",
            HandStrength.FullHouse => GetFullHouseDescription(cards),
            HandStrength.Flush => $"Flush, {GetHighCardName(cards[0].Rank)} high",
            HandStrength.Straight => IsWheel(cards) ? "Straight, Five high" : $"Straight, {GetHighCardName(cards[0].Rank)} high",
            HandStrength.ThreeOfAKind => $"Three of a Kind, {GetRankName(cards.GroupBy(c => c.Rank).First(g => g.Count() == 3).Key)}s",
            HandStrength.TwoPair => GetTwoPairDescription(cards),
            HandStrength.OnePair => $"Pair of {GetRankName(cards.GroupBy(c => c.Rank).First(g => g.Count() == 2).Key)}s",
            HandStrength.HighCard => $"{GetHighCardName(cards[0].Rank)} high",
            _ => "Unknown hand"
        };
    }

    private static string GetFullHouseDescription(List<Card> cards)
    {
        var groups = cards.GroupBy(c => c.Rank).OrderByDescending(g => g.Count()).ToList();
        var trips = groups[0].Key;
        var pair = groups[1].Key;
        return $"Full House, {GetRankName(trips)}s over {GetRankName(pair)}s";
    }

    private static string GetTwoPairDescription(List<Card> cards)
    {
        var pairs = cards.GroupBy(c => c.Rank).Where(g => g.Count() == 2).OrderByDescending(g => g.Key).ToList();
        return $"Two Pair, {GetRankName(pairs[0].Key)}s and {GetRankName(pairs[1].Key)}s";
    }

    private static bool IsWheel(List<Card> cards)
    {
        var ranks = cards.Select(c => c.Rank).OrderByDescending(r => r).ToList();
        return ranks.SequenceEqual(new[] { Rank.Ace, Rank.Five, Rank.Four, Rank.Three, Rank.Two });
    }

    private static string GetRankName(Rank rank)
    {
        return rank switch
        {
            Rank.Ace => "Ace",
            Rank.King => "King", 
            Rank.Queen => "Queen",
            Rank.Jack => "Jack",
            _ => rank.ToString()
        };
    }

    private static string GetHighCardName(Rank rank)
    {
        return GetRankName(rank);
    }
}

/// <summary>
/// Record representing the result of a hand evaluation - immutable data transfer object
/// </summary>
public record HandResult
{
    public HandStrength Strength { get; init; }
    public IReadOnlyList<Card> Cards { get; init; } = [];
    public long Score { get; init; }
    public string Description => HandEvaluator.GetHandDescription(this);
}