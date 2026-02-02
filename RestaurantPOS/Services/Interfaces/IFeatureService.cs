namespace RestaurantPOS.Services.Interfaces
{
    public interface IFeatureService
    {
        bool InventoryModuleEnabled { get; set; }
        bool InventoryCostModuleEnabled { get; set; }
    }
}
