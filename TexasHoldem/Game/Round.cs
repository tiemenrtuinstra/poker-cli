using TexasHoldem.Domain;
using TexasHoldem.Domain.Enums;
using TexasHoldem.Players;
using TexasHoldem.CLI;

namespace TexasHoldem.Game;

public class Round
{
    private readonly GameState _gameState;
    private readonly Dealer _dealer;
    private readonly Pot _pot;
    private readonly List<BettingRoundSummary> _bettingSummaries;
    private readonly SpectreGameUI _gameUI;

    public int RoundNumber => _gameState.HandNumber;
    public GamePhase CurrentPhase => _gameState.Phase;
    public List<Card> CommunityCards => _gameState.CommunityCards.ToList();
    public bool IsComplete { get; private set; }

    public Round(GameState gameState, Dealer dealer, SpectreGameUI? gameUI = null)
    {
        _gameState = gameState;
        _dealer = dealer;
        _pot = new Pot();
        _bettingSummaries = new List<BettingRoundSummary>();
        _gameUI = gameUI ?? new SpectreGameUI();
        IsComplete = false;
    }

    public async Task PlayRoundAsync()
    {
        try
        {
            _gameUI.ClearScreen();
            _gameUI.DisplayHandHeader(_gameState.HandNumber, _gameState.Players, _gameState.Dealer);
            await Task.Delay(1000);
            
            // Pre-flop
            await PlayPreFlopAsync();
            if (IsRoundOver()) { await CompleteRound(); return; }

            // Flop
            await PlayFlopAsync();
            if (IsRoundOver()) { await CompleteRound(); return; }

            // Turn
            await PlayTurnAsync();
            if (IsRoundOver()) { await CompleteRound(); return; }

            // River
            await PlayRiverAsync();
            if (IsRoundOver()) { await CompleteRound(); return; }

            // Showdown
            await PlayShowdownAsync();
            await CompleteRound();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error during round: {ex.Message}");
            IsComplete = true;
        }
    }

    private async Task PlayPreFlopAsync()
    {
        _gameState.Phase = GamePhase.PreFlop;
        _gameState.BettingPhase = BettingPhase.PreFlop;

        _gameUI.DisplayPhaseHeader(BettingPhase.PreFlop);

        // Deal hole cards with animation
        _gameUI.ShowDealingAnimation("Dealing hole cards", 1500);
        _dealer.DealHoleCards(_gameState.Players.Where(p => p.IsActive).ToList());

        // Post blinds and antes
        PostBlindsAndAntes();

        // Show blinds posted with visual display
        var sbPlayer = _gameState.Players[_gameState.SmallBlindPosition];
        var bbPlayer = _gameState.Players[_gameState.BigBlindPosition];
        _gameUI.DisplayBlindsPosted(sbPlayer, _gameState.SmallBlindAmount, bbPlayer, _gameState.BigBlindAmount);

        _gameUI.DisplayPotBox(_pot.TotalPotAmount);

        // Note: Cards are shown privately to each human player when it's their turn
        // This prevents other players from seeing each other's cards

        // Conduct betting round
        await ConductBettingRoundAsync();
    }

    private async Task PlayFlopAsync()
    {
        _gameState.Phase = GamePhase.Flop;
        _gameState.BettingPhase = BettingPhase.Flop;

        _gameUI.DisplayPhaseHeader(BettingPhase.Flop);

        _gameUI.ShowDealingAnimation("Dealing the flop", 1000);
        var flop = _dealer.DealFlop();
        _gameState.CommunityCards.AddRange(flop);

        // Show community cards with ASCII art
        _gameUI.DisplayCommunityCardsAscii(_gameState.CommunityCards, BettingPhase.Flop);

        await ConductBettingRoundAsync();
    }

    private async Task PlayTurnAsync()
    {
        _gameState.Phase = GamePhase.Turn;
        _gameState.BettingPhase = BettingPhase.Turn;

        _gameUI.DisplayPhaseHeader(BettingPhase.Turn);

        _gameUI.ShowDealingAnimation("Dealing the turn", 800);
        var turn = _dealer.DealTurn();
        _gameState.CommunityCards.Add(turn);

        // Show community cards with ASCII art
        _gameUI.DisplayCommunityCardsAscii(_gameState.CommunityCards, BettingPhase.Turn);

        await ConductBettingRoundAsync();
    }

