using TexasHoldem.Domain;
using TexasHoldem.Domain.Enums;

namespace TexasHoldem.Players;

public static class AiPersonality
{
    public static PlayerAction MakeDecision(IPlayer player, GameState gameState, PersonalityType personality, Random random)
    {
        var validActions = GetValidActions(player, gameState);
        var handStrength = EvaluateHandStrength(player, gameState);
        
        return personality switch
        {
            PersonalityType.Tight => MakeTightDecision(player, gameState, validActions, handStrength, random),
            PersonalityType.Loose => MakeLooseDecision(player, gameState, validActions, handStrength, random),
            PersonalityType.Aggressive => MakeAggressiveDecision(player, gameState, validActions, handStrength, random),
            PersonalityType.Passive => MakePassiveDecision(player, gameState, validActions, handStrength, random),
            PersonalityType.Bluffer => MakeBlufferDecision(player, gameState, validActions, handStrength, random),
            PersonalityType.Random => MakeRandomDecision(player, gameState, validActions, random),
            PersonalityType.Fish => MakeFishDecision(player, gameState, validActions, handStrength, random),
            PersonalityType.Shark => MakeSharkDecision(player, gameState, validActions, handStrength, random),
            PersonalityType.CallingStation => MakeCallingStationDecision(player, gameState, validActions, handStrength, random),
            PersonalityType.Maniac => MakeManiacDecision(player, gameState, validActions, handStrength, random),
            PersonalityType.Nit => MakeNitDecision(player, gameState, validActions, handStrength, random),
            _ => MakeRandomDecision(player, gameState, validActions, random)
        };
    }

    private static PlayerAction MakeTightDecision(IPlayer player, GameState gameState, List<ActionType> validActions, double handStrength, Random random)
    {
        // Tight players only play premium hands
        if (handStrength < 0.7)
        {
            return CreateAction(player, ActionType.Fold, 0, gameState);
        }

        if (handStrength > 0.85 && validActions.Contains(ActionType.Bet))
        {
            return CreateAction(player, ActionType.Bet, GetBetAmount(gameState, player.Chips, 0.5), gameState);
        }

        if (handStrength > 0.85 && validActions.Contains(ActionType.Raise))
        {
            return CreateAction(player, ActionType.Raise, GetRaiseAmount(gameState, player.Chips, 0.5), gameState);
        }

        if (validActions.Contains(ActionType.Call))
        {
            return CreateAction(player, ActionType.Call, GetCallAmount(gameState, player), gameState);
        }

        if (validActions.Contains(ActionType.Check))
        {
            return CreateAction(player, ActionType.Check, 0, gameState);
        }

        return CreateAction(player, ActionType.Fold, 0, gameState);
    }

    private static PlayerAction MakeLooseDecision(IPlayer player, GameState gameState, List<ActionType> validActions, double handStrength, Random random)
    {
        // Loose players play many hands
        if (handStrength < 0.2 && random.NextDouble() < 0.7) // Still fold really bad hands sometimes
        {
            return CreateAction(player, ActionType.Fold, 0, gameState);
        }

        if (handStrength > 0.6 && validActions.Contains(ActionType.Bet))
        {
            return CreateAction(player, ActionType.Bet, GetBetAmount(gameState, player.Chips, 0.7), gameState);
        }

        if (handStrength > 0.5 && validActions.Contains(ActionType.Raise))
        {
            return CreateAction(player, ActionType.Raise, GetRaiseAmount(gameState, player.Chips, 0.6), gameState);
        }

        if (validActions.Contains(ActionType.Call))
        {
            return CreateAction(player, ActionType.Call, GetCallAmount(gameState, player), gameState);
        }

        if (validActions.Contains(ActionType.Check))
        {
            return CreateAction(player, ActionType.Check, 0, gameState);
        }

        return CreateAction(player, ActionType.Fold, 0, gameState);
    }

