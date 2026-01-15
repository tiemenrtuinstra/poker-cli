namespace TexasHoldem.CLI;

/// <summary>
/// Provides Unicode or ASCII symbols based on configuration.
/// Use this class for all symbols to ensure consistent display across terminals.
/// </summary>
public static class Symbols
{
    private static bool _useUnicode = true;

    /// <summary>
    /// Initialize symbols based on configuration
    /// </summary>
    public static void Initialize(bool useUnicode)
    {
        _useUnicode = useUnicode;
    }

    public static bool UseUnicode => _useUnicode;

    // Currency
    public static string Euro => _useUnicode ? "€" : "EUR";
    public static string Dollar => _useUnicode ? "$" : "USD";

    // Card suits
    public static string Spade => _useUnicode ? "♠" : "S";
    public static string Heart => _useUnicode ? "♥" : "H";
    public static string Diamond => _useUnicode ? "♦" : "D";
    public static string Club => _useUnicode ? "♣" : "C";

    // Game status indicators
    public static string Check => _useUnicode ? "✓" : "[OK]";
    public static string Cross => _useUnicode ? "✗" : "[X]";
    public static string Warning => _useUnicode ? "⚠️" : "[!]";
    public static string Error => _useUnicode ? "❌" : "[ERR]";
    public static string Success => _useUnicode ? "✅" : "[OK]";
    public static string Info => _useUnicode ? "ℹ️" : "[i]";

    // Player/Game icons
    public static string Trophy => _useUnicode ? "🏆" : "[1ST]";
    public static string Cards => _useUnicode ? "🎴" : "[CARDS]";
    public static string Chip => _useUnicode ? "🎰" : "[CHIP]";
    public static string Money => _useUnicode ? "💰" : "[POT]";
    public static string Robot => _useUnicode ? "🤖" : "[AI]";
    public static string Human => _useUnicode ? "👤" : "[P]";
    public static string Dealer => _useUnicode ? "🎯" : "[D]";
    public static string Network => _useUnicode ? "🌐" : "[NET]";
    public static string Chat => _useUnicode ? "💬" : "[CHAT]";
    public static string Clock => _useUnicode ? "⏰" : "[TIME]";
    public static string Fire => _useUnicode ? "🔥" : "[!]";
    public static string Star => _useUnicode ? "⭐" : "[*]";
    public static string GameController => _useUnicode ? "🎮" : "[GAME]";
    public static string Settings => _useUnicode ? "⚙️" : "[CFG]";
    public static string File => _useUnicode ? "📁" : "[FILE]";
    public static string Clipboard => _useUnicode ? "📋" : "[LIST]";

    // Arrows and indicators
    public static string ArrowRight => _useUnicode ? "→" : "->";
    public static string ArrowLeft => _useUnicode ? "←" : "<-";
    public static string ArrowUp => _useUnicode ? "↑" : "^";
    public static string ArrowDown => _useUnicode ? "↓" : "v";
    public static string Bullet => _useUnicode ? "•" : "*";

    // Status
    public static string Thinking => _useUnicode ? "🤔" : "[...]";
    public static string Winner => _useUnicode ? "🎉" : "[WIN]";
    public static string Loser => _useUnicode ? "😢" : "[LOSE]";
    public static string AllIn => _useUnicode ? "🔥" : "[ALL-IN]";

    /// <summary>
    /// Get suit symbol for a given suit character
    /// </summary>
    public static string GetSuitSymbol(char suit)
    {
        return suit switch
        {
            'S' or 's' => Spade,
            'H' or 'h' => Heart,
            'D' or 'd' => Diamond,
            'C' or 'c' => Club,
            '♠' => Spade,
            '♥' => Heart,
            '♦' => Diamond,
            '♣' => Club,
            _ => suit.ToString()
        };
    }

    /// <summary>
    /// Format currency amount
    /// </summary>
    public static string FormatCurrency(int amount)
    {
        return _useUnicode ? $"€{amount:N0}" : $"EUR {amount:N0}";
    }

    /// <summary>
    /// Format currency amount with decimal places
    /// </summary>
    public static string FormatCurrency(decimal amount, int decimals = 0)
    {
        var format = decimals > 0 ? $"N{decimals}" : "N0";
        return _useUnicode ? $"€{amount.ToString(format)}" : $"EUR {amount.ToString(format)}";
    }
}
