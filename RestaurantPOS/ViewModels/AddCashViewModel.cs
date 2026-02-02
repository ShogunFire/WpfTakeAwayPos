using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantPOS.Services.Interfaces;

namespace RestaurantPOS.ViewModels
{
    public partial class AddCashViewModel : BaseViewModel
    {
        private readonly ICashControlService _cashControlService;
        private readonly IDialogService _dialogService;

        [ObservableProperty]
        private decimal keypadValue;

        public AddCashViewModel(ICashControlService cashControlService, IDialogService dialogService)
        {
            _cashControlService = cashControlService;
            _dialogService = dialogService;
        }

        [RelayCommand(CanExecute = nameof(CanSubmit))]
        private async void Submit()
        {
            _cashControlService.AddCash(KeypadValue, "Cash Addition");
            KeypadValue = 0;
            await _dialogService.Alert($"Cash addition of ${KeypadValue:F2} completed successfully!", "Add Cash");
        }

        [RelayCommand]
        private void Cancel()
        {
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
    }
}
