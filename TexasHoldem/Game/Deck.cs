using TexasHoldem.Game.Enums;

namespace TexasHoldem.Game;

public class Deck
{
    private readonly List<Card> _cards;
    private readonly Random _random;
    private int _currentIndex;

    public Deck(Random? random = null)
    {
        _random = random ?? new Random();
        _cards = new List<Card>();
        _currentIndex = 0;
        InitializeDeck();
    }

    public int RemainingCards => _cards.Count - _currentIndex;
    public bool IsEmpty => _currentIndex >= _cards.Count;

    private void InitializeDeck()
    {
        _cards.Clear();
        _currentIndex = 0;

        foreach (Suit suit in Enum.GetValues<Suit>())
        {
            foreach (Rank rank in Enum.GetValues<Rank>())
            {
                _cards.Add(new Card(suit, rank));
            }
        }
    }

    public void Shuffle()
    {
        // Fisher-Yates shuffle algorithm
        for (int i = _cards.Count - 1; i > 0; i--)
        {
            int j = _random.Next(i + 1);
            (_cards[i], _cards[j]) = (_cards[j], _cards[i]);
        }
        _currentIndex = 0;
    }

    public Card DealCard()
    {
        if (IsEmpty)
            throw new InvalidOperationException("Cannot deal from an empty deck");

        return _cards[_currentIndex++];
    }

    public List<Card> DealCards(int count)
    {
        if (count > RemainingCards)
            throw new InvalidOperationException($"Not enough cards in deck. Requested: {count}, Available: {RemainingCards}");

        var dealtCards = new List<Card>();
        for (int i = 0; i < count; i++)
        {
            dealtCards.Add(DealCard());
        }
        return dealtCards;
    }

    public void Reset()
    {
        InitializeDeck();
        Shuffle();
    }

    public List<Card> GetRemainingCards()
    {
        return _cards.Skip(_currentIndex).ToList();
    }

    public override string ToString()
    {
        return $"Deck: {RemainingCards} cards remaining";
    }
}