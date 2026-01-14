using System.Text.Json;
using TexasHoldem.Domain;
using TexasHoldem.Domain.Enums;

namespace TexasHoldem.Players;

/// <summary>
/// Abstract base class for LLM-powered AI poker players.
/// Provides common functionality for Claude, Gemini, and OpenAI implementations.
/// </summary>
public abstract class LlmAiPlayer : BasicAiPlayer
{
    protected readonly string _apiKey;
    protected readonly string _modelName;
    protected readonly bool _isLlmEnabled;
    protected readonly HttpClient _httpClient;

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
        _httpClient = new HttpClient();
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
            actions.Add($"- call: Match the current bet (costs ${amountToCall})");
        }

        if (gameState.CurrentBet == 0)
        {
            actions.Add($"- bet: Make an initial bet (minimum ${gameState.BigBlindAmount}, maximum ${Chips})");
        }
        else if (Chips > amountToCall)
        {
            var minRaise = gameState.CurrentBet * 2;
            var maxRaise = Chips + alreadyBet;
            actions.Add($"- raise: Increase the bet (minimum total ${minRaise}, maximum ${maxRaise})");
        }

        actions.Add($"- allin: Bet all your chips (${Chips})");

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

                Console.WriteLine($"  [{Provider}] {Name} decides: {actionType} {(amount > 0 ? $"${amount}" : "")}");
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
            PersonalityType.Aggressive => "Be aggressive with betting and raising. Apply pressure on opponents frequently. Don't be afraid to bluff.",
            PersonalityType.Tight => "Only play premium hands (high pairs, high cards). Fold marginal hands. Be very selective about which hands to play.",
            PersonalityType.Loose => "Play many hands. Be willing to gamble with weaker holdings. See more flops.",
            PersonalityType.Bluffer => "Bluff frequently. Represent hands you don't have. Mix up your play to be unpredictable.",
            PersonalityType.Passive => "Avoid betting and raising unless you have a very strong hand. Prefer calling and checking.",
            PersonalityType.Shark => "Play optimally. Calculate pot odds and expected value. Exploit weak players.",
            PersonalityType.Fish => "Make some suboptimal decisions. Call too often with weak draws. Occasionally bet at wrong times.",
            PersonalityType.CallingStation => "Call almost everything. Rarely fold even with weak hands. Rarely raise.",
            PersonalityType.Maniac => "Bet, raise, and go all-in frequently. Play very aggressively with any hand.",
            PersonalityType.Nit => "Only play the very best hands (AA, KK, QQ, AK). Fold everything else pre-flop.",
            PersonalityType.Random => "Make random decisions. Sometimes play optimally, sometimes not.",
            _ => "Play a balanced, standard poker strategy. Mix up your play."
        };
    }

    public override string ToString()
    {
        var personalityStr = Personality?.ToString() ?? "Unknown";
        var statusStr = IsActive ? "Active" : "Inactive";
        var chipsStr = Chips > 0 ? $"${Chips}" : "Busted";
        var aiStr = _isLlmEnabled ? $"{Provider} AI" : "Basic AI";

        return $"{Name} ({personalityStr}, {aiStr}) - {chipsStr} [{statusStr}]";
    }
}

/// <summary>
/// DTO for LLM response parsing
/// </summary>
public class LlmDecision
{
    public string Action { get; set; } = "check";
    public int Amount { get; set; } = 0;
    public string? Reasoning { get; set; }
}
