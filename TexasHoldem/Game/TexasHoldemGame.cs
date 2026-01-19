using Spectre.Console;
using TexasHoldem.Data.Services;
using TexasHoldem.Game;
using TexasHoldem.Game.Enums;
using TexasHoldem.Game.Events;
using TexasHoldem.Players;
using TexasHoldem.CLI;

namespace TexasHoldem.Game;

public class TexasHoldemGame
{
    private readonly GameConfig _config;
    private readonly Dealer _dealer;
    private readonly GameState _gameState;
    private readonly List<RoundResult> _roundHistory;
    private readonly Random _random;
    private readonly IGameUI _gameUI;
    private readonly IGameEventPublisher? _eventPublisher;
    private readonly IOpponentProfiler? _opponentProfiler;
    private bool _gameRunning;

    public bool IsGameRunning => _gameRunning;
    public int CurrentHandNumber => _gameState.HandNumber;
    public List<IPlayer> Players => _gameState.Players.ToList();

    public TexasHoldemGame(GameConfig config, IGameUI? gameUI = null, IGameEventPublisher? eventPublisher = null, IOpponentProfiler? opponentProfiler = null)
    {
        _config = config;
        _random = new Random();
        _dealer = new Dealer(_random);
        _gameState = new GameState();
        _roundHistory = [];
        _gameUI = gameUI ?? new SpectreGameUI();
        _eventPublisher = eventPublisher;
        _opponentProfiler = opponentProfiler;
        _gameRunning = false;

        InitializeGame();
    }

    private void InitializeGame()
    {
        Console.WriteLine("🎰 Initializing Texas Hold'em Game...");

        // Reset name generator for new game
        AiNameGenerator.ResetUsedNames();

        // Create players
        _gameState.Players.Clear();
        
        // Add human players
        for (int i = 0; i < _config.HumanPlayerCount; i++)
        {
            var playerName = _config.HumanPlayerNames?.ElementAtOrDefault(i) ?? $"Player {i + 1}";
            var humanPlayer = new HumanPlayer(playerName, _config.StartingChips, inputHelper: null, gameUI: _gameUI);
            _gameState.Players.Add(humanPlayer);
        }

        // Add AI players using the factory (supports multiple providers)
        if (_config.AiPlayerCount > 0)
        {
            var aiFactory = new AiPlayerFactory(_config, _random);
            var aiPlayers = aiFactory.CreateAiPlayers(_config.AiPlayerCount, _config.StartingChips);
            foreach (var aiPlayer in aiPlayers)
            {
                // Inject opponent profiler for historical learning
                if (aiPlayer is AiPlayer ai && _opponentProfiler != null)
                {
                    ai.SetOpponentProfiler(_opponentProfiler);
                }
                _gameState.Players.Add(aiPlayer);
            }
        }

        // Set up game parameters
        _gameState.SmallBlindAmount = _config.SmallBlind;
        _gameState.BigBlindAmount = _config.BigBlind;
        _gameState.AnteAmount = _config.Ante;
        _gameState.HandNumber = 0;

        // Randomly assign dealer position
        if (_gameState.Players.Any())
        {
            _gameState.DealerPosition = _random.Next(_gameState.Players.Count);
            _dealer.SetDealerPosition(_gameState.DealerPosition);
        }

        Console.WriteLine($"✅ Game initialized with {_gameState.Players.Count} players");
        Console.WriteLine($"💰 Starting chips: €{_config.StartingChips}");
        Console.WriteLine($"🎯 Blinds: €{_config.SmallBlind}/€{_config.BigBlind}");
        if (_config.Ante > 0)
        {
            Console.WriteLine($"💸 Ante: €{_config.Ante}");
        }

        ShowPlayerSummary();
    }

