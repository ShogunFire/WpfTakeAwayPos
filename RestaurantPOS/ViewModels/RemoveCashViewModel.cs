using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantPOS.Services.Interfaces;
using System.Collections.ObjectModel;

namespace RestaurantPOS.ViewModels
{
    public partial class RemoveCashViewModel : BaseViewModel
    {
        private readonly ICashControlService _cashControlService;
        private readonly IDialogService _dialogService;

        [ObservableProperty]
        private decimal keypadValue;

        [ObservableProperty]
        private string selectedRemovalReason;

        [ObservableProperty]
        private bool isExpense;

        [ObservableProperty]
        private ObservableCollection<string> removalReasons = new()
        {
            "Bank Deposit",
            "Petty Cash",
            "Change Fund",
            "Safe Drop",
            "Rent",
            "Utilities",
            "Payroll",
            "Supplies",
            "Equipment",
            "Maintenance",
            "Other"
        };

        public RemoveCashViewModel(ICashControlService cashControlService, IDialogService dialogService)
        {
            _cashControlService = cashControlService;
            _dialogService = dialogService;
        }

        [RelayCommand(CanExecute = nameof(CanSubmit))]
        private async void Submit()
        {
            if (!string.IsNullOrWhiteSpace(SelectedRemovalReason))
            {
                _cashControlService.RemoveCash(KeypadValue, SelectedRemovalReason, IsExpense);
                await _dialogService.Alert($"Removed ${KeypadValue:F2} - {SelectedRemovalReason}", "Cash Removal");
                KeypadValue = 0;
                SelectedRemovalReason = null;
                IsExpense = false;
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            KeypadValue = 0;
            SelectedRemovalReason = null;
        }

        private bool CanSubmit()
        {
            return KeypadValue > 0 && !string.IsNullOrWhiteSpace(SelectedRemovalReason);
        }

        partial void OnKeypadValueChanged(decimal value)
        {
            SubmitCommand.NotifyCanExecuteChanged();
        }

        partial void OnSelectedRemovalReasonChanged(string value)
        {
            SubmitCommand.NotifyCanExecuteChanged();
        }
    }
}