    private async Task PlayRiverAsync()
    {
        _gameState.Phase = GamePhase.River;
        _gameState.BettingPhase = BettingPhase.River;

        _gameUI.DisplayPhaseHeader(BettingPhase.River);

        _gameUI.ShowDealingAnimation("Dealing the river", 800);
        var river = _dealer.DealRiver();
        _gameState.CommunityCards.Add(river);

        // Show community cards with ASCII art
        _gameUI.DisplayCommunityCardsAscii(_gameState.CommunityCards, BettingPhase.River);

        await ConductBettingRoundAsync();
    }

    private async Task PlayShowdownAsync()
    {
        _gameState.Phase = GamePhase.Showdown;

        Console.WriteLine();
        _gameUI.DrawSeparator('═', 60);
        _gameUI.ShowColoredMessage("  🏆 SHOWDOWN", ConsoleColor.Yellow);
        _gameUI.DrawSeparator('═', 60);

        var playersInShowdown = BettingLogic.GetPlayersInHand(_gameState.Players);

        if (playersInShowdown.Count <= 1)
        {
            // Only one player left - they win by default
            if (playersInShowdown.Any())
            {
                var winner = playersInShowdown.First();
                var winAmount = _pot.TotalPotAmount;
                winner.AddChips(winAmount);
                _gameUI.ShowColoredMessage($"\n  🎉 {winner.Name} wins €{winAmount} by default!", ConsoleColor.Green);
                _gameUI.ShowChipAnimation("POT", winner.Name, winAmount);
            }
            return;
        }

        // Show final board with ASCII art
        Console.WriteLine("\n  📋 Final Board:");
        _gameUI.DisplayCommunityCardsAscii(_gameState.CommunityCards, BettingPhase.Showdown);
        Console.WriteLine();

        // Show all remaining players' cards with Spectre panels
        _gameUI.DisplayShowdownHands(playersInShowdown, _gameState.CommunityCards);
        await Task.Delay(2000);

        // Distribute winnings
        var winners = _pot.DistributePots(playersInShowdown, player =>
            HandEvaluator.EvaluateHand(player.HoleCards.Concat(_gameState.CommunityCards)));

        // Display winners
        _gameUI.DisplayShowdownWinners(winners);

        await Task.Delay(3000); // Let players see results
    }

    private async Task ConductBettingRoundAsync()
    {
        // Don't clear PlayerBets for pre-flop since blinds are already posted
        if (_gameState.BettingPhase != BettingPhase.PreFlop)
        {
            _gameState.ResetForNewBettingRound();
        }
        else
        {
            // For pre-flop, only reset the action tracking, keep the blind bets
            _gameState.CurrentBet = _gameState.BigBlindAmount;
            _gameState.ActionsThisRound.Clear();
            _gameState.PlayerHasActed.Clear();
            _gameState.IsRoundComplete = false;
        }

        var activePlayers = BettingLogic.GetActivePlayers(_gameState.Players);
        if (activePlayers.Count <= 1)
        {
            _gameUI.ShowColoredMessage("Only one player remaining, skipping betting round", ConsoleColor.DarkGray);
            return;
        }

        // Determine first player to act based on betting phase
        int startPosition;
        if (_gameState.BettingPhase == BettingPhase.PreFlop)
        {
            // Pre-flop: action starts from UTG (player after big blind)
            startPosition = _gameState.BigBlindPosition;
        }
        else
        {
            // Post-flop: action starts from first active player after dealer
            startPosition = _gameState.DealerPosition;
        }

        int currentPlayerPos = BettingLogic.GetNextPlayerToAct(_gameState.Players, _gameState, startPosition);
        
        while (!BettingLogic.IsBettingRoundComplete(activePlayers, _gameState) && currentPlayerPos != -1)
        {
            var currentPlayer = _gameState.Players[currentPlayerPos];
            
            // Skip players who can't act
            if (currentPlayer.HasFolded || currentPlayer.IsAllIn || currentPlayer.Chips <= 0)
            {
                currentPlayerPos = BettingLogic.GetNextPlayerToAct(_gameState.Players, _gameState, currentPlayerPos);
                continue;
            }

            _gameUI.DisplayPlayerTurn(currentPlayer, currentPlayer.Chips);

            try
            {
                _gameState.CurrentPlayerPosition = currentPlayerPos;
                var action = currentPlayer.TakeTurn(_gameState);

                // Validate action
                if (!BettingLogic.IsValidAction(currentPlayer, action.Action, action.Amount, _gameState))
                {
                    _gameUI.DisplayPlayerAction(currentPlayer, action.Action, action.Amount, "Invalid action!");
                    continue;
                }

                // Process the action
                BettingLogic.ProcessAction(currentPlayer, action, _gameState, _pot);

                // Display the action visually
                _gameUI.DisplayPlayerAction(currentPlayer, action.Action, action.Amount);

                // Update active players list
                activePlayers = BettingLogic.GetActivePlayers(_gameState.Players);

                // Check if only one player remains
                if (activePlayers.Count <= 1)
                {
                    _gameUI.ShowColoredMessage("Only one player remaining, ending betting round", ConsoleColor.DarkGray);
                    break;
                }
            }
            catch (Exception ex)
            {
                _gameUI.ShowColoredMessage($"Error with {currentPlayer.Name}'s action: {ex.Message}", ConsoleColor.Red);
            }

            // Move to next player
            currentPlayerPos = BettingLogic.GetNextPlayerToAct(_gameState.Players, _gameState, currentPlayerPos);
            
            await Task.Delay(500); // Small delay for readability
        }

        // Save betting round summary
        _bettingSummaries.Add(BettingLogic.GetBettingRoundSummary(_gameState));

        _gameUI.DisplayBettingComplete(_pot.TotalPotAmount);
    }

