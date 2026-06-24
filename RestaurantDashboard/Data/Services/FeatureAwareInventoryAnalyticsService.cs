using RestaurantDashboard.Data.Models;

namespace RestaurantDashboard.Data.Services;

public class NoopInventoryAnalyticsService : IInventoryAnalyticsService
{
    public Task<List<InventoryStatus>> GetInventoryStatusAsync(Guid? locationId = null)
        => Task.FromResult(new List<InventoryStatus>());
}

public class FeatureAwareInventoryAnalyticsService : IInventoryAnalyticsService
{
    private readonly InventoryAnalyticsService _enabledService;
    private readonly NoopInventoryAnalyticsService _disabledService;
    private readonly FeatureOptionsState _featureState;

    public FeatureAwareInventoryAnalyticsService(
        InventoryAnalyticsService enabledService,
        NoopInventoryAnalyticsService disabledService,
        FeatureOptionsState featureState)
    {
        _enabledService = enabledService;
        _disabledService = disabledService;
        _featureState = featureState;
    }

    public async Task<List<InventoryStatus>> GetInventoryStatusAsync(Guid? locationId = null)
    {
        await _featureState.EnsureLoadedAsync();

        if (!_featureState.Current.InventoryModuleEnabled)
        {
            return await _disabledService.GetInventoryStatusAsync(locationId);
        }

        return await _enabledService.GetInventoryStatusAsync(locationId);
    }
}
