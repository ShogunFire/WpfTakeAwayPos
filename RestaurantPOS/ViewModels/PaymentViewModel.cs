using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantPOS.Models;
using RestaurantPOS.Services;
using RestaurantPOS.Services.Interfaces;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using System.Threading.Tasks;

#nullable enable

namespace RestaurantPOS.ViewModels
{
    public partial class PaymentViewModel : BaseViewModel, INavigationGuard
    {
        private readonly INavigationService _navigationService;
        private readonly IDialogService _dialogService;
        private readonly IInventoryService _inventoryService;
        private readonly ICashControlService _cashControlService;
        private readonly IOrderSession _orderSession;
        public IOrderSession OrderSession => _orderSession;

        /// <summary>
        /// Always references the current order from the session.
        /// This ensures the ViewModel always points to the active order.
        /// </summary>

        [ObservableProperty]
        private string inputDisplay = "0";

        [ObservableProperty]
        private ObservableCollection<PaymentEntry> paymentEntries = new();

        [ObservableProperty]
        private decimal totalCashReceived;

        public PaymentViewModel(INavigationService navigationService, IDialogService dialogService, IInventoryService inventoryService, ICashControlService cashControlService, IOrderSession orderSession)
        {
            _navigationService = navigationService;
            _dialogService = dialogService;
            _inventoryService = inventoryService;
            _cashControlService = cashControlService;
            _orderSession = orderSession;
            
            // Subscribe to order changes
            _orderSession.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(IOrderSession.CurrentOrder))
                    CompleteOrderCommand.NotifyCanExecuteChanged();
            };
            
            _orderSession.OrderLinesChanged += (_, _) =>
                CompleteOrderCommand.NotifyCanExecuteChanged();
            
            UpdateTotals();
        }

       

       
        [RelayCommand]
        private void CashPayment()
        {
            if (decimal.TryParse(InputDisplay, out decimal amount) && amount > 0)
            {
                PaymentEntries.Add(new PaymentEntry 
                { 
                    PaymentMethod = "Cash",
                    Amount = amount,
                    CashReceived = amount,
                    Change = 0
                });

                UpdateTotals();
                InputDisplay = "0";
            }
        }

        [RelayCommand]
        private void CardPayment()
        {
            if (decimal.TryParse(InputDisplay, out decimal amount) && amount > 0)
            {
                PaymentEntries.Add(new PaymentEntry 
                { 
                    PaymentMethod = "Card",
                    Amount = amount 
                });

                UpdateTotals();
                InputDisplay = "0";
            }
        }

       

        [RelayCommand]
        private void RemovePayment(PaymentEntry entry)
        {
            PaymentEntries.Remove(entry);
            UpdateTotals();
        }

        [RelayCommand]
        private void BackToOrder()
        {
            _navigationService.Navigate<OrderEntryViewModel>();
        }

        [RelayCommand(CanExecute = nameof(CanCompleteOrder))]
        private async void CompleteOrder()
        {
            if (_orderSession.CurrentOrder.Remaining <= 0)
            {
                foreach (var line in _orderSession.CurrentOrder.OrderLines)
                {
                    var item = line.Item;
                    if (item == null || item.Components == null || item.Components.Count == 0)
                        continue;

                    foreach (var component in item.Components)
                    {
                        var totalUsed = component.QuantityUsed * line.Quantity;
                        _inventoryService.TryConsume(component.InventoryItemId, totalUsed);
                    }
                }

                // Record cash payments to cash control
                var cashPayments = PaymentEntries.Where(p => p.PaymentMethod == "Cash").Sum(p => p.Amount);
                if (cashPayments > 0)
                {
                    _cashControlService.RecordSale(cashPayments);
                }

                // Show completion dialog
                await _dialogService.Alert("Order completed successfully!", "Order Complete");

                clearOrder();
                
                
            }
        }

        private bool CanCompleteOrder()
        {
            return _orderSession.CurrentOrder.TotalPaid >= _orderSession.CurrentOrder.TotalAmount;
        }

        private void clearOrder()
        {
            _orderSession.Complete();
            
            // Clear payment entries and reset input
            PaymentEntries.Clear();
            InputDisplay = "0";
            
            // Navigate back to order entry
            _navigationService.Navigate<OrderEntryViewModel>();
        }

        public async Task<bool> CanNavigateAwayAsync()
        {
            
            await _dialogService.Alert(
                "Payment not completed. You can't leave this page");
                
           
            return false;
        }

        private void UpdateTotals()
        {
            decimal totalPaid = PaymentEntries.Sum(p => p.Amount);
            _orderSession.CurrentOrder.TotalPaid = totalPaid;
            _orderSession.CurrentOrder.UpdatePaymentCalculations();
            
            // Notify that the command state should be re-evaluated
            CompleteOrderCommand.NotifyCanExecuteChanged();
            
            // Calculate total cash received and change
            TotalCashReceived = PaymentEntries.Where(p => p.PaymentMethod == "Cash").Sum(p => p.CashReceived);
        }
    }

    public partial class PaymentEntry : ObservableObject
    {
        [ObservableProperty]
        private string? paymentMethod;

        [ObservableProperty]
        private decimal amount;

        [ObservableProperty]
        private decimal cashReceived;

        [ObservableProperty]
        private decimal change;
    }
}

#nullable restore