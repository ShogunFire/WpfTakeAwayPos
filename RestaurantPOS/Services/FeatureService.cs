using CommunityToolkit.Mvvm.ComponentModel;
using RestaurantPOS.Services.Interfaces;

namespace RestaurantPOS.Services
{
    public partial class FeatureService : ObservableObject, IFeatureService
    {
        [ObservableProperty]
        private bool inventoryModuleEnabled = true;

        [ObservableProperty]
        private bool inventoryCostModuleEnabled = false;
    }
}