    private static PlayerAction MakeAggressiveDecision(IPlayer player, GameState gameState, List<ActionType> validActions, double handStrength, Random random)
    {
        // Aggressive players bet and raise frequently
        if (handStrength > 0.4 && validActions.Contains(ActionType.Bet))
        {
            return CreateAction(player, ActionType.Bet, GetBetAmount(gameState, player.Chips, 0.8), gameState);
        }

        if (handStrength > 0.3 && validActions.Contains(ActionType.Raise))
        {
            return CreateAction(player, ActionType.Raise, GetRaiseAmount(gameState, player.Chips, 0.8), gameState);
        }

        if (handStrength > 0.6 && validActions.Contains(ActionType.AllIn) && random.NextDouble() < 0.2)
        {
            return CreateAction(player, ActionType.AllIn, player.Chips, gameState);
        }

        if (handStrength < 0.2)
        {
            return CreateAction(player, ActionType.Fold, 0, gameState);
        }

        if (validActions.Contains(ActionType.Call))
        {
            return CreateAction(player, ActionType.Call, GetCallAmount(gameState, player), gameState);
        }

        if (validActions.Contains(ActionType.Check))
        {
            return CreateAction(player, ActionType.Check, 0, gameState);
        }

        return CreateAction(player, ActionType.Fold, 0, gameState);
    }

    private static PlayerAction MakePassiveDecision(IPlayer player, GameState gameState, List<ActionType> validActions, double handStrength, Random random)
    {
        // Passive players rarely bet or raise
        if (handStrength < 0.3)
        {
            return CreateAction(player, ActionType.Fold, 0, gameState);
        }

        if (handStrength > 0.9 && validActions.Contains(ActionType.Bet) && random.NextDouble() < 0.3)
        {
            return CreateAction(player, ActionType.Bet, GetBetAmount(gameState, player.Chips, 0.3), gameState);
        }

        if (validActions.Contains(ActionType.Call) && handStrength > 0.4)
        {
            return CreateAction(player, ActionType.Call, GetCallAmount(gameState, player), gameState);
        }

        if (validActions.Contains(ActionType.Check))
        {
            return CreateAction(player, ActionType.Check, 0, gameState);
        }

        return CreateAction(player, ActionType.Fold, 0, gameState);
    }

    private static PlayerAction MakeBlufferDecision(IPlayer player, GameState gameState, List<ActionType> validActions, double handStrength, Random random)
    {
        // Bluffers bet with weak hands and sometimes fold strong hands
        var isBluffing = random.NextDouble() < 0.4; // 40% chance to bluff

        if (isBluffing && handStrength < 0.3)
        {
            if (validActions.Contains(ActionType.Bet))
            {
                return CreateAction(player, ActionType.Bet, GetBetAmount(gameState, player.Chips, 0.9), gameState);
            }
            if (validActions.Contains(ActionType.Raise))
            {
                return CreateAction(player, ActionType.Raise, GetRaiseAmount(gameState, player.Chips, 0.9), gameState);
            }
        }

        if (!isBluffing && handStrength > 0.8 && random.NextDouble() < 0.2) // Sometimes fold good hands to appear weak
        {
            return CreateAction(player, ActionType.Fold, 0, gameState);
        }

        // Normal play otherwise
        if (handStrength > 0.6)
        {
            if (validActions.Contains(ActionType.Bet))
            {
                return CreateAction(player, ActionType.Bet, GetBetAmount(gameState, player.Chips, 0.6), gameState);
            }
            if (validActions.Contains(ActionType.Call))
            {
                return CreateAction(player, ActionType.Call, GetCallAmount(gameState, player), gameState);
            }
        }

        if (validActions.Contains(ActionType.Check))
        {
            return CreateAction(player, ActionType.Check, 0, gameState);
        }

        return CreateAction(player, ActionType.Fold, 0, gameState);
    }

    private static PlayerAction MakeRandomDecision(IPlayer player, GameState gameState, List<ActionType> validActions, Random random)
    {
        // Completely random decisions
        var action = validActions[random.Next(validActions.Count)];
        var amount = action switch
        {
            ActionType.Bet => GetBetAmount(gameState, player.Chips, random.NextDouble()),
            ActionType.Raise => GetRaiseAmount(gameState, player.Chips, random.NextDouble()),
            ActionType.Call => GetCallAmount(gameState, player),
            ActionType.AllIn => player.Chips,
            _ => 0
        };

        return CreateAction(player, action, amount, gameState);
    }

    private static PlayerAction MakeFishDecision(IPlayer player, GameState gameState, List<ActionType> validActions, double handStrength, Random random)
    {
        // Fish make poor decisions, call too much, bet at wrong times
        if (validActions.Contains(ActionType.Call) && random.NextDouble() < 0.7) // Call too often
        {
            return CreateAction(player, ActionType.Call, GetCallAmount(gameState, player), gameState);
        }

        if (handStrength < 0.2 && validActions.Contains(ActionType.Bet) && random.NextDouble() < 0.3) // Bet with weak hands
        {
            return CreateAction(player, ActionType.Bet, GetBetAmount(gameState, player.Chips, 0.9), gameState);
        }

        if (handStrength > 0.8 && validActions.Contains(ActionType.Check) && random.NextDouble() < 0.4) // Check strong hands
        {
            return CreateAction(player, ActionType.Check, 0, gameState);
        }

        if (validActions.Contains(ActionType.Fold))
        {
            return CreateAction(player, ActionType.Fold, 0, gameState);
        }

        return CreateAction(player, validActions.First(), 0, gameState);
    }

