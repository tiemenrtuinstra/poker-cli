using TexasHoldem.Domain;
using TexasHoldem.Domain.Enums;
using TexasHoldem.Players;

namespace TexasHoldem.Game;

public class BettingLogic
{
    public static bool IsValidAction(IPlayer player, ActionType action, int amount, GameState gameState)
    {
        if (player.HasFolded) return action == ActionType.Fold;
        if (player.IsAllIn) return false; // All-in players can't act
        if (player.Chips <= 0) return false; // Busted players can't act

        var callAmount = Math.Max(0, gameState.CurrentBet - gameState.GetPlayerBetThisRound(player));

        return action switch
        {
            ActionType.Fold => true, // Can always fold
            ActionType.Check => callAmount == 0, // Can only check if no bet to call
            ActionType.Call => callAmount > 0 && callAmount <= player.Chips,
            ActionType.Bet => gameState.CurrentBet == 0 && amount > 0 && amount <= player.Chips,
            ActionType.Raise => gameState.CurrentBet > 0 && amount >= GetMinimumRaise(gameState) && amount <= player.Chips,
            ActionType.AllIn => player.Chips > 0,
            _ => false
        };
    }

    public static int GetMinimumRaise(GameState gameState)
    {
        // Minimum raise is typically double the current bet
        return Math.Max(gameState.CurrentBet * 2, gameState.BigBlindAmount);
    }

    public static int GetCallAmount(IPlayer player, GameState gameState)
    {
        var amount = Math.Max(0, gameState.CurrentBet - gameState.GetPlayerBetThisRound(player));
        return Math.Min(amount, player.Chips);
    }

    public static void ProcessAction(IPlayer player, PlayerAction action, GameState gameState, Pot pot)
    {
        switch (action.Action)
        {
            case ActionType.Fold:
                player.HasFolded = true;
                Console.WriteLine($"❌ {player.Name} folds");
                break;

            case ActionType.Check:
                Console.WriteLine($"✋ {player.Name} checks");
                break;

            case ActionType.Call:
                var callAmount = GetCallAmount(player, gameState);
                if (player.RemoveChips(callAmount))
                {
                    pot.AddToMainPot(callAmount);
                    // Check if player used all their chips (after removal, chips would be 0)
                    if (player.Chips == 0)
                    {
                        player.IsAllIn = true;
                        Console.WriteLine($"🎯 {player.Name} calls €{callAmount} and is ALL-IN!");
                    }
                    else
                    {
                        Console.WriteLine($"📞 {player.Name} calls €{callAmount}");
                    }
                }
                break;

            case ActionType.Bet:
                if (player.RemoveChips(action.Amount))
                {
                    pot.AddToMainPot(action.Amount);
                    gameState.CurrentBet = action.Amount;

                    // Check if player used all their chips (after removal, chips would be 0)
                    if (player.Chips == 0)
                    {
                        player.IsAllIn = true;
                        Console.WriteLine($"🎯 {player.Name} bets €{action.Amount} and is ALL-IN!");
                    }
                    else
                    {
                        Console.WriteLine($"💰 {player.Name} bets €{action.Amount}");
                    }
                }
                break;

            case ActionType.Raise:
                if (player.RemoveChips(action.Amount))
                {
                    pot.AddToMainPot(action.Amount);
                    gameState.CurrentBet = action.Amount;

                    // Check if player used all their chips (after removal, chips would be 0)
                    if (player.Chips == 0)
                    {
                        player.IsAllIn = true;
                        Console.WriteLine($"🎯 {player.Name} raises to €{action.Amount} and is ALL-IN!");
                    }
                    else
                    {
                        Console.WriteLine($"🚀 {player.Name} raises to €{action.Amount}");
                    }
                }
                break;

            case ActionType.AllIn:
                var allInAmount = player.Chips;
                if (player.RemoveChips(allInAmount))
                {
                    pot.AddToMainPot(allInAmount);
                    player.IsAllIn = true;
                    
                    if (allInAmount > gameState.CurrentBet)
                    {
                        gameState.CurrentBet = allInAmount;
                        Console.WriteLine($"💥 {player.Name} goes ALL-IN for €{allInAmount}! (Raise)");
                    }
                    else
                    {
                        Console.WriteLine($"💥 {player.Name} goes ALL-IN for €{allInAmount}! (Call)");
                    }
                }
                break;
        }

        gameState.AddPlayerAction(player, action.Action, action.Amount);
    }

