using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantPOS.Models;
using RestaurantPOS.Services;
using RestaurantPOS.Services.Interfaces;
using System;
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
        private readonly OrderService _orderService;
        private readonly PaymentService _paymentService;
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

        [ObservableProperty]
        private decimal exactRemaining;

        [ObservableProperty]
        private string exactRemainingDisplay = "Exact";

        [ObservableProperty]
        private decimal nextMultipleOf20;

        [ObservableProperty]
        private string nextMultipleOf20Display = "$20";

        [ObservableProperty]
        private decimal nextMultipleOf50;

        [ObservableProperty]
        private string nextMultipleOf50Display = "$50";

        [ObservableProperty]
        private decimal nextMultipleOf100;

        [ObservableProperty]
        private string nextMultipleOf100Display = "$100";

        [ObservableProperty]
        private decimal nextMultipleOf200;

        [ObservableProperty]
        private string nextMultipleOf200Display = "$200";

        [ObservableProperty]
        private decimal nextMultipleOf500;

        [ObservableProperty]
        private string nextMultipleOf500Display = "$500";

        public PaymentViewModel(INavigationService navigationService, IDialogService dialogService, IInventoryService inventoryService, ICashControlService cashControlService, OrderService orderService, PaymentService paymentService, IOrderSession orderSession)
        {
            _navigationService = navigationService;
            _dialogService = dialogService;
            _inventoryService = inventoryService;
            _cashControlService = cashControlService;
            _orderService = orderService;
            _paymentService = paymentService;
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
        private void SetPaymentAmount(decimal amount)
        {
            if (amount > 0)
            {
                InputDisplay = amount.ToString("F2");
            }
        }

        private void CalculateQuickPayAmounts()
        {
            var remaining = _orderSession.CurrentOrder.Remaining;
            if (remaining <= 0)
            {
                ExactRemaining = 0;
                ExactRemainingDisplay = "Paid";
                NextMultipleOf20 = 0;
                NextMultipleOf20Display = "$20";
                NextMultipleOf50 = 0;
                NextMultipleOf50Display = "$50";
                NextMultipleOf100 = 0;
                NextMultipleOf100Display = "$100";
                NextMultipleOf200 = 0;
                NextMultipleOf200Display = "$200";
                NextMultipleOf500 = 0;
                NextMultipleOf500Display = "$500";
                return;
            }

            ExactRemaining = remaining;
            ExactRemainingDisplay = "Exact";

            NextMultipleOf20 = RoundUpToMultiple(remaining, 20);
            NextMultipleOf20Display = $"${NextMultipleOf20:F2}";

            NextMultipleOf50 = RoundUpToMultiple(remaining, 50);
            NextMultipleOf50Display = $"${NextMultipleOf50:F2}";

            NextMultipleOf100 = RoundUpToMultiple(remaining, 100);
            NextMultipleOf100Display = $"${NextMultipleOf100:F2}";

            NextMultipleOf200 = RoundUpToMultiple(remaining, 200);
            NextMultipleOf200Display = $"${NextMultipleOf200:F2}";

            NextMultipleOf500 = RoundUpToMultiple(remaining, 500);
            NextMultipleOf500Display = $"${NextMultipleOf500:F2}";
        }

        private decimal RoundUpToMultiple(decimal value, decimal multiple)
        {
            return Math.Ceiling(value / multiple) * multiple;
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
            InputDisplay = "0";
            _navigationService.Navigate<OrderEntryViewModel>();
        }

        [RelayCommand(CanExecute = nameof(CanCompleteOrder))]
        private async void CompleteOrder()
        {
            if (_orderSession.CurrentOrder.Remaining <= 0)
            {
                // Consume inventory for each item in the order
                foreach (var line in _orderSession.CurrentOrder.OrderLines)
                {
                    var item = line.Item;
                    if (item == null || item.Components == null || item.Components.Count == 0)
                        continue;

                    foreach (var component in item.Components)
                    {
                        var totalUsed = component.QuantityUsed * line.Quantity;
                        _inventoryService.TryConsume(component.InventoryItemId, totalUsed, "Sale");
                    }
                }

                // Save the completed order
                _orderService.AddOrder(_orderSession.CurrentOrder);

                // Process each payment
                foreach (var paymentEntry in PaymentEntries)
                {
                    var payment = new Payment
                    {
                        PaymentGuid = Guid.NewGuid(),
                        OrderGuid = _orderSession.CurrentOrder.OrderGuid,
                        OrderId = _orderSession.CurrentOrder.OrderId,
                        Amount = paymentEntry.Amount,
                        PaymentMethod = paymentEntry.PaymentMethod ?? "Unknown"
                    };

                    _paymentService.ProcessPayment(payment);
                }

                // Record cash payments to cash control
                var cashPayments = PaymentEntries.Where(p => p.PaymentMethod == "Cash").Sum(p => p.Amount);
                var netCash = Math.Max(0m, cashPayments - _orderSession.CurrentOrder.TotalChange);
                if (netCash > 0)
                {
                    _cashControlService.RecordSale(netCash);
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

            // Update quick pay amount buttons
            CalculateQuickPayAmounts();
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