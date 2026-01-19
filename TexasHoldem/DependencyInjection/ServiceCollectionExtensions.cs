using Microsoft.Extensions.DependencyInjection;
using TexasHoldem.CLI;
using TexasHoldem.Data;
using TexasHoldem.Data.Configuration;
using TexasHoldem.Data.Observers;
using TexasHoldem.Data.Repositories;
using TexasHoldem.Data.Services;
using TexasHoldem.Game.Events;

namespace TexasHoldem.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPokerServices(this IServiceCollection services)
    {
        // Configuration - singleton, loads once at startup
        services.AddSingleton<ConfigurationManager>();

        // Event system - singleton so all components share the same publisher
        services.AddSingleton<IGameEventPublisher, GameEventPublisher>();

        // UI services
        services.AddSingleton<IGameUI, SpectreGameUI>();
        services.AddTransient<InputHelper>();

        // Menu system
        services.AddTransient<NetworkMenu>();
        services.AddTransient<Menu>();

        // Database services
        services.AddSingleton<DatabaseSettings>();
        services.AddDbContext<GameLogDbContext>(ServiceLifetime.Transient);
        services.AddTransient<IGameLogRepository, GameLogRepository>();
        services.AddTransient<IGameLogService, GameLogService>();
        services.AddTransient<IGameHistoryQueryService, GameHistoryQueryService>();

        // Opponent profiling for AI learning
        services.AddTransient<IOpponentProfiler, OpponentProfiler>();

        // Game logging observer - singleton to persist across games
        services.AddSingleton<GameLogObserver>();

        // Hand history menu for viewing statistics
        services.AddTransient<HandHistoryMenu>();

        return services;
    }
}
