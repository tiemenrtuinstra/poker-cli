using TexasHoldem.Domain;
using TexasHoldem.Domain.Enums;
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
    private readonly GameUI _gameUI;
    private bool _gameRunning;

    public bool IsGameRunning => _gameRunning;
    public int CurrentHandNumber => _gameState.HandNumber;
    public List<IPlayer> Players => _gameState.Players.ToList();

    public TexasHoldemGame(GameConfig config)
    {
        _config = config;
        _random = new Random();
        _dealer = new Dealer(_random);
        _gameState = new GameState();
        _roundHistory = new List<RoundResult>();
        _gameUI = new GameUI();
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
            var humanPlayer = new HumanPlayer(playerName, _config.StartingChips, gameUI: _gameUI);
            _gameState.Players.Add(humanPlayer);
        }

        // Add AI players using the factory (supports multiple providers)
        if (_config.AiPlayerCount > 0)
        {
            var aiFactory = new AiPlayerFactory(_config, _random);
            var aiPlayers = aiFactory.CreateAiPlayers(_config.AiPlayerCount, _config.StartingChips);
            foreach (var aiPlayer in aiPlayers)
            {
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
        Console.WriteLine($"💰 Starting chips: ${_config.StartingChips}");
        Console.WriteLine($"🎯 Blinds: ${_config.SmallBlind}/${_config.BigBlind}");
        if (_config.Ante > 0)
        {
            Console.WriteLine($"💸 Ante: ${_config.Ante}");
        }

        ShowPlayerSummary();
    }

    public async Task StartGame()
    {
        _gameRunning = true;
        Console.WriteLine("\n🚀 Starting Texas Hold'em Game!");
        Console.WriteLine("Press Ctrl+C to quit at any time.\n");

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
            var round = new Round(_gameState, _dealer, _gameUI);
            await round.PlayRoundAsync();

            // Save round result
            _roundHistory.Add(round.GetResult());

            Console.WriteLine($"\n⏱️  Hand #{_gameState.HandNumber} completed in {(DateTime.Now - _gameState.HandStartTime).TotalSeconds:F1} seconds");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error in hand #{_gameState.HandNumber}: {ex.Message}");
        }
    }

    private async Task PrepareNextHand()
    {
        Console.WriteLine("\n🔄 Preparing for next hand...");

        // Reset players for new hand
        BettingLogic.ResetPlayersForNewHand(_gameState.Players);

        // Eliminate busted players
        var remainingPlayers = BettingLogic.EliminateBustedPlayers(_gameState.Players);
        _gameState.Players.Clear();
        _gameState.Players.AddRange(remainingPlayers);

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

        // Show current standings
        ShowPlayerSummary();

        // Pause between hands
        Console.WriteLine("\nPress Enter to continue to next hand...");
        Console.ReadLine();
    }

    private void ShowPlayerSummary()
    {
        Console.WriteLine("\n👥 PLAYER SUMMARY:");
        Console.WriteLine(new string('-', 50));
        
        var activePlayers = _gameState.Players.Where(p => p.IsActive).OrderByDescending(p => p.Chips).ToList();
        
        for (int i = 0; i < activePlayers.Count; i++)
        {
            var player = activePlayers[i];
            var position = i == _gameState.DealerPosition ? " (D)" : "";
            var playerType = player is HumanPlayer ? "Human" : 
                           player.Personality?.ToString() ?? "AI";
            
            Console.WriteLine($"{i + 1,2}. {player.Name,-20} {playerType,-12} ${player.Chips,8}{position}");
        }

        if (_gameState.Players.Any(p => !p.IsActive))
        {
            Console.WriteLine("\n💸 ELIMINATED:");
            var eliminatedPlayers = _gameState.Players.Where(p => !p.IsActive).ToList();
            foreach (var player in eliminatedPlayers)
            {
                Console.WriteLine($"   {player.Name} - Busted");
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
        
        Console.WriteLine("\n" + new string('=', 60));
        Console.WriteLine("🏁 GAME OVER!");
        Console.WriteLine(new string('=', 60));

        var activePlayers = _gameState.Players.Where(p => p.IsActive && p.Chips > 0).ToList();
        
        if (activePlayers.Count == 1)
        {
            var winner = activePlayers.First();
            Console.WriteLine($"🏆 WINNER: {winner.Name} with ${winner.Chips}!");
            
            if (winner is HumanPlayer)
            {
                Console.WriteLine("🎉 Congratulations! You won the tournament!");
            }
            else
            {
                Console.WriteLine($"🤖 The AI player {winner.Name} ({winner.Personality}) has won!");
            }
        }
        else if (activePlayers.Count > 1)
        {
            Console.WriteLine("🏆 FINAL RANKINGS:");
            var rankings = activePlayers.OrderByDescending(p => p.Chips).ToList();
            
            for (int i = 0; i < rankings.Count; i++)
            {
                var player = rankings[i];
                var medal = i switch { 0 => "🥇", 1 => "🥈", 2 => "🥉", _ => $"{i + 1}." };
                Console.WriteLine($"{medal} {player.Name}: ${player.Chips}");
            }
        }
        else
        {
            Console.WriteLine("💔 No players remaining! What a strange game...");
        }

        // Show game statistics
        await ShowGameStatistics();

        Console.WriteLine("\nThanks for playing Texas Hold'em!");
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    private async Task ShowGameStatistics()
    {
        Console.WriteLine("\n📊 GAME STATISTICS:");
        Console.WriteLine(new string('-', 40));
        Console.WriteLine($"Total Hands Played: {_gameState.HandNumber}");
        Console.WriteLine($"Game Duration: {DateTime.Now - _gameState.HandStartTime:hh\\:mm\\:ss}");
        
        if (_roundHistory.Any())
        {
            var avgPot = _roundHistory.Average(r => r.TotalPot);
            var maxPot = _roundHistory.Max(r => r.TotalPot);
            var totalBettingRounds = _roundHistory.Sum(r => r.BettingSummaries.Count);
            
            Console.WriteLine($"Average Pot Size: ${avgPot:F0}");
            Console.WriteLine($"Largest Pot: ${maxPot}");
            Console.WriteLine($"Total Betting Rounds: {totalBettingRounds}");
        }

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
        
        Console.WriteLine("\n📈 BLINDS INCREASED!");
        Console.WriteLine($"Old blinds: ${oldSmallBlind}/${oldBigBlind}");
        Console.WriteLine($"New blinds: ${_gameState.SmallBlindAmount}/${_gameState.BigBlindAmount}");
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
            CurrentBlinds = $"${_gameState.SmallBlindAmount}/${_gameState.BigBlindAmount}"
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