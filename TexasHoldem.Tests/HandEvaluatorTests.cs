using TexasHoldem.Domain;
using TexasHoldem.Domain.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TexasHoldem.Tests;

[TestClass]
public class HandEvaluatorTests
{
    private HandEvaluator _evaluator;

    [TestInitialize]
    public void Setup()
    {
        _evaluator = new HandEvaluator();
    }

    // Helper method to create cards from strings
    private List<Card> CreateCards(params string[] cardStrings)
    {
        return cardStrings.Select(Card.Parse).ToList();
    }

    [TestMethod]
    public void EvaluateHand_RoyalFlush_ReturnsCorrectHandStrength()
    {
        // Arrange: Ten, Jack, Queen, King, Ace of Spades
        var cards = CreateCards("10♠", "J♠", "Q♠", "K♠", "A♠", "2♥", "3♦");

        // Act
        var (handStrength, score, description) = _evaluator.EvaluateHand(cards);

        // Assert
        Assert.AreEqual(HandStrength.RoyalFlush, handStrength);
        Assert.IsTrue(description.Contains("Royal Flush"));
        Assert.IsTrue(score > 0);
    }

    [TestMethod]
    public void EvaluateHand_StraightFlush_ReturnsCorrectHandStrength()
    {
        // Arrange: 5-6-7-8-9 of Hearts
        var cards = CreateCards("5♥", "6♥", "7♥", "8♥", "9♥", "K♠", "2♦");

        // Act
        var (handStrength, score, description) = _evaluator.EvaluateHand(cards);

        // Assert
        Assert.AreEqual(HandStrength.StraightFlush, handStrength);
        Assert.IsTrue(description.Contains("Straight Flush"));
    }

    [TestMethod]
    public void EvaluateHand_FourOfAKind_ReturnsCorrectHandStrength()
    {
        // Arrange: Four Aces
        var cards = CreateCards("A♠", "A♥", "A♦", "A♣", "K♠", "Q♥", "J♦");

        // Act
        var (handStrength, score, description) = _evaluator.EvaluateHand(cards);

        // Assert
        Assert.AreEqual(HandStrength.FourOfAKind, handStrength);
        Assert.IsTrue(description.Contains("Four of a Kind"));
    }

    [TestMethod]
    public void EvaluateHand_FullHouse_ReturnsCorrectHandStrength()
    {
        // Arrange: Three Kings, Two Queens
        var cards = CreateCards("K♠", "K♥", "K♦", "Q♠", "Q♥", "J♦", "2♠");

        // Act
        var (handStrength, score, description) = _evaluator.EvaluateHand(cards);

        // Assert
        Assert.AreEqual(HandStrength.FullHouse, handStrength);
        Assert.IsTrue(description.Contains("Full House"));
    }

    [TestMethod]
    public void EvaluateHand_Flush_ReturnsCorrectHandStrength()
    {
        // Arrange: Five Spades (not in sequence)
        var cards = CreateCards("2♠", "4♠", "7♠", "10♠", "K♠", "A♥", "3♦");

        // Act
        var (handStrength, score, description) = _evaluator.EvaluateHand(cards);

        // Assert
        Assert.AreEqual(HandStrength.Flush, handStrength);
        Assert.IsTrue(description.Contains("Flush"));
    }

    [TestMethod]
    public void EvaluateHand_Straight_ReturnsCorrectHandStrength()
    {
        // Arrange: 5-6-7-8-9 mixed suits
        var cards = CreateCards("5♠", "6♥", "7♦", "8♠", "9♣", "K♠", "2♥");

        // Act
        var (handStrength, score, description) = _evaluator.EvaluateHand(cards);

        // Assert
        Assert.AreEqual(HandStrength.Straight, handStrength);
        Assert.IsTrue(description.Contains("Straight"));
    }

    [TestMethod]
    public void EvaluateHand_AceLowStraight_ReturnsCorrectHandStrength()
    {
        // Arrange: A-2-3-4-5 (wheel)
        var cards = CreateCards("A♠", "2♥", "3♦", "4♠", "5♣", "K♠", "Q♥");

        // Act
        var (handStrength, score, description) = _evaluator.EvaluateHand(cards);

        // Assert
        Assert.AreEqual(HandStrength.Straight, handStrength);
        Assert.IsTrue(description.Contains("Straight"));
    }

    [TestMethod]
    public void EvaluateHand_ThreeOfAKind_ReturnsCorrectHandStrength()
    {
        // Arrange: Three Jacks
        var cards = CreateCards("J♠", "J♥", "J♦", "K♠", "Q♥", "9♦", "2♠");

        // Act
        var (handStrength, score, description) = _evaluator.EvaluateHand(cards);

        // Assert
        Assert.AreEqual(HandStrength.ThreeOfAKind, handStrength);
        Assert.IsTrue(description.Contains("Three of a Kind"));
    }

