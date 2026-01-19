using System.Text.Json;
using TexasHoldem.Domain;
using TexasHoldem.Domain.Enums;

namespace TexasHoldem.CLI;

public class Logger
{
    private readonly string _logDirectory;
    private readonly string? _gameLogFile;
    private readonly string? _handHistoryFile;
    private readonly bool _isEnabled;
    private readonly object _lockObject = new();

    public Logger(bool isEnabled = true, string? logDirectory = null)
    {
        _isEnabled = isEnabled;
        _logDirectory = logDirectory ?? Path.Combine(Environment.CurrentDirectory, "logs");

        if (_isEnabled)
        {
            Directory.CreateDirectory(_logDirectory);

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            _gameLogFile = Path.Combine(_logDirectory, $"game_{timestamp}.log");
            _handHistoryFile = Path.Combine(_logDirectory, $"hands_{timestamp}.json");

            LogInfo("=== TEXAS HOLD'EM POKER GAME STARTED ===");
        }
    }

    public void LogInfo(string message)
    {
        if (!_isEnabled || _gameLogFile is null) return;

        var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [INFO] {message}";

        lock (_lockObject)
        {
            Console.WriteLine($"📝 {message}");
            File.AppendAllText(_gameLogFile, logEntry + Environment.NewLine);
        }
    }

    public void LogWarning(string message)
    {
        if (!_isEnabled || _gameLogFile is null) return;

        var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [WARN] {message}";

        lock (_lockObject)
        {
            Console.WriteLine($"⚠️  {message}");
            File.AppendAllText(_gameLogFile, logEntry + Environment.NewLine);
        }
    }

    public void LogError(string message, Exception? exception = null)
    {
        if (!_isEnabled || _gameLogFile is null) return;

        var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [ERROR] {message}";
        if (exception != null)
        {
            logEntry += $" | Exception: {exception.Message}";
        }

        lock (_lockObject)
        {
            Console.WriteLine($"❌ {message}");
            File.AppendAllText(_gameLogFile, logEntry + Environment.NewLine);
        }
    }

    public void LogPlayerAction(string playerName, ActionType action, int amount, BettingPhase phase)
    {
        if (!_isEnabled || _gameLogFile is null) return;

        var amountStr = amount > 0 ? $" €{amount}" : "";
        var message = $"{playerName} {action}{amountStr} during {phase}";

        var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [ACTION] {message}";

        lock (_lockObject)
        {
            File.AppendAllText(_gameLogFile, logEntry + Environment.NewLine);
        }
    }

    public void LogHandStart(int handNumber, List<string> playerNames, int dealerPosition)
    {
        if (!_isEnabled) return;
        
        var message = $"Hand #{handNumber} started. Players: {string.Join(", ", playerNames)}. Dealer: {playerNames[dealerPosition]}";
        LogInfo(message);
    }

    public void LogHandEnd(int handNumber, List<PotWinner> winners, int totalPot)
    {
        if (!_isEnabled) return;
        
        var winnersStr = string.Join(", ", winners.Select(w => $"{w.Player.Name} (€{w.Amount})"));
        var message = $"Hand #{handNumber} ended. Total pot: €{totalPot}. Winners: {winnersStr}";
        LogInfo(message);
    }

    public void LogGameEnd(string? winner, int totalHands)
    {
        if (!_isEnabled) return;
        
        var message = winner != null 
            ? $"Game ended. Winner: {winner} after {totalHands} hands"
            : $"Game ended after {totalHands} hands";
        
        LogInfo(message);
        LogInfo("=== TEXAS HOLD'EM POKER GAME ENDED ===");
    }