    private static PlayerAction MakeSharkDecision(IPlayer player, GameState gameState, List<ActionType> validActions, double handStrength, Random random)
    {
        // Sharks are skilled, adaptive players
        var potOdds = CalculatePotOdds(gameState, player);
        
        // Only play hands with positive expected value
        if (handStrength < 0.4 && potOdds < 0.3)
        {
            return CreateAction(player, ActionType.Fold, 0, gameState);
        }

        if (handStrength > 0.8 && validActions.Contains(ActionType.Raise))
        {
            return CreateAction(player, ActionType.Raise, GetRaiseAmount(gameState, player.Chips, 0.7), gameState);
        }

        if (handStrength > 0.7 && validActions.Contains(ActionType.Bet))
        {
            return CreateAction(player, ActionType.Bet, GetBetAmount(gameState, player.Chips, 0.7), gameState);
        }

        if (potOdds > 0.6 && validActions.Contains(ActionType.Call))
        {
            return CreateAction(player, ActionType.Call, GetCallAmount(gameState, player), gameState);
        }

        if (validActions.Contains(ActionType.Check))
        {
            return CreateAction(player, ActionType.Check, 0, gameState);
        }

        return CreateAction(player, ActionType.Fold, 0, gameState);
    }

    private static PlayerAction MakeCallingStationDecision(IPlayer player, GameState gameState, List<ActionType> validActions, double handStrength, Random random)
    {
        // Calling stations call everything, rarely fold or raise
        if (validActions.Contains(ActionType.Call))
        {
            return CreateAction(player, ActionType.Call, GetCallAmount(gameState, player), gameState);
        }

        if (handStrength > 0.9 && validActions.Contains(ActionType.Bet))
        {
            return CreateAction(player, ActionType.Bet, GetBetAmount(gameState, player.Chips, 0.3), gameState);
        }

        if (validActions.Contains(ActionType.Check))
        {
            return CreateAction(player, ActionType.Check, 0, gameState);
        }

        return CreateAction(player, ActionType.Fold, 0, gameState);
    }

    private static PlayerAction MakeManiacDecision(IPlayer player, GameState gameState, List<ActionType> validActions, double handStrength, Random random)
    {
        // Maniacs bet, raise, and go all-in frequently
        if (validActions.Contains(ActionType.AllIn) && random.NextDouble() < 0.3)
        {
            return CreateAction(player, ActionType.AllIn, player.Chips, gameState);
        }

        if (validActions.Contains(ActionType.Raise))
        {
            return CreateAction(player, ActionType.Raise, GetRaiseAmount(gameState, player.Chips, 0.9), gameState);
        }

        if (validActions.Contains(ActionType.Bet))
        {
            return CreateAction(player, ActionType.Bet, GetBetAmount(gameState, player.Chips, 0.9), gameState);
        }

        if (validActions.Contains(ActionType.Call))
        {
            return CreateAction(player, ActionType.Call, GetCallAmount(gameState, player), gameState);
        }

        return CreateAction(player, ActionType.Check, 0, gameState);
    }

    private static PlayerAction MakeNitDecision(IPlayer player, GameState gameState, List<ActionType> validActions, double handStrength, Random random)
    {
        // Nits only play premium hands and play them very conservatively
        if (handStrength < 0.8)
        {
            return CreateAction(player, ActionType.Fold, 0, gameState);
        }

        if (handStrength > 0.95 && validActions.Contains(ActionType.Bet))
        {
            return CreateAction(player, ActionType.Bet, GetBetAmount(gameState, player.Chips, 0.3), gameState);
        }

        if (validActions.Contains(ActionType.Call) && handStrength > 0.9)
        {
            return CreateAction(player, ActionType.Call, GetCallAmount(gameState, player), gameState);
        }

        if (validActions.Contains(ActionType.Check))
        {
            return CreateAction(player, ActionType.Check, 0, gameState);
        }

        return CreateAction(player, ActionType.Fold, 0, gameState);
    }

