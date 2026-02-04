using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantPOS.Services.Interfaces;

namespace RestaurantPOS.ViewModels
{
    public partial class MainMenuViewModel : BaseViewModel
    {
        private readonly INavigationService _navigationService;

        public MainMenuViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
        }

        [RelayCommand]
        private void GoToSelling()
        {
            _navigationService.Navigate<OrderEntryViewModel>();
        }

        [RelayCommand]
        private void GoToBackoffice()
        {
            _navigationService.Navigate<CashControlViewModel>();
        }
    }
}
