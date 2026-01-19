using Microsoft.EntityFrameworkCore;
using TexasHoldem.Data.Entities;

namespace TexasHoldem.Data.Services;

/// <summary>
/// Service for managing application settings stored in SQLite.
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly GameLogDbContext _context;

    public SettingsService(GameLogDbContext context)
    {
        _context = context;
    }

    public async Task<ProgramSettingsEntity> GetProgramSettingsAsync()
    {
        var settings = await _context.ProgramSettings.FirstOrDefaultAsync();
        if (settings == null)
        {
            settings = new ProgramSettingsEntity();
            _context.ProgramSettings.Add(settings);
            await _context.SaveChangesAsync();
        }
        return settings;
    }

    public async Task SaveProgramSettingsAsync(ProgramSettingsEntity settings)
    {
        settings.Id = 1; // Ensure single row
        settings.UpdatedAt = DateTime.UtcNow;

        var existing = await _context.ProgramSettings.FirstOrDefaultAsync();
        if (existing == null)
        {
            _context.ProgramSettings.Add(settings);
        }
        else
        {
            _context.Entry(existing).CurrentValues.SetValues(settings);
        }

        await _context.SaveChangesAsync();
    }

    public async Task<GameDefaultsEntity> GetGameDefaultsAsync()
    {
        var defaults = await _context.GameDefaults.FirstOrDefaultAsync();
        if (defaults == null)
        {
            defaults = new GameDefaultsEntity();
            _context.GameDefaults.Add(defaults);
            await _context.SaveChangesAsync();
        }
        return defaults;
    }

    public async Task SaveGameDefaultsAsync(GameDefaultsEntity defaults)
    {
        defaults.Id = 1; // Ensure single row
        defaults.UpdatedAt = DateTime.UtcNow;

        var existing = await _context.GameDefaults.FirstOrDefaultAsync();
        if (existing == null)
        {
            _context.GameDefaults.Add(defaults);
        }
        else
        {
            _context.Entry(existing).CurrentValues.SetValues(defaults);
        }

        await _context.SaveChangesAsync();
    }

    public async Task ResetToDefaultsAsync()
    {
        // Remove existing settings
        var programSettings = await _context.ProgramSettings.FirstOrDefaultAsync();
        if (programSettings != null)
        {
            _context.ProgramSettings.Remove(programSettings);
        }

        var gameDefaults = await _context.GameDefaults.FirstOrDefaultAsync();
        if (gameDefaults != null)
        {
            _context.GameDefaults.Remove(gameDefaults);
        }

        await _context.SaveChangesAsync();

        // Create new defaults
        _context.ProgramSettings.Add(new ProgramSettingsEntity());
        _context.GameDefaults.Add(new GameDefaultsEntity());
        await _context.SaveChangesAsync();
    }

    public async Task EnsureSettingsExistAsync()
    {
        var programSettingsExist = await _context.ProgramSettings.AnyAsync();
        if (!programSettingsExist)
        {
            _context.ProgramSettings.Add(new ProgramSettingsEntity());
        }

        var gameDefaultsExist = await _context.GameDefaults.AnyAsync();
        if (!gameDefaultsExist)
        {
            _context.GameDefaults.Add(new GameDefaultsEntity());
        }

        await _context.SaveChangesAsync();
    }
}
