using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace RestaurantSynchronizationLib.Services;

/// <summary>
/// Background service that periodically synchronizes events to the API
/// </summary>
public class TimedSyncService : IDisposable
{
    private readonly EventSynchronizer _synchronizer;
    private readonly ILogger<TimedSyncService> _logger;
    private readonly int _intervalSeconds;
    private Timer? _timer;
    private bool _isRunning;
    private bool _disposed;

    public TimedSyncService(
        EventSynchronizer synchronizer,
        ILogger<TimedSyncService> logger,
        int intervalSeconds = 60)
    {
        _synchronizer = synchronizer ?? throw new ArgumentNullException(nameof(synchronizer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _intervalSeconds = intervalSeconds > 0 ? intervalSeconds : 60;
    }

    /// <summary>
    /// Start the background synchronization service
    /// </summary>
    public void Start()
    {
        if (_isRunning)
        {
            _logger.LogWarning("Sync service is already running");
            return;
        }

        _isRunning = true;
        _logger.LogInformation("Starting timed sync service with interval {Interval} seconds", _intervalSeconds);

        // Create timer that runs immediately, then every interval
        _timer = new Timer(async _ => await ExecuteSyncAsync(), null, TimeSpan.Zero, TimeSpan.FromSeconds(_intervalSeconds));
    }

    /// <summary>
    /// Stop the background synchronization service
    /// </summary>
    public void Stop()
    {
        if (!_isRunning)
        {
            _logger.LogWarning("Sync service is not running");
            return;
        }

        _isRunning = false;
        _timer?.Dispose();
        _timer = null;

        _logger.LogInformation("Stopped timed sync service");
    }

    /// <summary>
    /// Manually trigger a synchronization
    /// </summary>
    public async Task<SyncResult> SyncNowAsync()
    {
        if (!_isRunning)
        {
            _logger.LogWarning("Sync service is not running, but executing sync on demand");
        }

        return await ExecuteSyncAsync();
    }

    /// <summary>
    /// Execute the synchronization
    /// </summary>
    private async Task<SyncResult> ExecuteSyncAsync()
    {
        try
        {
            var result = await _synchronizer.SynchronizeAsync();

            if (result.Success)
            {
                if (result.TotalEvents > 0)
                {
                    _logger.LogInformation(
                        "Sync completed: {Synced} synced, {AlreadyProcessed} already processed, {Failed} failed",
                        result.SyncedCount,
                        result.AlreadyProcessedCount,
                        result.FailedCount);
                }
            }
            else
            {
                _logger.LogWarning("Sync failed: {Message}", result.Message);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during sync execution");
            return new SyncResult { Success = false, Message = ex.Message };
        }
    }

    /// <summary>
    /// Get current sync status
    /// </summary>
    public async Task<SyncStatistics> GetStatusAsync()
    {
        try
        {
            return await _synchronizer.GetStatisticsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sync status");
            throw;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        Stop();
        _timer?.Dispose();
        _disposed = true;
    }
}
