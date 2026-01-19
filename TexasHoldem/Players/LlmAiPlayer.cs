using System.Text.Json;
using TexasHoldem.Domain;
using TexasHoldem.Domain.Enums;

namespace TexasHoldem.Players;

/// <summary>
/// Abstract base class for LLM-powered AI poker players.
/// Provides common functionality for Claude, Gemini, and OpenAI implementations.
/// Implements IDisposable to properly dispose of HttpClient resources.
/// </summary>
public abstract class LlmAiPlayer : BasicAiPlayer, IDisposable
{
    protected readonly string _apiKey;
    protected readonly string _modelName;
    protected readonly bool _isLlmEnabled;
    protected readonly HttpClient _httpClient;
    private bool _disposed;

    public AiProvider Provider { get; }

    protected LlmAiPlayer(
        string name,
        int startingChips,
        PersonalityType personality,
        AiProvider provider,
        string? apiKey,
        string modelName,
        Random? random = null)
        : base(name, startingChips, personality, random)
    {
        Provider = provider;
        _apiKey = apiKey ?? string.Empty;
        _modelName = modelName;
        _isLlmEnabled = !string.IsNullOrEmpty(_apiKey);
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    /// <summary>
    /// Disposes of the HttpClient and other managed resources.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _httpClient?.Dispose();
            }
            _disposed = true;
        }
    }

    /// <summary>
    /// Disposes of the HttpClient to prevent socket exhaustion.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public override PlayerAction TakeTurn(GameState gameState)
    {
        if (_isLlmEnabled)
        {
            try
            {
                // Use async in sync context - not ideal but works for CLI
                return MakeLlmDecisionAsync(gameState).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [{Provider}] AI error for {Name}: {ex.Message}");
                Console.WriteLine($"  Falling back to basic AI...");
            }
        }

        return base.TakeTurn(gameState);
    }

    protected async Task<PlayerAction> MakeLlmDecisionAsync(GameState gameState)
    {
        var prompt = BuildPrompt(gameState);
        var response = await CallLlmApiAsync(prompt);
        return ParseLlmResponse(response, gameState);
    }

    /// <summary>
    /// Call the LLM API - must be implemented by each provider
    /// </summary>
    protected abstract Task<string> CallLlmApiAsync(string prompt);

    protected string BuildPrompt(GameState gameState)
    {
        var gameStateJson = SerializeGameStateForAi(gameState);
        var personalityInstructions = GetPersonalityInstructions(Personality);
        var validActions = GetValidActionsDescription(gameState);

        return $@"You are an AI poker player with a {Personality} personality playing Texas Hold'em.

GAME STATE:
{gameStateJson}

YOUR PERSONALITY ({Personality}):
{personalityInstructions}

VALID ACTIONS:
{validActions}

IMPORTANT: You must respond with ONLY a JSON object, no other text. Format:
{{
  ""action"": ""fold"" | ""check"" | ""call"" | ""bet"" | ""raise"" | ""allin"",
  ""amount"": <number for bet/raise, 0 for others>,
  ""reasoning"": ""<brief explanation>""
}}

Consider pot odds, hand strength, position, opponent tendencies, and your personality traits.
Make your decision:";
    }

    protected string SerializeGameStateForAi(GameState gameState)
    {
        var amountToCall = Math.Max(0, gameState.CurrentBet - gameState.GetPlayerBetThisRound(this));

        var aiGameState = new
        {
            HandNumber = gameState.HandNumber,
            Phase = gameState.Phase.ToString(),
            MyCards = HoleCards.Select(c => c.ToString()).ToList(),
            CommunityCards = gameState.CommunityCards.Select(c => c.ToString()).ToList(),
            MyChips = Chips,
            TotalPot = gameState.TotalPot,
            CurrentBet = gameState.CurrentBet,
            MyBetThisRound = gameState.GetPlayerBetThisRound(this),
            AmountToCall = amountToCall,
            Position = GetPositionDescription(gameState),
            ActivePlayers = gameState.Players
                .Where(p => p.IsActive && !p.HasFolded)
                .Select(p => new
                {
                    Name = p.Name,
                    Chips = p.Chips,
                    IsAllIn = p.IsAllIn,
                    BetThisRound = gameState.GetPlayerBetThisRound(p)
                }).ToList(),
            RecentActions = gameState.ActionsThisRound.TakeLast(5).Select(a => new
            {
                Player = a.PlayerId,
                Action = a.Action.ToString(),
                Amount = a.Amount
            }).ToList()
        };

        return JsonSerializer.Serialize(aiGameState, new JsonSerializerOptions { WriteIndented = true });
    }

    protected string GetPositionDescription(GameState gameState)
    {
        var myIndex = gameState.Players.IndexOf(this);
        if (myIndex == gameState.DealerPosition) return "Dealer (Button)";
        if (myIndex == gameState.SmallBlindPosition) return "Small Blind";
        if (myIndex == gameState.BigBlindPosition) return "Big Blind";
        return "Middle Position";
    }

    protected string GetValidActionsDescription(GameState gameState)
    {
        var actions = new List<string>();
        var amountToCall = Math.Max(0, gameState.CurrentBet - gameState.GetPlayerBetThisRound(this));
        var alreadyBet = gameState.GetPlayerBetThisRound(this);

        actions.Add("- fold: Give up your hand");

        if (amountToCall == 0)
        {
            actions.Add("- check: Pass without betting (no cost)");
        }
        else
        {
            actions.Add($"- call: Match the current bet (costs €{amountToCall})");
        }

        if (gameState.CurrentBet == 0)
        {
            actions.Add($"- bet: Make an initial bet (minimum €{gameState.BigBlindAmount}, maximum €{Chips})");
        }
        else if (Chips > amountToCall)
        {
            var minRaise = gameState.CurrentBet * 2;
            var maxRaise = Chips + alreadyBet;
            actions.Add($"- raise: Increase the bet (minimum total €{minRaise}, maximum €{maxRaise})");
        }

        actions.Add($"- allin: Bet all your chips (€{Chips})");

        return string.Join("\n", actions);
    }

    protected PlayerAction ParseLlmResponse(string response, GameState gameState)
    {
        try
        {
            // Try to extract JSON from response (in case there's extra text)
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}');
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                response = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var decision = JsonSerializer.Deserialize<LlmDecision>(response, options);

            if (decision != null)
            {
                var actionType = ParseActionType(decision.Action);
                var amount = CalculateValidAmount(actionType, decision.Amount, gameState);

                Console.WriteLine($"  [{Provider}] {Name} decides: {actionType} {(amount > 0 ? $"€{amount}" : "")}");
                if (!string.IsNullOrEmpty(decision.Reasoning))
                {
                    Console.WriteLine($"  Reasoning: {decision.Reasoning}");
                }

                return new PlayerAction
                {
                    PlayerId = Name,
                    Action = actionType,
                    Amount = amount,
                    Timestamp = DateTime.Now,
                    BettingPhase = gameState.BettingPhase
                };
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [{Provider}] Failed to parse response: {ex.Message}");
        }

        // Fall back to basic AI decision
        return base.TakeTurn(gameState);
    }

    private ActionType ParseActionType(string action)
    {
        return action?.ToLower() switch
        {
            "fold" => ActionType.Fold,
            "check" => ActionType.Check,
            "call" => ActionType.Call,
            "bet" => ActionType.Bet,
            "raise" => ActionType.Raise,
            "allin" or "all-in" or "all_in" => ActionType.AllIn,
            _ => ActionType.Check
        };
    }

    private int CalculateValidAmount(ActionType action, int requestedAmount, GameState gameState)
    {
        var amountToCall = Math.Max(0, gameState.CurrentBet - gameState.GetPlayerBetThisRound(this));
        var alreadyBet = gameState.GetPlayerBetThisRound(this);

        return action switch
        {
            ActionType.Fold or ActionType.Check => 0,
            ActionType.Call => Math.Min(amountToCall, Chips),
            ActionType.AllIn => Chips,
            ActionType.Bet => Math.Clamp(requestedAmount, gameState.BigBlindAmount, Chips),
            ActionType.Raise => Math.Clamp(requestedAmount, gameState.CurrentBet * 2, Chips + alreadyBet),
            _ => 0
        };
    }

    protected string GetPersonalityInstructions(PersonalityType? personality)
    {
        return personality switch
        {
            PersonalityType.Aggressive => "Be aggressive with betting and raising. Apply pressure with pot-sized bets. Bluff occasionally but avoid going all-in unless you have a very strong hand (90%+ equity) or are short-stacked (less than 5x big blind).",
            PersonalityType.Tight => "Only play premium hands (high pairs, high cards). Fold marginal hands. Be very selective. Only go all-in with the nuts or when short-stacked.",
            PersonalityType.Loose => "Play many hands and see more flops. Be willing to gamble with weaker holdings, but bet sensibly - use pot-sized bets, not all-ins. Save all-ins for very strong hands or desperate situations.",
            PersonalityType.Bluffer => "Bluff frequently with pot-sized bets. Represent hands you don't have. Mix up your play. Avoid bluff all-ins - use smaller bets to bluff effectively.",
            PersonalityType.Passive => "Avoid betting and raising unless you have a very strong hand. Prefer calling and checking. Rarely go all-in.",
            PersonalityType.Shark => "Play optimally. Calculate pot odds and expected value. Use strategic bet sizing (50-100% of pot). Only go all-in when the math strongly favors it - strong hand or good fold equity when short-stacked.",
            PersonalityType.Fish => "Make some suboptimal decisions. Call too often with weak draws. Occasionally bet at wrong times. Rarely go all-in.",
            PersonalityType.CallingStation => "Call almost everything. Rarely fold even with weak hands. Rarely raise or go all-in.",
            PersonalityType.Maniac => "Be very aggressive with betting and raising. Make big bets (80-100% pot). Apply maximum pressure, but save all-ins for strong hands (70%+ equity) or when short-stacked. Being aggressive doesn't mean going all-in every hand.",
            PersonalityType.Nit => "Only play the very best hands (AA, KK, QQ, AK). Fold everything else pre-flop. Only go all-in with the absolute nuts.",
            PersonalityType.Random => "Make varied decisions. Sometimes aggressive, sometimes passive. Avoid unnecessary all-ins.",
            _ => "Play a balanced, standard poker strategy. Use appropriate bet sizing (50-75% pot). Reserve all-ins for very strong hands or short-stack situations."
        };
    }

    public override string ToString()
    {
        var personalityStr = Personality?.ToString() ?? "Unknown";
        var statusStr = IsActive ? "Active" : "Inactive";
        var chipsStr = Chips > 0 ? $"€{Chips}" : "Busted";
        var aiStr = _isLlmEnabled ? $"{Provider} AI" : "Basic AI";

        return $"{Name} ({personalityStr}, {aiStr}) - {chipsStr} [{statusStr}]";
    }
}

/// <summary>
/// Record for LLM response parsing - immutable data transfer object
/// </summary>
public record LlmDecision
{
    public string Action { get; init; } = "check";
    public int Amount { get; init; } = 0;
    public string? Reasoning { get; init; }
}