    private void PostBlindsAndAntes()
    {
        // Get blind positions before posting
        var (smallBlindPos, bigBlindPos) = _dealer.GetBlindPositions(_gameState.Players);
        var smallBlindPlayer = _gameState.Players[smallBlindPos];
        var bigBlindPlayer = _gameState.Players[bigBlindPos];

        // Calculate actual blind amounts (may be less if player is short-stacked)
        var actualSmallBlind = Math.Min(_gameState.SmallBlindAmount, smallBlindPlayer.Chips);
        var actualBigBlind = Math.Min(_gameState.BigBlindAmount, bigBlindPlayer.Chips);

        // Post blinds (removes chips from players)
        _dealer.PostBlinds(_gameState.Players, _gameState.SmallBlindAmount, _gameState.BigBlindAmount);
        _pot.AddToMainPot(actualSmallBlind + actualBigBlind);

        // Track blind bets in GameState so call amounts are calculated correctly
        _gameState.PlayerBets[smallBlindPlayer.Name] = actualSmallBlind;
        _gameState.PlayerBets[bigBlindPlayer.Name] = actualBigBlind;

        // Store blind positions for action order
        _gameState.SmallBlindPosition = smallBlindPos;
        _gameState.BigBlindPosition = bigBlindPos;

        if (_gameState.AnteAmount > 0)
        {
            _dealer.PostAntes(_gameState.Players, _gameState.AnteAmount);
            var activePlayerCount = _gameState.Players.Count(p => p.IsActive);
            _pot.AddToMainPot(_gameState.AnteAmount * activePlayerCount);
        }

        _gameState.CurrentBet = _gameState.BigBlindAmount;
    }

    private bool IsRoundOver()
    {
        var playersInHand = BettingLogic.GetPlayersInHand(_gameState.Players);
        return playersInHand.Count <= 1;
    }

    private async Task CompleteRound()
    {
        _gameState.Phase = GamePhase.HandComplete;
        IsComplete = true;

        // If pot hasn't been distributed yet (winner by fold before showdown), distribute it now
        var playersInHand = BettingLogic.GetPlayersInHand(_gameState.Players);
        if (playersInHand.Count == 1 && _pot.TotalPotAmount > 0)
        {
            var winner = playersInHand.First();
            var winAmount = _pot.TotalPotAmount;
            winner.AddChips(winAmount);
            _gameUI.DisplayFoldWinner(winner, winAmount);
        }

        // Eliminate busted players
        var remainingPlayers = BettingLogic.EliminateBustedPlayers(_gameState.Players);

        // Display hand summary with Spectre tables and panels
        _gameUI.DisplayHandSummary(_pot.TotalPotAmount, _bettingSummaries.Count, _gameState.Players);

        await Task.Delay(3000); // Give time to review results
    }

    public RoundResult GetResult()
    {
        return new RoundResult
        {
            RoundNumber = RoundNumber,
            TotalPot = _pot.TotalPotAmount,
            BettingSummaries = _bettingSummaries.ToList(),
            CommunityCards = _gameState.CommunityCards.ToList(),
            IsComplete = IsComplete,
            PlayersRemaining = _gameState.Players.Count(p => p.IsActive)
        };
    }
}

public class RoundResult
{
    public int RoundNumber { get; set; }
    public int TotalPot { get; set; }
    public List<BettingRoundSummary> BettingSummaries { get; set; } = new();
    public List<Card> CommunityCards { get; set; } = new();
    public bool IsComplete { get; set; }
    public int PlayersRemaining { get; set; }
}