using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantPOS.Services.Interfaces;
using System.Threading.Tasks;

namespace RestaurantPOS.ViewModels
{
    public partial class TopBarViewModel : BaseViewModel
    {
        private readonly INavigationService _navigationService;
        private readonly IPopupService _popupService;

        [ObservableProperty]
        private bool showHomeButton;

        public TopBarViewModel(INavigationService navigationService, IPopupService popupService)
        {
            _navigationService = navigationService;
            _popupService = popupService;

            // Subscribe to navigation changes for ShowHomeButton
            _navigationService.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(INavigationService.ShowHomeButton))
                {
                    ShowHomeButton = _navigationService.ShowHomeButton;
                }
            };

            ShowHomeButton = _navigationService.ShowHomeButton;
        }

        [RelayCommand]
        public async Task GoHome()
        {
            var current = _navigationService.CurrentViewModel;

            if (current is INavigationGuard guard)
            {
                if (!await guard.CanNavigateAwayAsync())
                    return;
            }

            _navigationService.GoHome();
        }

        [RelayCommand]
        public async Task EndShift()
        {
            var current = _navigationService.CurrentViewModel;

            if (current is INavigationGuard guard)
            {
                if (!await guard.CanNavigateAwayAsync())
                    return;
            }

            _popupService.Show<EndShiftViewModel>();
        }
    }
}
