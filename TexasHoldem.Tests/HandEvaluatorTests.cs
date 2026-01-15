using TexasHoldem.Domain;
using TexasHoldem.Domain.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TexasHoldem.Tests;

[TestClass]
public class HandEvaluatorTests
{
    // Helper method to create cards from strings
    private List<Card> CreateCards(params string[] cardStrings)
    {
        return cardStrings.Select(Card.Parse).ToList();
    }

    [TestMethod]
    public void EvaluateHand_RoyalFlush_ReturnsCorrectHandStrength()
    {
        // Arrange: Ten, Jack, Queen, King, Ace of Spades
        var cards = CreateCards("10S", "JS", "QS", "KS", "AS", "2H", "3D");

        // Act
        var result = HandEvaluator.EvaluateHand(cards);

        // Assert
        Assert.AreEqual(HandStrength.RoyalFlush, result.Strength);
        Assert.IsTrue(result.Description.Contains("Royal Flush"));
        Assert.IsTrue(result.Score > 0);
    }

    [TestMethod]
    public void EvaluateHand_StraightFlush_ReturnsCorrectHandStrength()
    {
        // Arrange: 5-6-7-8-9 of Hearts
        var cards = CreateCards("5H", "6H", "7H", "8H", "9H", "KS", "2D");

        // Act
        var result = HandEvaluator.EvaluateHand(cards);

        // Assert
        Assert.AreEqual(HandStrength.StraightFlush, result.Strength);
        Assert.IsTrue(result.Description.Contains("Straight Flush"));
    }

    [TestMethod]
    public void EvaluateHand_FourOfAKind_ReturnsCorrectHandStrength()
    {
        // Arrange: Four Aces
        var cards = CreateCards("AS", "AH", "AD", "AC", "KS", "QH", "JD");

        // Act
        var result = HandEvaluator.EvaluateHand(cards);

        // Assert
        Assert.AreEqual(HandStrength.FourOfAKind, result.Strength);
        Assert.IsTrue(result.Description.Contains("Four of a Kind"));
    }

    [TestMethod]
    public void EvaluateHand_FullHouse_ReturnsCorrectHandStrength()
    {
        // Arrange: Three Kings, Two Queens
        var cards = CreateCards("KS", "KH", "KD", "QS", "QH", "JD", "2S");

        // Act
        var result = HandEvaluator.EvaluateHand(cards);

        // Assert
        Assert.AreEqual(HandStrength.FullHouse, result.Strength);
        Assert.IsTrue(result.Description.Contains("Full House"));
    }

    [TestMethod]
    public void EvaluateHand_Flush_ReturnsCorrectHandStrength()
    {
        // Arrange: Five Spades (not in sequence)
        var cards = CreateCards("2S", "4S", "7S", "10S", "KS", "AH", "3D");

        // Act
        var result = HandEvaluator.EvaluateHand(cards);

        // Assert
        Assert.AreEqual(HandStrength.Flush, result.Strength);
        Assert.IsTrue(result.Description.Contains("Flush"));
    }

    [TestMethod]
    public void EvaluateHand_Straight_ReturnsCorrectHandStrength()
    {
        // Arrange: 5-6-7-8-9 mixed suits
        var cards = CreateCards("5S", "6H", "7D", "8S", "9C", "KS", "2H");

        // Act
        var result = HandEvaluator.EvaluateHand(cards);

        // Assert
        Assert.AreEqual(HandStrength.Straight, result.Strength);
        Assert.IsTrue(result.Description.Contains("Straight"));
    }

    [TestMethod]
    public void EvaluateHand_AceLowStraight_ReturnsCorrectHandStrength()
    {
        // Arrange: A-2-3-4-5 (wheel)
        var cards = CreateCards("AS", "2H", "3D", "4S", "5C", "KS", "QH");

        // Act
        var result = HandEvaluator.EvaluateHand(cards);

        // Assert
        Assert.AreEqual(HandStrength.Straight, result.Strength);
        Assert.IsTrue(result.Description.Contains("Straight"));
    }

    [TestMethod]
    public void EvaluateHand_ThreeOfAKind_ReturnsCorrectHandStrength()
    {
        // Arrange: Three Jacks
        var cards = CreateCards("JS", "JH", "JD", "KS", "QH", "9D", "2S");

        // Act
        var result = HandEvaluator.EvaluateHand(cards);

        // Assert
        Assert.AreEqual(HandStrength.ThreeOfAKind, result.Strength);
        Assert.IsTrue(result.Description.Contains("Three of a Kind"));
    }