    // Helper methods
    private static List<ActionType> GetValidActions(IPlayer player, GameState gameState)
    {
        var actions = new List<ActionType>();
        
        if (!player.HasFolded)
        {
            actions.Add(ActionType.Fold);
        }

        if (player.IsAllIn || player.Chips <= 0)
        {
            return actions;
        }

        var callAmount = Math.Max(0, gameState.CurrentBet - gameState.GetPlayerBetThisRound(player));
        
        if (callAmount == 0)
        {
            actions.Add(ActionType.Check);
        }
        
        if (callAmount > 0 && callAmount <= player.Chips)
        {
            actions.Add(ActionType.Call);
        }

        if (gameState.CurrentBet == 0 && player.Chips > 0)
        {
            actions.Add(ActionType.Bet);
        }

        // Can raise if there's a current bet and we have enough chips for minimum raise
        // (accounting for what we've already bet this round)
        var minRaise = gameState.CurrentBet * 2;
        var alreadyBet = gameState.GetPlayerBetThisRound(player);
        var chipsNeededForMinRaise = minRaise - alreadyBet;
        if (gameState.CurrentBet > 0 && player.Chips >= chipsNeededForMinRaise)
        {
            actions.Add(ActionType.Raise);
        }

        if (player.Chips > 0)
        {
            actions.Add(ActionType.AllIn);
        }

        return actions;
    }

    private static double EvaluateHandStrength(IPlayer player, GameState gameState)
    {
        var allCards = new List<Card>(player.HoleCards);
        allCards.AddRange(gameState.CommunityCards);
        
        if (allCards.Count < 5)
        {
            return EvaluateHoleCards(player.HoleCards);
        }
        
        var handResult = HandEvaluator.EvaluateHand(allCards);
        return (double)handResult.Strength / 10.0;
    }

    private static double EvaluateHoleCards(List<Card> holeCards)
    {
        if (holeCards.Count != 2) return 0.0;
        
        var card1 = holeCards[0];
        var card2 = holeCards[1];
        
        if (card1.Rank == card2.Rank)
        {
            var pairStrength = (int)card1.Rank / 14.0;
            return 0.6 + (pairStrength * 0.4);
        }
        
        bool suited = card1.Suit == card2.Suit;
        var highCard = (int)((int)card1.Rank > (int)card2.Rank ? card1.Rank : card2.Rank);
        var lowCard = (int)((int)card1.Rank < (int)card2.Rank ? card1.Rank : card2.Rank);
        
        double strength = (highCard + lowCard) / 28.0;
        
        if (suited) strength += 0.1;
        
        if (Math.Abs((int)card1.Rank - (int)card2.Rank) == 1)
        {
            strength += 0.05;
        }
        
        return Math.Min(strength, 1.0);
    }

    private static double CalculatePotOdds(GameState gameState, IPlayer player)
    {
        var callAmount = Math.Max(0, gameState.CurrentBet - gameState.GetPlayerBetThisRound(player));
        if (callAmount == 0) return 1.0;
        
        return (double)gameState.TotalPot / (gameState.TotalPot + callAmount);
    }

    private static int GetCallAmount(GameState gameState, IPlayer player)
    {
        var amount = Math.Max(0, gameState.CurrentBet - gameState.GetPlayerBetThisRound(player));
        return Math.Min(amount, player.Chips);
    }

    private static int GetBetAmount(GameState gameState, int playerChips, double aggressionFactor)
    {
        var minBet = Math.Max(gameState.BigBlindAmount, 1);
        var maxBet = playerChips;
        
        var targetBet = (int)(minBet + ((maxBet - minBet) * aggressionFactor * 0.5));
        return Math.Max(minBet, Math.Min(targetBet, maxBet));
    }

    private static int GetRaiseAmount(GameState gameState, int playerChips, double aggressionFactor)
    {
        var minRaise = gameState.CurrentBet * 2;
        var maxRaise = playerChips;
        
        var targetRaise = (int)(minRaise + ((maxRaise - minRaise) * aggressionFactor * 0.5));
        return Math.Max(minRaise, Math.Min(targetRaise, maxRaise));
    }

    private static PlayerAction CreateAction(IPlayer player, ActionType action, int amount, GameState gameState)
    {
        return new PlayerAction
        {
            PlayerId = player.Name,
            Action = action,
            Amount = amount,
            Timestamp = DateTime.Now,
            BettingPhase = gameState.BettingPhase
        };
    }
}