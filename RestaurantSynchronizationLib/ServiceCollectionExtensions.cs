using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RestaurantSynchronizationLib.Configuration;
using RestaurantSynchronizationLib.Persistence;
using RestaurantSynchronizationLib.Services;

namespace RestaurantSynchronizationLib;

/// <summary>
/// Extension methods for registering synchronization services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add RestaurantSynchronization services to the service collection
    /// </summary>
    public static IServiceCollection AddRestaurantSynchronization(
        this IServiceCollection services,
        SyncConfiguration config,
        int timedSyncIntervalSeconds = 60)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        // Register configuration as singleton
        services.AddSingleton(config);

        // Register repositories
        services.AddSingleton<ISyncEventRepository>(sp => new SyncEventRepository(
            config.DatabaseConnectionString,
            sp.GetRequiredService<ILogger<SyncEventRepository>>(),
            config.DeviceId));

        // Register ApiEventClient
        services.AddSingleton(sp => new ApiEventClient(
            config,
            sp.GetRequiredService<ILogger<ApiEventClient>>()));

        // Register event synchronizer
        services.AddSingleton<EventSynchronizer>();

        // Register timed sync service as singleton for better control
        services.AddSingleton(sp => new TimedSyncService(
            sp.GetRequiredService<EventSynchronizer>(),
            sp.GetRequiredService<ILogger<TimedSyncService>>(),
            timedSyncIntervalSeconds));

        return services;
    }

    /// <summary>
    /// Add RestaurantSynchronization services with configuration from settings
    /// </summary>
    public static IServiceCollection AddRestaurantSynchronization(
        this IServiceCollection services,
        string apiBaseAddress,
        string deviceId,
        string databaseConnectionString,
        int timedSyncIntervalSeconds = 60,
        int requestTimeoutSeconds = 30,
        bool useBatchEndpoint = true,
        int batchSize = 10)
    {
        var config = new SyncConfiguration
        {
            ApiBaseAddress = apiBaseAddress,
            DeviceId = deviceId,
            DatabaseConnectionString = databaseConnectionString,
            RequestTimeoutSeconds = requestTimeoutSeconds,
            UseBatchEndpoint = useBatchEndpoint,
            BatchSize = batchSize
        };

        return services.AddRestaurantSynchronization(config, timedSyncIntervalSeconds);
    }
}