    [TestMethod]
    public void EvaluateHand_TwoPair_ReturnsCorrectHandStrength()
    {
        // Arrange: Two Kings, Two Queens
        var cards = CreateCards("K♠", "K♥", "Q♦", "Q♠", "J♥", "9♦", "2♠");

        // Act
        var (handStrength, score, description) = _evaluator.EvaluateHand(cards);

        // Assert
        Assert.AreEqual(HandStrength.TwoPair, handStrength);
        Assert.IsTrue(description.Contains("Two Pair"));
    }

    [TestMethod]
    public void EvaluateHand_OnePair_ReturnsCorrectHandStrength()
    {
        // Arrange: Two Aces
        var cards = CreateCards("A♠", "A♥", "K♦", "Q♠", "J♥", "9♦", "2♠");

        // Act
        var (handStrength, score, description) = _evaluator.EvaluateHand(cards);

        // Assert
        Assert.AreEqual(HandStrength.OnePair, handStrength);
        Assert.IsTrue(description.Contains("Pair"));
    }

    [TestMethod]
    public void EvaluateHand_HighCard_ReturnsCorrectHandStrength()
    {
        // Arrange: No pairs, no straight, no flush
        var cards = CreateCards("A♠", "K♥", "Q♦", "J♠", "9♥", "7♦", "2♠");

        // Act
        var (handStrength, score, description) = _evaluator.EvaluateHand(cards);

        // Assert
        Assert.AreEqual(HandStrength.HighCard, handStrength);
        Assert.IsTrue(description.Contains("High Card"));
    }

    [TestMethod]
    public void EvaluateHand_SevenCards_FindsBestFiveCardHand()
    {
        // Arrange: Seven cards with a flush in spades
        var cards = CreateCards("A♠", "K♠", "Q♠", "J♠", "10♠", "9♥", "2♦");

        // Act
        var (handStrength, score, description) = _evaluator.EvaluateHand(cards);

        // Assert
        Assert.AreEqual(HandStrength.RoyalFlush, handStrength);
        Assert.IsTrue(description.Contains("Royal Flush"));
    }

    [TestMethod]
    public void CompareHands_HigherHandWins()
    {
        // Arrange
        var royalFlush = CreateCards("10♠", "J♠", "Q♠", "K♠", "A♠");
        var fourOfAKind = CreateCards("K♠", "K♥", "K♦", "K♣", "Q♠");

        // Act
        var royalResult = _evaluator.EvaluateHand(royalFlush);
        var fourKindResult = _evaluator.EvaluateHand(fourOfAKind);

        // Assert
        Assert.IsTrue(royalResult.Score > fourKindResult.Score);
    }

    [TestMethod]
    public void CompareHands_SameHandType_HigherRankWins()
    {
        // Arrange: Two pairs - Aces vs Kings
        var acePair = CreateCards("A♠", "A♥", "K♦", "Q♠", "J♥");
        var kingPair = CreateCards("K♠", "K♥", "A♦", "Q♠", "J♥");

        // Act
        var aceResult = _evaluator.EvaluateHand(acePair);
        var kingResult = _evaluator.EvaluateHand(kingPair);

        // Assert
        Assert.AreEqual(HandStrength.OnePair, aceResult.HandStrength);
        Assert.AreEqual(HandStrength.OnePair, kingResult.HandStrength);
        Assert.IsTrue(aceResult.Score > kingResult.Score);
    }

    [TestMethod]
    public void CompareHands_SameHandType_KickerMatters()
    {
        // Arrange: Same pair, different kickers
        var aceKingKicker = CreateCards("A♠", "A♥", "K♦", "Q♠", "J♥");
        var aceQueenKicker = CreateCards("A♠", "A♥", "Q♦", "J♠", "10♥");

        // Act
        var kingKickerResult = _evaluator.EvaluateHand(aceKingKicker);
        var queenKickerResult = _evaluator.EvaluateHand(aceQueenKicker);

        // Assert
        Assert.AreEqual(HandStrength.OnePair, kingKickerResult.HandStrength);
        Assert.AreEqual(HandStrength.OnePair, queenKickerResult.HandStrength);
        Assert.IsTrue(kingKickerResult.Score > queenKickerResult.Score);
    }

