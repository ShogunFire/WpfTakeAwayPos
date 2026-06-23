using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantPOS.Models;
using RestaurantPOS.Services.Interfaces;
using System;

namespace RestaurantPOS.ViewModels
{
    public partial class AddInventoryViewModel : BaseViewModel
    {
        private readonly IInventoryService _inventoryService;
        private readonly IInventoryCostService _costService;
        private readonly ICashControlService _cashControlService;
        private readonly IPopupService _popupService;

        [ObservableProperty]
        private InventoryItem activeInventoryItem;

        [ObservableProperty]
        private KeypadTarget activeTarget;

        [ObservableProperty]
        private decimal quantity;

        [ObservableProperty]
        private decimal totalCost;

        [ObservableProperty]
        private bool paidWithCash;

        public AddInventoryViewModel(IInventoryService inventoryService, IInventoryCostService costService, ICashControlService cashControlService, IPopupService popupService)
        {
            _inventoryService = inventoryService;
            _costService = costService;
            _cashControlService = cashControlService;
            _popupService = popupService;
        }

        public void Initialize(InventoryItem item)
        {
            ActiveInventoryItem = item;
            Quantity = 0;
            TotalCost = 0;
            PaidWithCash = false;
            ActiveTarget = KeypadTarget.Quantity;
            ConfirmAdjustmentCommand.NotifyCanExecuteChanged();
        }

        public decimal KeypadValue
        {
            get => ActiveTarget switch
            {
                KeypadTarget.Quantity => Quantity,
                KeypadTarget.TotalCost => TotalCost,
                _ => 0
            };
            set
            {
                switch (ActiveTarget)
                {
                    case KeypadTarget.Quantity:
                        Quantity = value;
                        break;
                    case KeypadTarget.TotalCost:
                        TotalCost = value;
                        break;
                }
            }
        }

        [RelayCommand]
        public void SetTarget(object parameter)
        {
            if (parameter is KeypadTarget target)
            {
                ActiveTarget = target;
            }
        }

        [RelayCommand(CanExecute = nameof(CanConfirmAdjustment))]
        public void ConfirmAdjustment()
        {
            if (ActiveInventoryItem == null || Quantity <= 0)
                return;

            // Add stock to inventory
            _inventoryService.AddStock(ActiveInventoryItem.InventoryItemId, Quantity, "Manual Add", TotalCost, PaidWithCash);

            // Record cost if entered
            if (TotalCost > 0)
            {
                _costService.RecordPurchase(
                    ActiveInventoryItem.InventoryItemId,
                    ActiveInventoryItem.Name,
                    Quantity,
                    TotalCost);

                if (PaidWithCash)
                {
                    _cashControlService.RemoveCash(
                        TotalCost,
                        $"Inventory Purchase - {ActiveInventoryItem.Name}",
                        isExpense: true,
                        isInventoryAdd: true);
                }
            }

            _popupService.Close();
        }

        [RelayCommand]
        public void Cancel()
        {
            _popupService.Close();
        }

        private bool CanConfirmAdjustment()
        {
            if (ActiveInventoryItem == null || Quantity <= 0)
                return false;

            if (_costService.IsCostMandatory() && TotalCost <= 0)
                return false;

            return true;
        }

        partial void OnQuantityChanged(decimal value)
        {
            ConfirmAdjustmentCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(KeypadValue));
        }

        partial void OnTotalCostChanged(decimal value)
        {
            ConfirmAdjustmentCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(KeypadValue));
        }

        partial void OnActiveTargetChanged(KeypadTarget value)
        {
            OnPropertyChanged(nameof(KeypadValue));
        }
    }
}
