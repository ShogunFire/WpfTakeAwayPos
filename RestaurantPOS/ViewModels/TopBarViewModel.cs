using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantPOS.Services.Interfaces;
using RestaurantSynchronizationLib.Services;
using System.Threading.Tasks;

namespace RestaurantPOS.ViewModels
{
    public partial class TopBarViewModel : BaseViewModel
    {
        private readonly INavigationService _navigationService;
        private readonly IPopupService _popupService;
        private readonly EndShiftViewModel _endShiftViewModel;
        private readonly TimedSyncService _syncService;
        private readonly IDialogService _dialogService;

        [ObservableProperty]
        private bool showHomeButton;

        [ObservableProperty]
        private bool showTopMenuButtons = true;

        [ObservableProperty]
        private bool isSyncing;

        public TopBarViewModel(
            INavigationService navigationService,
            IPopupService popupService,
            EndShiftViewModel endShiftViewModel,
            TimedSyncService syncService,
            IDialogService dialogService)
        {
            _navigationService = navigationService;
            _popupService = popupService;
            _endShiftViewModel = endShiftViewModel;
            _syncService = syncService;
            _dialogService = dialogService;

            // Subscribe to navigation changes for ShowHomeButton and ShowTopMenuButtons
            _navigationService.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(INavigationService.ShowHomeButton))
                {
                    ShowHomeButton = _navigationService.ShowHomeButton;
                }
                else if (e.PropertyName == nameof(INavigationService.CurrentViewModel))
                {
                    // Hide buttons when on ShiftSummaryView or LoginView
                    ShowTopMenuButtons = !(_navigationService.CurrentViewModel is ShiftSummaryViewModel || _navigationService.CurrentViewModel is LoginViewModel);
                }
            };

            ShowHomeButton = _navigationService.ShowHomeButton;
            ShowTopMenuButtons = !(_navigationService.CurrentViewModel is ShiftSummaryViewModel || _navigationService.CurrentViewModel is LoginViewModel);
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

            _endShiftViewModel.RefreshData();
            _popupService.Show<EndShiftViewModel>();
        }

        [RelayCommand]
        public async Task SyncNow()
        {
            if (IsSyncing)
                return;

            try
            {
                IsSyncing = true;
                var result = await _syncService.SyncNowAsync();

                if (result.Success)
                {
                    await _dialogService.Alert(
                        $"Sync complete: {result.SyncedCount} synced, {result.AlreadyProcessedCount} already processed, {result.FailedCount} failed.",
                        "Sync");
                }
                else
                {
                    await _dialogService.Alert($"Sync failed: {result.Message}", "Sync");
                }
            }
            finally
            {
                IsSyncing = false;
            }
        }
    }
}
