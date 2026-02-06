using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantPOS.Models;
using RestaurantPOS.Services.Interfaces;
using System.Collections.ObjectModel;

namespace RestaurantPOS.ViewModels
{
    public partial class RemoveInventoryViewModel : BaseViewModel
    {
        private readonly IInventoryService _inventoryService;
        private readonly IPopupService _popupService;

        [ObservableProperty]
        private InventoryItem activeInventoryItem;

        [ObservableProperty]
        private KeypadTarget activeTarget;

        [ObservableProperty]
        private decimal quantity;

        [ObservableProperty]
        private string selectedRemovalReason;

        [ObservableProperty]
        private ObservableCollection<string> removalReasons = new() { "Missing", "Waste" };

        public RemoveInventoryViewModel(IInventoryService inventoryService, IPopupService popupService)
        {
            _inventoryService = inventoryService;
            _popupService = popupService;
        }

        public void Initialize(InventoryItem item)
        {
            ActiveInventoryItem = item;
            Quantity = 0;
            SelectedRemovalReason = null;
            ActiveTarget = KeypadTarget.Quantity;
            ConfirmAdjustmentCommand.NotifyCanExecuteChanged();
        }

        public decimal KeypadValue
        {
            get => Quantity;
            set => Quantity = value;
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
            if (ActiveInventoryItem == null || Quantity <= 0 || string.IsNullOrWhiteSpace(SelectedRemovalReason))
                return;

            _inventoryService.TryConsume(ActiveInventoryItem.InventoryItemId, Quantity, SelectedRemovalReason);
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

            if (string.IsNullOrWhiteSpace(SelectedRemovalReason))
                return false;

            return true;
        }

        partial void OnSelectedRemovalReasonChanged(string value)
        {
            ConfirmAdjustmentCommand.NotifyCanExecuteChanged();
        }

        partial void OnQuantityChanged(decimal value)
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
