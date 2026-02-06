using System;
using System.Threading.Tasks;

namespace RestaurantSynchronizationLib.Services.Deprecated;

/// <summary>
/// DEPRECATED: Use RestaurantPOS.Services.IMasterDataSyncService instead.
/// This interface is kept for backwards compatibility only.
/// </summary>
[Obsolete("Use RestaurantPOS.Services.IMasterDataSyncService instead")]
public interface IMasterDataSyncService
{
    Task<MasterDataSyncResult> SyncMasterDataAsync();
}
