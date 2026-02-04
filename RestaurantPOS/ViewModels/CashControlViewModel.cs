using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantPOS.Models;
using RestaurantPOS.Services.Interfaces;
using System.Collections.ObjectModel;
using System.Linq;

namespace RestaurantPOS.ViewModels
{
    public enum CashFlowType
    {
        Add,
        Remove
    }

    public partial class CashControlViewModel : BaseViewModel
    {
        private readonly ICashControlService _cashControlService;
        private readonly IPopupService _popupService;
        private readonly IDialogService _dialogService;
        private readonly StepPopupViewModel _stepPopupViewModel;

        [ObservableProperty]
        private ObservableCollection<CashTransaction> transactions = new();

        public CashControlViewModel(ICashControlService cashControlService, IPopupService popupService, IDialogService dialogService, StepPopupViewModel stepPopupViewModel)
        {
            _cashControlService = cashControlService;
            _popupService = popupService;
            _dialogService = dialogService;
            _stepPopupViewModel = stepPopupViewModel;

            RefreshTransactions();
        }

        [RelayCommand]
        private void AddCash()
        {
            StartCashFlow(CashFlowType.Add);
        }

        [RelayCommand]
        private void RemoveCash()
        {
            StartCashFlow(CashFlowType.Remove);
        }

        private void StartCashFlow(CashFlowType flowType)
        {
            var flow = new CashFlowWizardViewModel(_cashControlService, _dialogService, flowType, RefreshTransactions);
            _stepPopupViewModel.Initialize(flow);
            _popupService.Show<StepPopupViewModel>();
        }

        private void RefreshTransactions()
        {
            var trans = _cashControlService.GetTransactions()
                .Where(t => t.Type != CashTransactionType.Sale);
            Transactions = new ObservableCollection<CashTransaction>(trans);
        }
    }
}
