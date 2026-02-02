using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace RestaurantPOS.Models
{
    public partial class Order : ObservableObject
    {
        [ObservableProperty] private int orderId;
        [ObservableProperty] private decimal subtotal;
        [ObservableProperty] private decimal tax;
        [ObservableProperty] private decimal totalAmount;
        [ObservableProperty] private decimal totalPaid;
        [ObservableProperty] private decimal remaining;
        [ObservableProperty] private decimal totalChange;

        // Keep observable collection but manage subscriptions to recalc totals on changes
        private ObservableCollection<OrderLine> _orderLines = new ObservableCollection<OrderLine>();
        public ObservableCollection<OrderLine> OrderLines
        {
            get => _orderLines;
            set
            {
                if (_orderLines == value) return;
                if (_orderLines != null) UnsubscribeCollection(_orderLines);
                _orderLines = value ?? new ObservableCollection<OrderLine>();
                SubscribeCollection(_orderLines);
                CalculateTotal();
                OnPropertyChanged(nameof(OrderLines));
            }
        }


        public void AddMenuItem(MenuItem menuItem)
        {
            if (menuItem == null) return;

            var existing = OrderLines.FirstOrDefault(m => m.Item?.OriginalMenuItemId == menuItem.MenuItemGuid);
            if (existing != null)
            {
                existing.Quantity += 1;
            }
            else
            {
                var components = menuItem.Components == null
                    ? new List<MenuItemComponentSnapshot>()
                    : menuItem.Components
                        .Select(c => new MenuItemComponentSnapshot(c.InventoryItemId, c.QuantityUsed))
                        .ToList();

                var snapshot = new MenuItemSnapshot(
                    menuItem.MenuItemGuid,
                    menuItem.Name,
                    menuItem.Price,
                    components);

                OrderLines.Add(new OrderLine(snapshot, 1));
            }

            CalculateTotal();
        }

        public void RemoveMenuItem(OrderLine orderLine)
        {
            if (orderLine == null) return;

            var existing = OrderLines.FirstOrDefault(m => m.Item?.OriginalMenuItemId == orderLine.Item?.OriginalMenuItemId);
            if (existing != null)
            {
                UnsubscribeItem(existing);
                OrderLines.Remove(existing);
                CalculateTotal();
            }
        }

        private void CalculateTotal()
        {
            // Calculate total (price includes tax)
            TotalAmount = OrderLines.Sum(i => i.Item != null ? i.Item.Price * (i.Quantity > 0 ? i.Quantity : 1) : 0m);
            
            // Calculate subtotal (before tax) - tax is already included in the price
            // Assuming 8% tax rate: Price = Subtotal * 1.08, so Subtotal = Price / 1.08
            Subtotal = System.Math.Round(TotalAmount / 1.08m, 2);
            
            // Calculate tax (difference between total and subtotal)
            Tax = System.Math.Round(TotalAmount - Subtotal, 2);
            
            // Recalculate payment-related properties
            UpdatePaymentCalculations();
        }

        public void UpdatePaymentCalculations()
        {
            Remaining = TotalAmount - TotalPaid;
            
            // Calculate change: if total paid exceeds total amount, give change
            if (TotalPaid > TotalAmount)
            {
                TotalChange = TotalPaid - TotalAmount;
            }
            else
            {
                TotalChange = 0;
            }
        }

        private void SubscribeCollection(ObservableCollection<OrderLine> collection)
        {
            collection.CollectionChanged += MenuItems_CollectionChanged;
            foreach (var item in collection)
                SubscribeItem(item);
        }

        private void UnsubscribeCollection(ObservableCollection<OrderLine> collection)
        {
            collection.CollectionChanged -= MenuItems_CollectionChanged;
            foreach (var item in collection)
                UnsubscribeItem(item);
        }

        private void MenuItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
                foreach (OrderLine ni in e.NewItems)
                    SubscribeItem(ni);

            if (e.OldItems != null)
                foreach (OrderLine oi in e.OldItems)
                    UnsubscribeItem(oi);

            CalculateTotal();
        }

        private void SubscribeItem(OrderLine item)
        {
            if (item != null)
            {
                item.PropertyChanged += MenuItem_PropertyChanged;
                if (item.Item != null)
                    item.Item.PropertyChanged += MenuItem_PropertyChanged;
            }
        }

        private void UnsubscribeItem(OrderLine item)
        {
            if (item != null)
            {
                item.PropertyChanged -= MenuItem_PropertyChanged;
                if (item.Item != null)
                    item.Item.PropertyChanged -= MenuItem_PropertyChanged;
            }
        }

        private void MenuItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(OrderLine.Quantity) || e.PropertyName == nameof(MenuItemSnapshot.Price))
                CalculateTotal();
        }

       
    }
}