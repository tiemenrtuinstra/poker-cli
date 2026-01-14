using TexasHoldem.Domain;
using TexasHoldem.Domain.Enums;

namespace TexasHoldem.CLI;

public class ReplayViewer
{
    private readonly Logger _logger;
    private readonly InputHelper _inputHelper;
    private readonly GameUI _gameUI;

    public ReplayViewer(Logger logger)
    {
        _logger = logger;
        _inputHelper = new InputHelper();
        _gameUI = new GameUI();
    }

    public async Task ShowReplayMenu()
    {
        while (true)
        {
            _inputHelper.ClearScreen();
            Console.WriteLine("🎬 HAND REPLAY VIEWER");
            Console.WriteLine("=====================");
            Console.WriteLine();

            var choice = _inputHelper.GetChoiceInput("What would you like to do?", new Dictionary<string, string>
            {
                {"View Recent Hands", "recent"},
                {"Load Hand History File", "load"},
                {"Show Game Statistics", "stats"},
                {"Back to Main Menu", "back"}
            }, "recent");

            switch (choice)
            {
                case "recent":
                    await ShowRecentHands();
                    break;
                case "load":
                    await LoadAndViewHandHistory();
                    break;
                case "stats":
                    ShowGameStatistics();
                    break;
                case "back":
                    return;
            }
        }
    }

    private async Task ShowRecentHands()
    {
        var historyFiles = _logger.GetAvailableHandHistoryFiles();
        
        if (!historyFiles.Any())
        {
            _inputHelper.ShowWarning("No hand history files found.");
            _inputHelper.PressAnyKeyToContinue();
            return;
        }

        // Get the most recent file
        var latestFile = historyFiles.OrderByDescending(f => f).First();
        var handRecords = _logger.LoadHandHistory(latestFile);
        
        if (!handRecords.Any())
        {
            _inputHelper.ShowWarning("No hands found in the history file.");
            _inputHelper.PressAnyKeyToContinue();
            return;
        }

        await ViewHandRecords(handRecords, $"Recent hands from {latestFile}");
    }

    private async Task LoadAndViewHandHistory()
    {
        var historyFiles = _logger.GetAvailableHandHistoryFiles();
        
        if (!historyFiles.Any())
        {
            _inputHelper.ShowWarning("No hand history files found.");
            _inputHelper.PressAnyKeyToContinue();
            return;
        }

        Console.WriteLine("Available hand history files:");
        for (int i = 0; i < historyFiles.Count; i++)
        {
            Console.WriteLine($"  {i + 1}. {historyFiles[i]}");
        }

        var fileIndex = _inputHelper.GetIntegerInput("Select file number", 1, historyFiles.Count, 1) - 1;
        var selectedFile = historyFiles[fileIndex];
        
        var handRecords = _logger.LoadHandHistory(selectedFile);
        
        if (!handRecords.Any())
        {
            _inputHelper.ShowWarning($"No hands found in {selectedFile}");
            _inputHelper.PressAnyKeyToContinue();
            return;
        }

        await ViewHandRecords(handRecords, selectedFile);
    }

    private async Task ViewHandRecords(List<HandRecord> handRecords, string source)
    {
        Console.WriteLine($"\n📚 Viewing hands from: {source}");
        Console.WriteLine($"Total hands: {handRecords.Count}");
        Console.WriteLine();

        for (int i = 0; i < handRecords.Count; i++)
        {
            var hand = handRecords[i];
            Console.WriteLine($"{i + 1,3}. Hand #{hand.HandNumber} - {hand.StartTime:yyyy-MM-dd HH:mm} - Pot: ${hand.TotalPot}");
        }

        while (true)
        {
            Console.WriteLine();
            var choice = _inputHelper.GetChoiceInput("Select an option:", new Dictionary<string, string>
            {
                {"View specific hand", "view"},
                {"Step through all hands", "step"},
                {"Show summary statistics", "summary"},
                {"Back to replay menu", "back"}
            }, "view");

            switch (choice)
            {
                case "view":
                    await ViewSpecificHand(handRecords);
                    break;
                case "step":
                    await StepThroughHands(handRecords);
                    break;
                case "summary":
                    ShowHandSummaryStatistics(handRecords);
                    break;
                case "back":
                    return;
            }
        }
    }

    private async Task ViewSpecificHand(List<HandRecord> handRecords)
    {
        var handNumber = _inputHelper.GetIntegerInput("Enter hand number to view", 1, handRecords.Count, 1);
        var hand = handRecords[handNumber - 1];
        
        await DisplayHandReplay(hand);
        _inputHelper.PressAnyKeyToContinue();
    }

