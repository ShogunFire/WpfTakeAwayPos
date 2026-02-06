using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantPOS.Services.Interfaces;

namespace RestaurantPOS.ViewModels
{
    public partial class EndShiftViewModel : BaseViewModel
    {
        private readonly ICashControlService _cashControlService;
        private readonly IPopupService _popupService;
        private readonly IShiftService _shiftService;
        private readonly INavigationService _navigationService;
        private readonly ShiftSummaryViewModel _shiftSummaryViewModel;

        [ObservableProperty]
        private decimal keypadValue;

        [ObservableProperty]
        private decimal expectedCash;

        [ObservableProperty]
        private decimal countedCash;

        [ObservableProperty]
        private decimal difference;

        [ObservableProperty]
        private decimal openingFloat;

        public EndShiftViewModel(
            ICashControlService cashControlService, 
            IPopupService popupService,
            IShiftService shiftService,
            INavigationService navigationService,
            ShiftSummaryViewModel shiftSummaryViewModel)
        {
            _cashControlService = cashControlService;
            _popupService = popupService;
            _shiftService = shiftService;
            _navigationService = navigationService;
            _shiftSummaryViewModel = shiftSummaryViewModel;
            RefreshData();
        }

        [RelayCommand(CanExecute = nameof(CanSubmit))]
        public void Submit()
        {
            // End the current shift with counted and expected cash
            var completedShift = _shiftService.EndShift(KeypadValue, ExpectedCash, "");
            
            _popupService.Close();
            KeypadValue = 0;
            
            // Load summary and navigate to shift summary
            if (completedShift != null)
            {
                _shiftSummaryViewModel.LoadShiftSummary(completedShift);
            }

            _navigationService.Navigate<ShiftSummaryViewModel>();
        }

        [RelayCommand]
        public void Cancel()
        {
            _popupService.Close();
            KeypadValue = 0;
        }

        private bool CanSubmit()
        {
            return KeypadValue > 0;
        }

        partial void OnKeypadValueChanged(decimal value)
        {
            SubmitCommand.NotifyCanExecuteChanged();
        }

        public void RefreshData()
        {
            OpeningFloat = _cashControlService.OpeningFloat;
            ExpectedCash = _cashControlService.ExpectedCash;
            CountedCash = _cashControlService.ActualCash;
            Difference = _cashControlService.Difference;
        }
    }
}
