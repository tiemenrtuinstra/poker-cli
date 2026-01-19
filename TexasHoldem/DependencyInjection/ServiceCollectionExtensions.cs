using Microsoft.Extensions.DependencyInjection;
using TexasHoldem.CLI;

namespace TexasHoldem.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPokerServices(this IServiceCollection services)
    {
        // Configuration - singleton, loads once at startup
        services.AddSingleton<ConfigurationManager>();

        // UI services
        services.AddSingleton<IGameUI, SpectreGameUI>();
        services.AddTransient<InputHelper>();

        // Menu system
        services.AddTransient<NetworkMenu>();
        services.AddTransient<Menu>();

        return services;
    }
}
