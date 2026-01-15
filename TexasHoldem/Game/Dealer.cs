using TexasHoldem.Domain;
using TexasHoldem.Domain.Enums;
using TexasHoldem.Players;

namespace TexasHoldem.Game;

public class Dealer
{
    private readonly Deck _deck;
    private readonly Random _random;
    
    public int DealerPosition { get; private set; }
    
    public Dealer(Random? random = null)
    {
        _random = random ?? new Random();
        _deck = new Deck(_random);
        DealerPosition = 0;
    }

    public void ShuffleDeck()
    {
        _deck.Reset(); // Reset and shuffle
        Console.WriteLine("🃏 Dealer shuffles the deck...");
    }

    public void DealHoleCards(List<IPlayer> players)
    {
        // Shuffle deck before each hand (just like real poker)
        _deck.Reset();

        if (_deck.RemainingCards < players.Count * 2)
        {
            throw new InvalidOperationException("Not enough cards to deal hole cards to all players");
        }

        Console.WriteLine("🃏 Dealing hole cards...");
        
        // Deal two cards to each player
        for (int cardRound = 0; cardRound < 2; cardRound++)
        {
            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];
                if (player.IsActive && !player.HasFolded)
                {
                    var card = _deck.DealCard();
                    if (cardRound == 0)
                    {
                        player.HoleCards.Clear(); // Start fresh
                    }
                    player.HoleCards.Add(card);
                }
            }
        }

        // Simulate dealing delay
        Thread.Sleep(1000);
    }

    public List<Card> DealFlop()
    {
        if (_deck.RemainingCards < 4) // 1 burn + 3 flop
        {
            throw new InvalidOperationException("Not enough cards for the flop");
        }

        Console.WriteLine("🔥 Burning a card...");
        _deck.DealCard(); // Burn card
        
        Console.WriteLine("🃏 Dealing the flop...");
        var flop = _deck.DealCards(3);
        
        Thread.Sleep(1500); // Dramatic pause
        return flop;
    }

    public Card DealTurn()
    {
        if (_deck.RemainingCards < 2) // 1 burn + 1 turn
        {
            throw new InvalidOperationException("Not enough cards for the turn");
        }

        Console.WriteLine("🔥 Burning a card...");
        _deck.DealCard(); // Burn card
        
        Console.WriteLine("🃏 Dealing the turn...");
        var turn = _deck.DealCard();
        
        Thread.Sleep(1500);
        return turn;
    }

    public Card DealRiver()
    {
        if (_deck.RemainingCards < 2) // 1 burn + 1 river
        {
            throw new InvalidOperationException("Not enough cards for the river");
        }

        Console.WriteLine("🔥 Burning a card...");
        _deck.DealCard(); // Burn card
        
        Console.WriteLine("🃏 Dealing the river...");
        var river = _deck.DealCard();
        
        Thread.Sleep(1500);
        return river;
    }

    public void MoveDealerButton(List<IPlayer> players)
    {
        if (players.Count <= 1) return;

        int attempts = 0;
        do
        {
            DealerPosition = (DealerPosition + 1) % players.Count;
            attempts++;
            
            // Prevent infinite loop if all players are inactive
            if (attempts >= players.Count)
            {
                // Find any active player
                var activePlayer = players.FirstOrDefault(p => p.IsActive && p.Chips > 0);
                if (activePlayer != null)
                {
                    DealerPosition = players.IndexOf(activePlayer);
                }
                break;
            }
        } while (!players[DealerPosition].IsActive || players[DealerPosition].Chips <= 0);

        Console.WriteLine($"🔘 Dealer button moves to {players[DealerPosition].Name}");
    }

    public (int SmallBlindPos, int BigBlindPos) GetBlindPositions(List<IPlayer> players)
    {
        if (players.Count < 2)
            throw new InvalidOperationException("Need at least 2 players for blinds");

        var activePlayers = players.Where(p => p.IsActive && p.Chips > 0).ToList();
        if (activePlayers.Count < 2)
            throw new InvalidOperationException("Need at least 2 active players for blinds");

        int smallBlindPos = DealerPosition;
        int bigBlindPos = DealerPosition;

        if (players.Count == 2)
        {
            // Heads up: dealer posts small blind
            smallBlindPos = DealerPosition;
            bigBlindPos = (DealerPosition + 1) % players.Count;
        }
        else
        {
            // Multi-way: small blind is next to dealer, big blind is after small blind
            smallBlindPos = GetNextActivePlayer(players, DealerPosition);
            bigBlindPos = GetNextActivePlayer(players, smallBlindPos);
        }

        return (smallBlindPos, bigBlindPos);
    }

    private int GetNextActivePlayer(List<IPlayer> players, int currentPos)
    {
        int nextPos = (currentPos + 1) % players.Count;
        int attempts = 0;

        while ((!players[nextPos].IsActive || players[nextPos].Chips <= 0) && attempts < players.Count)
        {
            nextPos = (nextPos + 1) % players.Count;
            attempts++;
        }

        if (attempts >= players.Count)
        {
            throw new InvalidOperationException("No active players found");
        }

        return nextPos;
    }

    public void PostBlinds(List<IPlayer> players, int smallBlindAmount, int bigBlindAmount)
    {
        var (smallBlindPos, bigBlindPos) = GetBlindPositions(players);

        var smallBlindPlayer = players[smallBlindPos];
        var bigBlindPlayer = players[bigBlindPos];

        // Post small blind - check for all-in BEFORE removing chips
        var actualSmallBlind = Math.Min(smallBlindAmount, smallBlindPlayer.Chips);
        var smallBlindGoesAllIn = actualSmallBlind == smallBlindPlayer.Chips;
        smallBlindPlayer.RemoveChips(actualSmallBlind);
        if (smallBlindGoesAllIn)
        {
            smallBlindPlayer.IsAllIn = true;
            Console.WriteLine($"💰 {smallBlindPlayer.Name} posts small blind €{actualSmallBlind} and is ALL-IN!");
        }
        else
        {
            Console.WriteLine($"💰 {smallBlindPlayer.Name} posts small blind €{actualSmallBlind}");
        }

        // Post big blind - check for all-in BEFORE removing chips
        var actualBigBlind = Math.Min(bigBlindAmount, bigBlindPlayer.Chips);
        var bigBlindGoesAllIn = actualBigBlind == bigBlindPlayer.Chips;
        bigBlindPlayer.RemoveChips(actualBigBlind);
        if (bigBlindGoesAllIn)
        {
            bigBlindPlayer.IsAllIn = true;
            Console.WriteLine($"💰 {bigBlindPlayer.Name} posts big blind €{actualBigBlind} and is ALL-IN!");
        }
        else
        {
            Console.WriteLine($"💰 {bigBlindPlayer.Name} posts big blind €{actualBigBlind}");
        }
    }

    public void PostAntes(List<IPlayer> players, int anteAmount)
    {
        if (anteAmount <= 0) return;

        Console.WriteLine($"💰 Posting antes of €{anteAmount}...");

        foreach (var player in players.Where(p => p.IsActive && p.Chips > 0))
        {
            var actualAnte = Math.Min(anteAmount, player.Chips);
            var goesAllIn = actualAnte == player.Chips; // Check BEFORE removing chips
            player.RemoveChips(actualAnte);

            if (goesAllIn)
            {
                player.IsAllIn = true;
                Console.WriteLine($"  {player.Name} posts ante €{actualAnte} and is ALL-IN!");
            }
        }
    }

    public int GetNextPlayerPosition(List<IPlayer> players, int currentPos)
    {
        return GetNextActivePlayer(players, currentPos);
    }

    public void SetDealerPosition(int position)
    {
        DealerPosition = position;
    }

    public int GetRemainingCards()
    {
        return _deck.RemainingCards;
    }

    public void ShowDeckStatus()
    {
        Console.WriteLine($"📊 Deck status: {_deck.RemainingCards} cards remaining");
    }

    public override string ToString()
    {
        return $"Dealer (Button at position {DealerPosition}, {_deck.RemainingCards} cards left)";
    }
}