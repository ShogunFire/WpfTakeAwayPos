using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantPOS.Services.Interfaces;

namespace RestaurantPOS.ViewModels
{
    public partial class LoginViewModel : BaseViewModel
    {
        private readonly INavigationService _navigationService;
        private readonly IShiftService _shiftService;
        private readonly ICashControlService _cashControlService;

        public LoginViewModel(
            INavigationService navigationService,
            IShiftService shiftService,
            ICashControlService cashControlService)
        {
            _navigationService = navigationService;
            _shiftService = shiftService;
            _cashControlService = cashControlService;
        }

        [RelayCommand]
        private void Login()
        {
            // Open a new shift and reset cash to opening float
            _shiftService.StartNewShift(_cashControlService.OpeningFloat);
            _cashControlService.ResetShift();

            _navigationService.Navigate<MainMenuViewModel>();
        }
    }
}