    private async Task StepThroughHands(List<HandRecord> handRecords)
    {
        for (int i = 0; i < handRecords.Count; i++)
        {
            _inputHelper.ClearScreen();
            Console.WriteLine($"📽️  STEPPING THROUGH HANDS ({i + 1}/{handRecords.Count})");
            Console.WriteLine("=================================================");
            
            await DisplayHandReplay(handRecords[i]);
            
            Console.WriteLine();
            if (i < handRecords.Count - 1)
            {
                var continueChoice = _inputHelper.GetChoiceInput("Continue?", new Dictionary<string, string>
                {
                    {"Next hand", "next"},
                    {"Stop stepping", "stop"}
                }, "next");
                
                if (continueChoice == "stop") break;
            }
            else
            {
                Console.WriteLine("🏁 Reached the end of hand history.");
                _inputHelper.PressAnyKeyToContinue();
            }
        }
    }

    private async Task DisplayHandReplay(HandRecord hand)
    {
        Console.WriteLine($"\n🎲 HAND #{hand.HandNumber} REPLAY");
        Console.WriteLine($"Time: {hand.StartTime:yyyy-MM-dd HH:mm:ss} - {hand.EndTime:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"Duration: {(hand.EndTime - hand.StartTime).TotalSeconds:F1} seconds");
        Console.WriteLine($"Blinds: ${hand.SmallBlind}/${hand.BigBlind}");
        Console.WriteLine();

        // Show starting positions
        Console.WriteLine("🪑 STARTING POSITIONS:");
        for (int i = 0; i < hand.Players.Count; i++)
        {
            var player = hand.Players[i];
            var position = i == hand.DealerPosition ? " (Dealer)" : "";
            var personality = player.IsHuman ? "Human" : player.Personality ?? "AI";
            Console.WriteLine($"   Seat {i + 1}: {player.Name} - ${player.ChipsBefore} ({personality}){position}");
        }
        Console.WriteLine();

        // Show hole cards (only for humans in original game)
        Console.WriteLine("🃏 HOLE CARDS:");
        foreach (var player in hand.Players)
        {
            if (player.IsHuman)
            {
                Console.WriteLine($"   {player.Name}: {string.Join(" ", player.HoleCards.Select(c => c.GetDisplayString()))}");
            }
            else
            {
                Console.WriteLine($"   {player.Name}: [Hidden] [Hidden]");
            }
        }
        Console.WriteLine();

        // Show betting rounds
        foreach (var round in hand.BettingRounds)
        {
            Console.WriteLine($"💰 {round.Phase} BETTING:");
            
            if (round.Phase != BettingPhase.PreFlop && hand.CommunityCards.Any())
            {
                var cardsToShow = round.Phase switch
                {
                    BettingPhase.Flop => hand.CommunityCards.Take(3),
                    BettingPhase.Turn => hand.CommunityCards.Take(4),
                    BettingPhase.River => hand.CommunityCards.Take(5),
                    _ => hand.CommunityCards
                };
                Console.WriteLine($"   Board: {string.Join(" ", cardsToShow.Select(c => c.GetDisplayString()))}");
            }

            foreach (var action in round.Actions)
            {
                var amountStr = action.Amount > 0 ? $" ${action.Amount}" : "";
                Console.WriteLine($"   {action.PlayerId}: {action.Action}{amountStr}");
            }
            
            if (round.TotalBet > 0)
            {
                Console.WriteLine($"   Total bet this round: ${round.TotalBet}");
            }
            Console.WriteLine();

            await Task.Delay(500); // Small delay for readability
        }

        // Show final community cards
        if (hand.CommunityCards.Count == 5)
        {
            Console.WriteLine("🎴 FINAL BOARD:");
            Console.WriteLine($"   {string.Join(" ", hand.CommunityCards.Select(c => c.GetDisplayString()))}");
            Console.WriteLine();
        }

        // Show showdown if applicable
        if (hand.Winners.Any())
        {
            Console.WriteLine("🏆 SHOWDOWN RESULTS:");
            
            // Show all players' final hands
            foreach (var player in hand.Players.Where(p => !p.Folded))
            {
                var allCards = player.HoleCards.Concat(hand.CommunityCards).ToList();
                if (allCards.Count >= 5)
                {
                    var handResult = HandEvaluator.EvaluateHand(allCards);
                    Console.WriteLine($"   {player.Name}: {string.Join(" ", player.HoleCards.Select(c => c.GetDisplayString()))} - {handResult.Description}");
                }
            }
            
            Console.WriteLine();
            Console.WriteLine("💰 WINNERS:");
            foreach (var winner in hand.Winners)
            {
                Console.WriteLine($"   🎉 {winner.PlayerName} wins ${winner.AmountWon} from {winner.PotType}");
                Console.WriteLine($"       with {winner.HandDescription}");
            }
        }
        else
        {
            var winner = hand.Players.FirstOrDefault(p => !p.Folded);
            if (winner != null)
            {
                Console.WriteLine($"🎉 {winner.Name} wins by default (everyone else folded)");
            }
        }

        // Show final chip counts
        Console.WriteLine();
        Console.WriteLine("💰 FINAL CHIP COUNTS:");
        foreach (var player in hand.Players)
        {
            var change = player.ChipsAfter - player.ChipsBefore;
            var changeStr = change > 0 ? $"+${change}" : change < 0 ? $"-${Math.Abs(change)}" : "$0";
            var changeColor = change > 0 ? "gain" : change < 0 ? "loss" : "even";
            
            Console.WriteLine($"   {player.Name}: ${player.ChipsAfter} ({changeStr})");
        }
    }

    private void ShowHandSummaryStatistics(List<HandRecord> handRecords)
    {
        _inputHelper.ClearScreen();
        Console.WriteLine("📊 HAND SUMMARY STATISTICS");
        Console.WriteLine("==========================");
        Console.WriteLine();

        if (!handRecords.Any())
        {
            Console.WriteLine("No hands to analyze.");
            _inputHelper.PressAnyKeyToContinue();
            return;
        }

        var totalHands = handRecords.Count;
        var totalPot = handRecords.Sum(h => h.TotalPot);
        var averagePot = handRecords.Average(h => h.TotalPot);
        var largestPot = handRecords.Max(h => h.TotalPot);
        var smallestPot = handRecords.Min(h => h.TotalPot);

        Console.WriteLine($"Total Hands: {totalHands}");
        Console.WriteLine($"Total Money in Play: ${totalPot:N0}");
        Console.WriteLine($"Average Pot Size: ${averagePot:F0}");
        Console.WriteLine($"Largest Pot: ${largestPot:N0}");
        Console.WriteLine($"Smallest Pot: ${smallestPot:N0}");
        Console.WriteLine();

        // Player statistics
        var allPlayers = handRecords.SelectMany(h => h.Players).GroupBy(p => p.Name);
        
        Console.WriteLine("👥 PLAYER STATISTICS:");
        foreach (var playerGroup in allPlayers.OrderBy(g => g.Key))
        {
            var playerHands = playerGroup.ToList();
            var handsPlayed = playerHands.Count;
            var totalWinnings = playerHands.Sum(p => p.ChipsAfter - p.ChipsBefore);
            var winRate = playerHands.Count(p => p.ChipsAfter > p.ChipsBefore) / (double)handsPlayed * 100;
            var foldRate = playerHands.Count(p => p.Folded) / (double)handsPlayed * 100;

            Console.WriteLine($"   {playerGroup.Key}:");
            Console.WriteLine($"     Hands Played: {handsPlayed}");
            Console.WriteLine($"     Total Winnings: ${totalWinnings:+#;-#;0}");
            Console.WriteLine($"     Win Rate: {winRate:F1}%");
            Console.WriteLine($"     Fold Rate: {foldRate:F1}%");
        }

        Console.WriteLine();
        _inputHelper.PressAnyKeyToContinue();
    }

    private void ShowGameStatistics()
    {
        _inputHelper.ClearScreen();
        Console.WriteLine("📈 GAME STATISTICS");
        Console.WriteLine("==================");
        Console.WriteLine();

        var logFiles = _logger.GetAvailableLogFiles();
        var handHistoryFiles = _logger.GetAvailableHandHistoryFiles();

        Console.WriteLine($"Log Directory: {_logger.GetLogDirectory()}");
        Console.WriteLine($"Available Log Files: {logFiles.Count}");
        Console.WriteLine($"Available Hand History Files: {handHistoryFiles.Count}");
        Console.WriteLine();

        if (handHistoryFiles.Any())
        {
            Console.WriteLine("📁 HAND HISTORY FILES:");
            foreach (var file in handHistoryFiles.OrderByDescending(f => f))
            {
                var filePath = Path.Combine(_logger.GetLogDirectory(), file);
                var fileInfo = new FileInfo(filePath);
                var handCount = _logger.LoadHandHistory(file).Count;
                
                Console.WriteLine($"   {file}");
                Console.WriteLine($"     Created: {fileInfo.CreationTime:yyyy-MM-dd HH:mm}");
                Console.WriteLine($"     Size: {fileInfo.Length / 1024.0:F1} KB");
                Console.WriteLine($"     Hands: {handCount}");
            }
        }

        Console.WriteLine();
        _inputHelper.PressAnyKeyToContinue();
    }
}