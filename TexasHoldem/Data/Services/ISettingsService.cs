using TexasHoldem.Data.Entities;

namespace TexasHoldem.Data.Services;

/// <summary>
/// Service for managing application settings stored in SQLite.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Get program-wide settings (display, AI behavior, etc.)
    /// </summary>
    Task<ProgramSettingsEntity> GetProgramSettingsAsync();

    /// <summary>
    /// Save program-wide settings
    /// </summary>
    Task SaveProgramSettingsAsync(ProgramSettingsEntity settings);

    /// <summary>
    /// Get game default settings (players, chips, blinds, etc.)
    /// </summary>
    Task<GameDefaultsEntity> GetGameDefaultsAsync();

    /// <summary>
    /// Save game default settings
    /// </summary>
    Task SaveGameDefaultsAsync(GameDefaultsEntity defaults);

    /// <summary>
    /// Reset all settings to defaults
    /// </summary>
    Task ResetToDefaultsAsync();

    /// <summary>
    /// Initialize settings tables with defaults if they don't exist
    /// </summary>
    Task EnsureSettingsExistAsync();
}