    public async Task StartGame()
    {
        _gameRunning = true;
        Console.WriteLine("\n🚀 Starting Texas Hold'em Game!");
        Console.WriteLine("Press Ctrl+C to quit at any time.\n");

        // Load historical profiles for AI learning
        await LoadHistoricalProfilesForAiPlayersAsync();

        try
        {
            while (_gameRunning && !IsGameOver())
            {
                await PlayHandAsync();
                
                if (!IsGameOver())
                {
                    await PrepareNextHand();
                }

                // Check for tournament blind increases
                if (_config.IsBlindIncreaseEnabled && ShouldIncreaseBlinds())
                {
                    IncreaseBlinds();
                }
            }

            await EndGame();
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("\n🛑 Game interrupted by user");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Game error: {ex.Message}");
        }
        finally
        {
            _gameRunning = false;
        }
    }

    private async Task PlayHandAsync()
    {
        _gameState.HandNumber++;
        _gameState.HandStartTime = DateTime.Now;

        Console.WriteLine("\n" + new string('=', 60));
        Console.WriteLine($"🎲 HAND #{_gameState.HandNumber}");
        Console.WriteLine(new string('=', 60));

        try
        {
            // Create and play the round
            var round = new Round(_gameState, _dealer, _gameUI, _eventPublisher);
            await round.PlayRoundAsync();

            // Save round result
            _roundHistory.Add(round.GetResult());

            _gameUI.DisplayHandCompleted(_gameState.HandNumber, (DateTime.Now - _gameState.HandStartTime).TotalSeconds);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error in hand #{_gameState.HandNumber}: {ex.Message}");
        }
    }

    private async Task PrepareNextHand()
    {
        // Reset players for new hand
        BettingLogic.ResetPlayersForNewHand(_gameState.Players);

        // Find busted players before eliminating
        var bustedPlayers = _gameState.Players.Where(p => p.Chips <= 0).ToList();

        // Eliminate busted players
        var remainingPlayers = BettingLogic.EliminateBustedPlayers(_gameState.Players);
        _gameState.Players.Clear();
        _gameState.Players.AddRange(remainingPlayers);

        // Handle rebuys and AI respawns if enabled
        if (_config.AllowRebuys && bustedPlayers.Any())
        {
            await HandleBustedPlayers(bustedPlayers);
        }

        // Move dealer button
        if (_gameState.Players.Count > 1)
        {
            _dealer.MoveDealerButton(_gameState.Players);
            _gameState.DealerPosition = _dealer.DealerPosition;
        }

        // Clear community cards
        _gameState.CommunityCards.Clear();

        // Reset game state for new hand
        _gameState.Phase = GamePhase.Setup;
        _gameState.CurrentBet = 0;
        _gameState.TotalPot = 0;
        _gameState.ActionsThisRound.Clear();
        _gameState.PlayerBets.Clear();
        _gameState.PlayerHasFolded.Clear();
        _gameState.PlayerHasActed.Clear();
        _gameState.TotalContributions.Clear(); // Clear contributions for side pot calculation

        // Show preparing for next hand with new dealer
        var newDealer = _gameState.Players[_gameState.DealerPosition].Name;
        _gameUI.DisplayPreparingNextHand(newDealer);

        // Show current standings
        ShowPlayerSummary();

        // Pause between hands
        Console.WriteLine("\nPress Enter to continue to next hand...");
        Console.ReadLine();
    }

    private async Task HandleBustedPlayers(List<IPlayer> bustedPlayers)
    {
        var rebuyAmount = _config.RebuyAmount > 0 ? _config.RebuyAmount : _config.StartingChips;

        // Handle busted human players - offer rebuy
        var bustedHumans = bustedPlayers.Where(p => p is HumanPlayer).ToList();
        foreach (var player in bustedHumans)
        {
            Console.WriteLine();
            _gameUI.DrawSeparator('─', 50);
            Console.WriteLine($"💀 {player.Name} is out of chips!");

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title($"[yellow]Would you like to rebuy for €{rebuyAmount}?[/]")
                    .HighlightStyle(new Style(Color.Black, Color.Yellow))
                    .AddChoices(new[]
                    {
                        $"💰  Yes, rebuy for €{rebuyAmount}",
                        "🚪  No, sit out"
                    }));

            if (choice.Contains("Yes"))
            {
                player.Chips = rebuyAmount;
                player.IsActive = true;
                player.HasFolded = false;
                player.IsAllIn = false;
                _gameState.Players.Add(player);
                Console.WriteLine($"✅ {player.Name} has rebuyed for €{rebuyAmount}!");
            }
            else
            {
                Console.WriteLine($"👋 {player.Name} has left the table.");
            }
        }