    [TestMethod]
    public void EvaluateHand_TwoPair_CorrectRanking()
    {
        // Arrange: Aces and Kings vs Aces and Queens
        var acesKings = CreateCards("A♠", "A♥", "K♦", "K♠", "Q♥");
        var acesQueens = CreateCards("A♠", "A♥", "Q♦", "Q♠", "K♥");

        // Act
        var acesKingsResult = _evaluator.EvaluateHand(acesKings);
        var acesQueensResult = _evaluator.EvaluateHand(acesQueens);

        // Assert
        Assert.AreEqual(HandStrength.TwoPair, acesKingsResult.HandStrength);
        Assert.AreEqual(HandStrength.TwoPair, acesQueensResult.HandStrength);
        Assert.IsTrue(acesKingsResult.Score > acesQueensResult.Score);
    }

    [TestMethod]
    public void EvaluateHand_FullHouse_CorrectRanking()
    {
        // Arrange: Aces full of Kings vs Kings full of Aces
        var acesFull = CreateCards("A♠", "A♥", "A♦", "K♠", "K♥");
        var kingsFull = CreateCards("K♠", "K♥", "K♦", "A♠", "A♥");

        // Act
        var acesFullResult = _evaluator.EvaluateHand(acesFull);
        var kingsFullResult = _evaluator.EvaluateHand(kingsFull);

        // Assert
        Assert.AreEqual(HandStrength.FullHouse, acesFullResult.HandStrength);
        Assert.AreEqual(HandStrength.FullHouse, kingsFullResult.HandStrength);
        Assert.IsTrue(acesFullResult.Score > kingsFullResult.Score);
    }

    [TestMethod]
    public void EvaluateHand_StraightFlush_CorrectRanking()
    {
        // Arrange: 9-high straight flush vs 8-high straight flush
        var nineHighSF = CreateCards("5♠", "6♠", "7♠", "8♠", "9♠");
        var eightHighSF = CreateCards("4♠", "5♠", "6♠", "7♠", "8♠");

        // Act
        var nineResult = _evaluator.EvaluateHand(nineHighSF);
        var eightResult = _evaluator.EvaluateHand(eightHighSF);

        // Assert
        Assert.AreEqual(HandStrength.StraightFlush, nineResult.HandStrength);
        Assert.AreEqual(HandStrength.StraightFlush, eightResult.HandStrength);
        Assert.IsTrue(nineResult.Score > eightResult.Score);
    }

    [TestMethod]
    public void EvaluateHand_EdgeCase_EmptyCards_ThrowsException()
    {
        // Arrange
        var cards = new List<Card>();

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => _evaluator.EvaluateHand(cards));
    }

    [TestMethod]
    public void EvaluateHand_EdgeCase_LessThanFiveCards_ThrowsException()
    {
        // Arrange
        var cards = CreateCards("A♠", "K♥", "Q♦", "J♠");

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => _evaluator.EvaluateHand(cards));
    }

    [TestMethod]
    public void EvaluateHand_ExactlyFiveCards_Works()
    {
        // Arrange
        var cards = CreateCards("A♠", "K♥", "Q♦", "J♠", "10♥");

        // Act
        var result = _evaluator.EvaluateHand(cards);

        // Assert
        Assert.AreEqual(HandStrength.Straight, result.HandStrength);
    }

    [TestMethod]
    public void EvaluateHand_FlushBeatsSTRAIGHT()
    {
        // Arrange
        var flush = CreateCards("2♠", "4♠", "6♠", "8♠", "10♠");
        var straight = CreateCards("A♠", "2♥", "3♦", "4♠", "5♣");

        // Act
        var flushResult = _evaluator.EvaluateHand(flush);
        var straightResult = _evaluator.EvaluateHand(straight);

        // Assert
        Assert.AreEqual(HandStrength.Flush, flushResult.HandStrength);
        Assert.AreEqual(HandStrength.Straight, straightResult.HandStrength);
        Assert.IsTrue(flushResult.Score > straightResult.Score);
    }

    [TestMethod]
    public void EvaluateHand_DescriptionContainsRelevantCards()
    {
        // Arrange: Full house
        var cards = CreateCards("K♠", "K♥", "K♦", "Q♠", "Q♥");

        // Act
        var (_, _, description) = _evaluator.EvaluateHand(cards);

        // Assert
        Assert.IsTrue(description.Contains("Kings"));
        Assert.IsTrue(description.Contains("Queens"));
    }

    [TestMethod]
    public void EvaluateHand_MultipleIdenticalHands_SameScore()
    {
        // Arrange: Same hand, different suits
        var hand1 = CreateCards("A♠", "A♥", "K♦", "Q♠", "J♥");
        var hand2 = CreateCards("A♦", "A♣", "K♠", "Q♥", "J♠");

        // Act
        var result1 = _evaluator.EvaluateHand(hand1);
        var result2 = _evaluator.EvaluateHand(hand2);

        // Assert
        Assert.AreEqual(result1.Score, result2.Score);
        Assert.AreEqual(result1.HandStrength, result2.HandStrength);
    }
}