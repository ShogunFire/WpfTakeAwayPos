using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantPOS.Models;
using RestaurantPOS.Services.Interfaces;
using System.Collections.ObjectModel;

namespace RestaurantPOS.ViewModels
{
    public partial class InventoryViewModel : BaseViewModel
    {
        private readonly IInventoryService _inventoryService;
        private readonly IPopupService _popupService;
        private readonly AddInventoryViewModel _addInventoryViewModel;
        private readonly RemoveInventoryViewModel _removeInventoryViewModel;

        [ObservableProperty]
        private ObservableCollection<InventoryItem> inventoryItems = new();

        public InventoryViewModel(
            IInventoryService inventoryService,
            IPopupService popupService,
            AddInventoryViewModel addInventoryViewModel,
            RemoveInventoryViewModel removeInventoryViewModel)
        {
            _inventoryService = inventoryService;
            _popupService = popupService;
            _addInventoryViewModel = addInventoryViewModel;
            _removeInventoryViewModel = removeInventoryViewModel;
            InventoryItems = _inventoryService.InventoryItems;
        }

        [RelayCommand]
        private void ShowAddPopup(InventoryItem item)
        {
            _addInventoryViewModel.Initialize(item);
            _popupService.Show<AddInventoryViewModel>();
        }

        [RelayCommand]
        private void ShowRemovePopup(InventoryItem item)
        {
            _removeInventoryViewModel.Initialize(item);
            _popupService.Show<RemoveInventoryViewModel>();
        }
    }
}