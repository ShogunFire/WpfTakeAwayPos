using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantPOS.Models;
using RestaurantPOS.Services.Interfaces;
using System.Collections.ObjectModel;
using System.Linq;

namespace RestaurantPOS.ViewModels
{
    public partial class CashControlViewModel : BaseViewModel
    {
        private readonly INavigationService _navigationService;
        private readonly ICashControlService _cashControlService;

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

        [ObservableProperty]
        private bool isCounted;

        [ObservableProperty]
        private bool isKeypadOpen;

        [ObservableProperty]
        private string popupMode; // "EndShift", "RemoveCash", "AddCash"

        [ObservableProperty]
        private string selectedRemovalReason;

        [ObservableProperty]
        private ObservableCollection<string> removalReasons = new() 
        { 
            "Bank Deposit", 
            "Petty Cash", 
            "Change Fund", 
            "Safe Drop",
            "Other" 
        };

        public CashControlViewModel(INavigationService navigationService, ICashControlService cashControlService)
        {
            _navigationService = navigationService;
            _cashControlService = cashControlService;
            
            RefreshData();
        }

        [RelayCommand]
        private void ShowEndShiftPopup()
        {
            PopupMode = "EndShift";
            KeypadValue = 0;
            IsKeypadOpen = true;
        }

        [RelayCommand]
        private void ShowRemoveCashPopup()
        {
            PopupMode = "RemoveCash";
            KeypadValue = 0;
            SelectedRemovalReason = null;
            IsKeypadOpen = true;
        }

        [RelayCommand]
        private void ShowAddCashPopup()
        {
            PopupMode = "AddCash";
            KeypadValue = 0;
            IsKeypadOpen = true;
        }

        [RelayCommand]
        private void CloseKeypad()
        {
            IsKeypadOpen = false;
            KeypadValue = 0;
            SelectedRemovalReason = null;
        }

        [RelayCommand(CanExecute = nameof(CanConfirm))]
        private void Confirm()
        {
            switch (PopupMode)
            {
                case "EndShift":
                    _cashControlService.SetActualCash(KeypadValue);
                    break;
                case "RemoveCash":
                    if (!string.IsNullOrWhiteSpace(SelectedRemovalReason))
                    {
                        _cashControlService.RemoveCash(KeypadValue, SelectedRemovalReason);
                    }
                    break;
                case "AddCash":
                    _cashControlService.AddCash(KeypadValue, "Cash Addition");
                    break;
            }

            RefreshData();
            CloseKeypad();
        }

        private bool CanConfirm()
        {
            if (KeypadValue <= 0) return false;

            if (PopupMode == "RemoveCash" && string.IsNullOrWhiteSpace(SelectedRemovalReason))
                return false;

            return true;
        }

        partial void OnKeypadValueChanged(decimal value)
        {
            ConfirmCommand.NotifyCanExecuteChanged();
        }

        partial void OnSelectedRemovalReasonChanged(string value)
        {
            ConfirmCommand.NotifyCanExecuteChanged();
        }

        private void RefreshData()
        {
            OpeningFloat = _cashControlService.OpeningFloat;
            ExpectedCash = _cashControlService.ExpectedCash;
            IsCounted = _cashControlService.IsCounted;
            CountedCash = _cashControlService.ActualCash;
            Difference = _cashControlService.Difference;
        }
    }
}
