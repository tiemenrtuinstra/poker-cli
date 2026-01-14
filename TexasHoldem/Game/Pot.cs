using TexasHoldem.Domain;
using TexasHoldem.Domain.Enums;
using TexasHoldem.Players;

namespace TexasHoldem.Game;

public class Pot
{
    private int _mainPot;
    private readonly List<SidePot> _sidePots;
    
    public int MainPotAmount => _mainPot;
    public List<SidePot> SidePots => _sidePots.ToList();
    public int TotalPotAmount => _mainPot + _sidePots.Sum(sp => sp.Amount);

    public Pot()
    {
        _mainPot = 0;
        _sidePots = new List<SidePot>();
    }

    public void AddToMainPot(int amount)
    {
        if (amount < 0)
            throw new ArgumentException("Cannot add negative amount to pot");
        
        _mainPot += amount;
    }

    public void AddContribution(IPlayer player, int amount)
    {
        if (amount <= 0) return;
        
        // For now, add everything to main pot
        // Side pot logic will be handled when players go all-in
        _mainPot += amount;
    }

    public void CreateSidePots(List<IPlayer> players, Dictionary<string, int> playerContributions)
    {
        _sidePots.Clear();
        
        // Find all-in players and their amounts
        var allInPlayers = players.Where(p => p.IsAllIn).ToList();
        if (!allInPlayers.Any()) return;

        // Sort all-in amounts from smallest to largest
        var allInAmounts = allInPlayers.Select(p => playerContributions.GetValueOrDefault(p.Name, 0))
            .Where(amount => amount > 0)
            .Distinct()
            .OrderBy(amount => amount)
            .ToList();

        if (!allInAmounts.Any()) return;

        int previousAmount = 0;
        
        foreach (var allInAmount in allInAmounts)
        {
            var sidePotContribution = allInAmount - previousAmount;
            if (sidePotContribution <= 0) continue;

            // Determine eligible players (those who contributed at least this amount)
            var eligiblePlayers = players
                .Where(p => playerContributions.GetValueOrDefault(p.Name, 0) >= allInAmount && !p.HasFolded)
                .Select(p => p.Name)
                .ToList();

            if (eligiblePlayers.Count > 1)
            {
                var sidePotAmount = sidePotContribution * eligiblePlayers.Count;
                
                _sidePots.Add(new SidePot
                {
                    Amount = sidePotAmount,
                    EligiblePlayers = eligiblePlayers
                });

                _mainPot -= sidePotAmount;
            }

            previousAmount = allInAmount;
        }
    }

    public List<PotWinner> DistributePots(List<IPlayer> players, Func<IPlayer, HandResult> getHandResult)
    {
        var winners = new List<PotWinner>();
        
        // Distribute side pots first (smallest to largest)
        foreach (var sidePot in _sidePots.OrderBy(sp => sp.Amount))
        {
            var eligiblePlayers = players.Where(p => sidePot.EligiblePlayers.Contains(p.Name) && !p.HasFolded).ToList();
            var sidePotWinners = DeterminePotWinners(eligiblePlayers, getHandResult);
            
            var amountPerWinner = sidePot.Amount / sidePotWinners.Count;
            var remainder = sidePot.Amount % sidePotWinners.Count;
            
            for (int i = 0; i < sidePotWinners.Count; i++)
            {
                var winAmount = amountPerWinner + (i < remainder ? 1 : 0); // Distribute remainder
                sidePotWinners[i].AddChips(winAmount);
                
                winners.Add(new PotWinner
                {
                    Player = sidePotWinners[i],
                    Amount = winAmount,
                    PotType = "Side Pot",
                    HandDescription = getHandResult(sidePotWinners[i]).Description
                });
            }
        }

        // Distribute main pot
        if (_mainPot > 0)
        {
            var activePlayers = players.Where(p => !p.HasFolded).ToList();
            var mainPotWinners = DeterminePotWinners(activePlayers, getHandResult);
            
            var amountPerWinner = _mainPot / mainPotWinners.Count;
            var remainder = _mainPot % mainPotWinners.Count;
            
            for (int i = 0; i < mainPotWinners.Count; i++)
            {
                var winAmount = amountPerWinner + (i < remainder ? 1 : 0);
                mainPotWinners[i].AddChips(winAmount);
                
                winners.Add(new PotWinner
                {
                    Player = mainPotWinners[i],
                    Amount = winAmount,
                    PotType = "Main Pot",
                    HandDescription = getHandResult(mainPotWinners[i]).Description
                });
            }
        }

        return winners;
    }

    private List<IPlayer> DeterminePotWinners(List<IPlayer> eligiblePlayers, Func<IPlayer, HandResult> getHandResult)
    {
        if (!eligiblePlayers.Any()) return new List<IPlayer>();
        
        // Evaluate all hands and find the best
        var playerHands = eligiblePlayers
            .Select(p => new { Player = p, Hand = getHandResult(p) })
            .OrderByDescending(ph => ph.Hand.Score)
            .ToList();

        // Find all players with the best hand (in case of ties)
        var bestScore = playerHands.First().Hand.Score;
        return playerHands
            .Where(ph => ph.Hand.Score == bestScore)
            .Select(ph => ph.Player)
            .ToList();
    }

    public void Reset()
    {
        _mainPot = 0;
        _sidePots.Clear();
    }

    public PotSummary GetSummary()
    {
        return new PotSummary
        {
            MainPot = _mainPot,
            SidePotsCount = _sidePots.Count,
            TotalAmount = TotalPotAmount,
            SidePotDetails = _sidePots.Select(sp => new SidePotSummary
            {
                Amount = sp.Amount,
                EligiblePlayersCount = sp.EligiblePlayers.Count,
                EligiblePlayerNames = sp.EligiblePlayers.ToList()
            }).ToList()
        };
    }

    public override string ToString()
    {
        if (_sidePots.Any())
        {
            return $"Total Pot: €{TotalPotAmount} (Main: €{_mainPot}, Side Pots: {_sidePots.Count})";
        }
        return $"Pot: €{TotalPotAmount}";
    }
}

public class PotWinner
{
    public IPlayer Player { get; set; } = null!;
    public int Amount { get; set; }
    public string PotType { get; set; } = string.Empty;
    public string HandDescription { get; set; } = string.Empty;
}

public class PotSummary
{
    public int MainPot { get; set; }
    public int SidePotsCount { get; set; }
    public int TotalAmount { get; set; }
    public List<SidePotSummary> SidePotDetails { get; set; } = new();
}

public class SidePotSummary
{
    public int Amount { get; set; }
    public int EligiblePlayersCount { get; set; }
    public List<string> EligiblePlayerNames { get; set; } = new();
}