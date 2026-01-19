using Microsoft.Extensions.DependencyInjection;
using TexasHoldem.CLI;
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

        return services;
    }
}