    public void SaveHandHistory(HandRecord handRecord)
    {
        if (!_isEnabled || _handHistoryFile is null) return;

        try
        {
            lock (_lockObject)
            {
                var jsonOptions = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var json = JsonSerializer.Serialize(handRecord, jsonOptions);

                // Append to hand history file
                var handEntry = json + Environment.NewLine + "---" + Environment.NewLine;
                File.AppendAllText(_handHistoryFile, handEntry);
            }
        }
        catch (Exception ex)
        {
            LogError($"Failed to save hand history: {ex.Message}", ex);
        }
    }

    public void SaveGameState(GameState gameState, string fileName)
    {
        if (!_isEnabled) return;
        
        try
        {
            var filePath = Path.Combine(_logDirectory, fileName);
            var jsonOptions = new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            var json = JsonSerializer.Serialize(gameState, jsonOptions);
            File.WriteAllText(filePath, json);
            
            LogInfo($"Game state saved to {fileName}");
        }
        catch (Exception ex)
        {
            LogError($"Failed to save game state: {ex.Message}", ex);
        }
    }

    public List<HandRecord> LoadHandHistory(string fileName)
    {
        var handRecords = new List<HandRecord>();
        
        if (!_isEnabled) return handRecords;
        
        try
        {
            var filePath = Path.Combine(_logDirectory, fileName);
            if (!File.Exists(filePath)) return handRecords;
            
            var content = File.ReadAllText(filePath);
            var handEntries = content.Split(new[] { "---" }, StringSplitOptions.RemoveEmptyEntries);
            
            var jsonOptions = new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            foreach (var entry in handEntries)
            {
                var trimmedEntry = entry.Trim();
                if (!string.IsNullOrEmpty(trimmedEntry))
                {
                    var handRecord = JsonSerializer.Deserialize<HandRecord>(trimmedEntry, jsonOptions);
                    if (handRecord != null)
                    {
                        handRecords.Add(handRecord);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogError($"Failed to load hand history: {ex.Message}", ex);
        }
        
        return handRecords;
    }

    public string GetLogDirectory() => _logDirectory;
    
    public List<string> GetAvailableLogFiles()
    {
        if (!_isEnabled || !Directory.Exists(_logDirectory))
            return [];

        return Directory.GetFiles(_logDirectory, "*.log")
            .Select(Path.GetFileName)
            .Where(name => name != null)
            .Cast<string>()
            .ToList();
    }

    public List<string> GetAvailableHandHistoryFiles()
    {
        if (!_isEnabled || !Directory.Exists(_logDirectory))
            return [];

        return Directory.GetFiles(_logDirectory, "hands_*.json")
            .Select(Path.GetFileName)
            .Where(name => name != null)
            .Cast<string>()
            .ToList();
    }

    public void Dispose()
    {
        if (_isEnabled)
        {
            LogInfo("Logger disposed");
        }
    }
}

public class HandRecord
{
    public int HandNumber { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public List<PlayerSnapshot> Players { get; set; } = new();
    public List<Card> CommunityCards { get; set; } = new();
    public List<BettingRoundRecord> BettingRounds { get; set; } = new();
    public List<WinnerRecord> Winners { get; set; } = new();
    public int TotalPot { get; set; }
    public int DealerPosition { get; set; }
    public int SmallBlind { get; set; }
    public int BigBlind { get; set; }
}

public class PlayerSnapshot
{
    public string Name { get; set; } = string.Empty;
    public int ChipsBefore { get; set; }
    public int ChipsAfter { get; set; }
    public List<Card> HoleCards { get; set; } = new();
    public string? Personality { get; set; }
    public bool IsHuman { get; set; }
    public bool Folded { get; set; }
    public bool WentAllIn { get; set; }
}

public class BettingRoundRecord
{
    public BettingPhase Phase { get; set; }
    public List<PlayerAction> Actions { get; set; } = new();
    public int TotalBet { get; set; }
}

public class WinnerRecord
{
    public string PlayerName { get; set; } = string.Empty;
    public int AmountWon { get; set; }
    public string HandDescription { get; set; } = string.Empty;
    public string PotType { get; set; } = string.Empty;
}