    [TestMethod]
    public void EvaluateHand_TwoPair_ReturnsCorrectHandStrength()
    {
        // Arrange: Two Kings, Two Queens
        var cards = CreateCards("KS", "KH", "QD", "QS", "JH", "9D", "2S");

        // Act
        var result = HandEvaluator.EvaluateHand(cards);

        // Assert
        Assert.AreEqual(HandStrength.TwoPair, result.Strength);
        Assert.IsTrue(result.Description.Contains("Two Pair"));
    }

    [TestMethod]
    public void EvaluateHand_OnePair_ReturnsCorrectHandStrength()
    {
        // Arrange: Two Aces
        var cards = CreateCards("AS", "AH", "KD", "QS", "JH", "9D", "2S");

        // Act
        var result = HandEvaluator.EvaluateHand(cards);

        // Assert
        Assert.AreEqual(HandStrength.OnePair, result.Strength);
        Assert.IsTrue(result.Description.Contains("Pair"));
    }

    [TestMethod]
    public void EvaluateHand_HighCard_ReturnsCorrectHandStrength()
    {
        // Arrange: No pairs, no straight, no flush
        var cards = CreateCards("AS", "KH", "QD", "JS", "9H", "7D", "2S");

        // Act
        var result = HandEvaluator.EvaluateHand(cards);

        // Assert
        Assert.AreEqual(HandStrength.HighCard, result.Strength);
        Assert.IsTrue(result.Description.Contains("high")); // e.g., "Ace high"
    }

    [TestMethod]
    public void EvaluateHand_SevenCards_FindsBestFiveCardHand()
    {
        // Arrange: Seven cards with a flush in spades
        var cards = CreateCards("AS", "KS", "QS", "JS", "10S", "9H", "2D");

        // Act
        var result = HandEvaluator.EvaluateHand(cards);

        // Assert
        Assert.AreEqual(HandStrength.RoyalFlush, result.Strength);
        Assert.IsTrue(result.Description.Contains("Royal Flush"));
    }

    [TestMethod]
    public void CompareHands_HigherHandWins()
    {
        // Arrange
        var royalFlush = CreateCards("10S", "JS", "QS", "KS", "AS");
        var fourOfAKind = CreateCards("KS", "KH", "KD", "KC", "QS");

        // Act
        var royalResult = HandEvaluator.EvaluateHand(royalFlush);
        var fourKindResult = HandEvaluator.EvaluateHand(fourOfAKind);

        // Assert
        Assert.IsTrue(royalResult.Score > fourKindResult.Score);
    }

    [TestMethod]
    public void CompareHands_SameHandType_HigherRankWins()
    {
        // Arrange: Two pairs - Aces vs Kings
        var acePair = CreateCards("AS", "AH", "KD", "QS", "JH");
        var kingPair = CreateCards("KS", "KH", "AD", "QS", "JH");

        // Act
        var aceResult = HandEvaluator.EvaluateHand(acePair);
        var kingResult = HandEvaluator.EvaluateHand(kingPair);

        // Assert
        Assert.AreEqual(HandStrength.OnePair, aceResult.Strength);
        Assert.AreEqual(HandStrength.OnePair, kingResult.Strength);
        Assert.IsTrue(aceResult.Score > kingResult.Score);
    }

    [TestMethod]
    public void CompareHands_SameHandType_KickerMatters()
    {
        // Arrange: Same pair, different kickers
        var aceKingKicker = CreateCards("AS", "AH", "KD", "QS", "JH");
        var aceQueenKicker = CreateCards("AS", "AH", "QD", "JS", "10H");

        // Act
        var kingKickerResult = HandEvaluator.EvaluateHand(aceKingKicker);
        var queenKickerResult = HandEvaluator.EvaluateHand(aceQueenKicker);

        // Assert
        Assert.AreEqual(HandStrength.OnePair, kingKickerResult.Strength);
        Assert.AreEqual(HandStrength.OnePair, queenKickerResult.Strength);
        Assert.IsTrue(kingKickerResult.Score > queenKickerResult.Score);
    }

    [TestMethod]
    public void EvaluateHand_TwoPair_CorrectRanking()
    {
        // Arrange: Aces and Kings vs Aces and Queens
        var acesKings = CreateCards("AS", "AH", "KD", "KS", "QH");
        var acesQueens = CreateCards("AS", "AH", "QD", "QS", "KH");

        // Act
        var acesKingsResult = HandEvaluator.EvaluateHand(acesKings);
        var acesQueensResult = HandEvaluator.EvaluateHand(acesQueens);

        // Assert
        Assert.AreEqual(HandStrength.TwoPair, acesKingsResult.Strength);
        Assert.AreEqual(HandStrength.TwoPair, acesQueensResult.Strength);
        Assert.IsTrue(acesKingsResult.Score > acesQueensResult.Score);
    }