    public static bool IsBettingRoundComplete(List<IPlayer> activePlayers, GameState gameState)
    {
        // Get players who can still act (not folded, not all-in, have chips)
        var playersWhoCanAct = activePlayers.Where(p => !p.HasFolded && !p.IsAllIn && p.Chips > 0).ToList();

        // Special case: Pre-flop BB option
        // The big blind must always get a chance to act (check or raise) even if they're alone
        if (gameState.BettingPhase == BettingPhase.PreFlop && playersWhoCanAct.Count >= 1)
        {
            var bigBlindPlayer = gameState.BigBlindPosition < gameState.Players.Count
                ? gameState.Players[gameState.BigBlindPosition]
                : null;

            // If BB can act and hasn't acted yet, round isn't complete
            if (bigBlindPlayer != null &&
                playersWhoCanAct.Contains(bigBlindPlayer) &&
                !gameState.HasPlayerActed(bigBlindPlayer))
            {
                return false;
            }
        }

        // If only one or no players can act, betting is complete
        if (playersWhoCanAct.Count <= 1)
        {
            return true;
        }

        // If no current bet, everyone who can act must have acted (checked)
        if (gameState.CurrentBet == 0)
        {
            return playersWhoCanAct.All(p => gameState.HasPlayerActed(p));
        }

        // If there's a current bet, all players who can act must have:
        // 1. Acted this round AND
        // 2. Either matched the current bet or folded/gone all-in
        foreach (var player in playersWhoCanAct)
        {
            if (!gameState.HasPlayerActed(player))
            {
                return false; // Player hasn't acted yet
            }

            var playerBet = gameState.GetPlayerBetThisRound(player);
            if (playerBet < gameState.CurrentBet)
            {
                return false; // Player hasn't matched the current bet
            }
        }

        return true;
    }

    public static List<IPlayer> GetActivePlayers(List<IPlayer> players)
    {
        return players.Where(p => p.IsActive && !p.HasFolded).ToList();
    }

    public static List<IPlayer> GetPlayersInHand(List<IPlayer> players)
    {
        return players.Where(p => !p.HasFolded).ToList();
    }

    public static int GetNextPlayerToAct(List<IPlayer> players, GameState gameState, int dealerPosition)
    {
        var activePlayers = players.Where(p => !p.HasFolded && !p.IsAllIn && p.Chips > 0).ToList();
        
        if (activePlayers.Count <= 1)
        {
            return -1; // No one to act
        }

        // Start from the player after the current player (or dealer+1 if starting fresh)
        int startPos = gameState.CurrentPlayerPosition >= 0 
            ? (gameState.CurrentPlayerPosition + 1) % players.Count
            : (dealerPosition + 1) % players.Count;

        // Find the next player who can act
        for (int i = 0; i < players.Count; i++)
        {
            int pos = (startPos + i) % players.Count;
            var player = players[pos];
            
            if (activePlayers.Contains(player))
            {
                return pos;
            }
        }

        return -1; // No valid player found
    }

    public static bool ShouldShowdown(List<IPlayer> players)
    {
        var playersInHand = GetPlayersInHand(players);
        return playersInHand.Count > 1;
    }

    public static void ResetPlayersForNewHand(List<IPlayer> players)
    {
        foreach (var player in players)
        {
            player.Reset();
        }
    }

    public static List<IPlayer> EliminateBustedPlayers(List<IPlayer> players)
    {
        var remainingPlayers = new List<IPlayer>();
        
        foreach (var player in players)
        {
            if (player.Chips <= 0)
            {
                player.IsActive = false;
                Console.WriteLine($"💸 {player.Name} is eliminated (busted)!");
            }
            else
            {
                remainingPlayers.Add(player);
            }
        }

        return remainingPlayers;
    }

    public static void ShowBettingAction(PlayerAction action)
    {
        var timestamp = action.Timestamp.ToString("HH:mm:ss");
        var amountStr = action.Amount > 0 ? $" €{action.Amount}" : "";
        Console.WriteLine($"[{timestamp}] {action.PlayerId}: {action.Action}{amountStr}");
    }

    public static BettingRoundSummary GetBettingRoundSummary(GameState gameState)
    {
        return new BettingRoundSummary
        {
            Phase = gameState.BettingPhase,
            TotalActions = gameState.ActionsThisRound.Count,
            TotalBet = gameState.ActionsThisRound.Sum(a => a.Amount),
            PlayerActions = gameState.ActionsThisRound.ToList(),
            FinalBet = gameState.CurrentBet
        };
    }
}

public class BettingRoundSummary
{
    public BettingPhase Phase { get; set; }
    public int TotalActions { get; set; }
    public int TotalBet { get; set; }
    public List<PlayerAction> PlayerActions { get; set; } = new();
    public int FinalBet { get; set; }
}