        // Handle busted AI players - offer to respawn or add new ones
        var bustedAIs = bustedPlayers.Where(p => p is not HumanPlayer).ToList();
        if (bustedAIs.Any())
        {
            Console.WriteLine();
            _gameUI.DrawSeparator('─', 50);
            Console.WriteLine($"🤖 {bustedAIs.Count} AI player(s) eliminated: {string.Join(", ", bustedAIs.Select(p => p.Name))}");

            var aiChoice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[yellow]What would you like to do with AI players?[/]")
                    .HighlightStyle(new Style(Color.Black, Color.Yellow))
                    .AddChoices(new[]
                    {
                        $"🔄  Respawn same AI players (€{rebuyAmount} each)",
                        "🆕  Add new AI players with different names",
                        "⏭️  Continue without them"
                    }));

            if (aiChoice.Contains("Respawn"))
            {
                // Respawn same AI players
                foreach (var ai in bustedAIs)
                {
                    ai.Chips = rebuyAmount;
                    ai.IsActive = true;
                    ai.HasFolded = false;
                    ai.IsAllIn = false;
                    _gameState.Players.Add(ai);
                }
                Console.WriteLine($"✅ {bustedAIs.Count} AI player(s) respawned with €{rebuyAmount} each!");
            }
            else if (aiChoice.Contains("new AI"))
            {
                // Create new AI players with different names
                var aiFactory = new AiPlayerFactory(_config, _random);
                var newAIs = aiFactory.CreateAiPlayers(bustedAIs.Count, rebuyAmount);
                foreach (var ai in newAIs)
                {
                    _gameState.Players.Add(ai);
                }
                Console.WriteLine($"✅ {newAIs.Count} new AI player(s) joined: {string.Join(", ", newAIs.Select(p => p.Name))}");
            }
            else
            {
                Console.WriteLine("👋 AI players have left the table.");
            }
        }

        // Option to add extra AI players if the table is getting thin
        if (_gameState.Players.Count < 4 && _gameState.Players.Count >= 2)
        {
            Console.WriteLine();
            var addMore = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title($"[aqua]Only {_gameState.Players.Count} players remaining. Add more AI players?[/]")
                    .HighlightStyle(new Style(Color.Black, Color.Aqua))
                    .AddChoices(new[]
                    {
                        "➕  Add 1 AI player",
                        "➕  Add 2 AI players",
                        "➕  Add 3 AI players",
                        "⏭️  Continue as is"
                    }));

            if (!addMore.Contains("Continue"))
            {
                int countToAdd = addMore.Contains("1") ? 1 : addMore.Contains("2") ? 2 : 3;
                var aiFactory = new AiPlayerFactory(_config, _random);
                var newAIs = aiFactory.CreateAiPlayers(countToAdd, rebuyAmount);
                foreach (var ai in newAIs)
                {
                    _gameState.Players.Add(ai);
                }
                Console.WriteLine($"✅ {countToAdd} new AI player(s) joined the game!");
            }
        }
    }

    private void ShowPlayerSummary()
    {
        _gameUI.DisplayPlayerSummary(_gameState.Players, _gameState.DealerPosition);
    }

    private async Task LoadHistoricalProfilesForAiPlayersAsync()
    {
        if (_opponentProfiler == null) return;

        var playerNames = _gameState.Players.Select(p => p.Name).ToList();

        foreach (var player in _gameState.Players.OfType<AiPlayer>())
        {
            try
            {
                await player.LoadHistoricalProfilesAsync(playerNames);
            }
            catch
            {
                // Silently ignore errors loading profiles
            }
        }
    }

    private bool IsGameOver()
    {
        var activePlayers = _gameState.Players.Where(p => p.IsActive && p.Chips > 0).ToList();
        
        if (activePlayers.Count <= 1)
        {
            return true; // Game over - winner or no players left
        }

        if (_config.MaxHands > 0 && _gameState.HandNumber >= _config.MaxHands)
        {
            Console.WriteLine($"\n⏰ Maximum hands ({_config.MaxHands}) reached!");
            return true;
        }

        return false;
    }

    private async Task EndGame()
    {
        _gameRunning = false;

        var activePlayers = _gameState.Players.Where(p => p.IsActive && p.Chips > 0).ToList();
        var winner = activePlayers.Count == 1 ? activePlayers.First() : activePlayers.OrderByDescending(p => p.Chips).FirstOrDefault();

        // Publish game ended event
        var reason = activePlayers.Count <= 1 ? "Single player remaining" :
                     (_config.MaxHands > 0 && _gameState.HandNumber >= _config.MaxHands) ? "Max hands reached" : "Game completed";
        _eventPublisher?.Publish(new GameEndedEvent(winner, _gameState.HandNumber, reason));

        // Display game over with visual rankings
        _gameUI.DisplayGameOver(activePlayers);

        // Show game statistics
        await ShowGameStatistics();

        _gameUI.DisplayThanksForPlaying();
    }

    private async Task ShowGameStatistics()
    {
        double? avgPot = null;
        int? maxPot = null;
        int? totalBettingRounds = null;

        if (_roundHistory.Any())
        {
            avgPot = _roundHistory.Average(r => r.TotalPot);
            maxPot = _roundHistory.Max(r => r.TotalPot);
            totalBettingRounds = _roundHistory.Sum(r => r.BettingSummaries.Count);
        }

        var duration = DateTime.Now - _gameState.HandStartTime;
        _gameUI.DisplayGameStatistics(_gameState.HandNumber, duration, avgPot, maxPot, totalBettingRounds);

        await Task.Delay(1000);
    }

    private bool ShouldIncreaseBlinds()
    {
        if (!_config.IsBlindIncreaseEnabled) return false;
        return _gameState.HandNumber % _config.BlindIncreaseInterval == 0;
    }

    private void IncreaseBlinds()
    {
        var oldSmallBlind = _gameState.SmallBlindAmount;
        var oldBigBlind = _gameState.BigBlindAmount;

        _gameState.SmallBlindAmount = (int)(_gameState.SmallBlindAmount * _config.BlindIncreaseMultiplier);
        _gameState.BigBlindAmount = (int)(_gameState.BigBlindAmount * _config.BlindIncreaseMultiplier);

        _gameUI.DisplayBlindsIncrease(oldSmallBlind, oldBigBlind, _gameState.SmallBlindAmount, _gameState.BigBlindAmount);
    }

    public void StopGame()
    {
        _gameRunning = false;
        Console.WriteLine("\n🛑 Game stopping...");
    }

    public GameStatistics GetGameStatistics()
    {
        return new GameStatistics
        {
            HandsPlayed = _gameState.HandNumber,
            PlayersRemaining = _gameState.Players.Count(p => p.IsActive),
            RoundHistory = _roundHistory.ToList(),
            CurrentBlinds = $"€{_gameState.SmallBlindAmount}/€{_gameState.BigBlindAmount}"
        };
    }
}

public class GameStatistics
{
    public int HandsPlayed { get; set; }
    public int PlayersRemaining { get; set; }
    public List<RoundResult> RoundHistory { get; set; } = new();
    public string CurrentBlinds { get; set; } = string.Empty;
}