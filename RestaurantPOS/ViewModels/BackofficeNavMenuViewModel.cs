using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantPOS.Services.Interfaces;

namespace RestaurantPOS.ViewModels
{
    public partial class BackofficeNavMenuViewModel : BaseViewModel
    {
        private readonly INavigationService _navigationService;

        public BackofficeNavMenuViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
        }

        [RelayCommand]
        private void GoToInventory()
        {
            _navigationService.Navigate<InventoryViewModel>();
        }

        [RelayCommand]
        private void GoToAddCash()
        {
            _navigationService.Navigate<AddCashViewModel>();
        }

        [RelayCommand]
        private void GoToRemoveCash()
        {
            _navigationService.Navigate<RemoveCashViewModel>();
        }
    }
}
