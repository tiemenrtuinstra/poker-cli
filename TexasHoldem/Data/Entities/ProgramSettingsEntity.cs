using System.ComponentModel.DataAnnotations;

namespace TexasHoldem.Data.Entities;

/// <summary>
/// Program-wide settings that persist across all games.
/// Stored as a single row in the database.
/// </summary>
public class ProgramSettingsEntity
{
    [Key]
    public int Id { get; set; } = 1; // Always 1, single row

    // Display Settings
    public bool UseColors { get; set; } = true;
    public bool EnableAsciiArt { get; set; } = true;
    public bool UseUnicodeSymbols { get; set; } = true;
    public bool ClearScreenBetweenHands { get; set; } = true;
    public bool ShowHandRankings { get; set; } = true;
    public double AnimationSpeed { get; set; } = 1.0;

    // AI Behavior Settings
    public int ThinkingDelayMin { get; set; } = 500;
    public int ThinkingDelayMax { get; set; } = 2000;
    public bool EnablePokerTalk { get; set; } = true;
    public double PokerTalkFrequency { get; set; } = 0.2;

    // AI Provider Settings (API keys stay in .env)
    public string EnabledProviders { get; set; } = "None"; // Comma-separated: "None" or "Claude,Gemini,OpenAI"
    public string ClaudeModel { get; set; } = "claude-3-haiku-20240307";
    public string GeminiModel { get; set; } = "gemini-2.0-flash";
    public string OpenAiModel { get; set; } = "gpt-4o-mini";

    // Player Name Settings
    public bool UseFunnyAiNames { get; set; } = true;
    public string DefaultHumanNames { get; set; } = "Player 1,Player 2,Player 3"; // Comma-separated
    public string CustomAiNames { get; set; } = ""; // Comma-separated

    // Update Settings
    public bool CheckForUpdatesOnStartup { get; set; } = true;

    // Logging Settings (always enabled, but log level configurable)
    public string LogLevel { get; set; } = "Info";

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