    [TestMethod]
    public void EvaluateHand_FullHouse_CorrectRanking()
    {
        // Arrange: Aces full of Kings vs Kings full of Aces
        var acesFull = CreateCards("AS", "AH", "AD", "KS", "KH");
        var kingsFull = CreateCards("KS", "KH", "KD", "AS", "AH");

        // Act
        var acesFullResult = HandEvaluator.EvaluateHand(acesFull);
        var kingsFullResult = HandEvaluator.EvaluateHand(kingsFull);

        // Assert
        Assert.AreEqual(HandStrength.FullHouse, acesFullResult.Strength);
        Assert.AreEqual(HandStrength.FullHouse, kingsFullResult.Strength);
        Assert.IsTrue(acesFullResult.Score > kingsFullResult.Score);
    }

    [TestMethod]
    public void EvaluateHand_StraightFlush_CorrectRanking()
    {
        // Arrange: 9-high straight flush vs 8-high straight flush
        var nineHighSF = CreateCards("5S", "6S", "7S", "8S", "9S");
        var eightHighSF = CreateCards("4S", "5S", "6S", "7S", "8S");

        // Act
        var nineResult = HandEvaluator.EvaluateHand(nineHighSF);
        var eightResult = HandEvaluator.EvaluateHand(eightHighSF);

        // Assert
        Assert.AreEqual(HandStrength.StraightFlush, nineResult.Strength);
        Assert.AreEqual(HandStrength.StraightFlush, eightResult.Strength);
        Assert.IsTrue(nineResult.Score > eightResult.Score);
    }

    [TestMethod]
    public void EvaluateHand_EdgeCase_EmptyCards_ThrowsException()
    {
        // Arrange
        var cards = new List<Card>();

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => HandEvaluator.EvaluateHand(cards));
    }

    [TestMethod]
    public void EvaluateHand_EdgeCase_LessThanFiveCards_ThrowsException()
    {
        // Arrange
        var cards = CreateCards("AS", "KH", "QD", "JS");

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => HandEvaluator.EvaluateHand(cards));
    }

    [TestMethod]
    public void EvaluateHand_ExactlyFiveCards_Works()
    {
        // Arrange
        var cards = CreateCards("AS", "KH", "QD", "JS", "10H");

        // Act
        var result = HandEvaluator.EvaluateHand(cards);

        // Assert
        Assert.AreEqual(HandStrength.Straight, result.Strength);
    }

    [TestMethod]
    public void EvaluateHand_FlushBeatsStraight()
    {
        // Arrange
        var flush = CreateCards("2S", "4S", "6S", "8S", "10S");
        var straight = CreateCards("AS", "2H", "3D", "4S", "5C");

        // Act
        var flushResult = HandEvaluator.EvaluateHand(flush);
        var straightResult = HandEvaluator.EvaluateHand(straight);

        // Assert
        Assert.AreEqual(HandStrength.Flush, flushResult.Strength);
        Assert.AreEqual(HandStrength.Straight, straightResult.Strength);
        Assert.IsTrue(flushResult.Score > straightResult.Score);
    }

    [TestMethod]
    public void EvaluateHand_DescriptionContainsRelevantCards()
    {
        // Arrange: Full house
        var cards = CreateCards("KS", "KH", "KD", "QS", "QH");

        // Act
        var result = HandEvaluator.EvaluateHand(cards);

        // Assert
        Assert.IsTrue(result.Description.Contains("Kings"));
        Assert.IsTrue(result.Description.Contains("Queens"));
    }

    [TestMethod]
    public void EvaluateHand_MultipleIdenticalHands_SameScore()
    {
        // Arrange: Same hand, different suits
        var hand1 = CreateCards("AS", "AH", "KD", "QS", "JH");
        var hand2 = CreateCards("AD", "AC", "KS", "QH", "JS");

        // Act
        var result1 = HandEvaluator.EvaluateHand(hand1);
        var result2 = HandEvaluator.EvaluateHand(hand2);

        // Assert
        Assert.AreEqual(result1.Score, result2.Score);
        Assert.AreEqual(result1.Strength, result2.Strength);
    }
}
