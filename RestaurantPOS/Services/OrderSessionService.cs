using CommunityToolkit.Mvvm.ComponentModel;
using RestaurantPOS.Models;
using RestaurantPOS.Services.Interfaces;
using System;
using System.Collections.Specialized;

#nullable enable

namespace RestaurantPOS.Services
{
    /// <summary>
    /// Manages the current order session for the application.
    /// Ensures a single order is always active and provides lifecycle management.
    /// </summary>
    public partial class OrderSessionService : ObservableObject, IOrderSession
    {
        private Order _currentOrder;

        public Order CurrentOrder
        {
            get => _currentOrder;
            private set
            {
                if (_currentOrder != null)
                    _currentOrder.OrderLines.CollectionChanged -= OnOrderLinesChanged;

                _currentOrder = value;

                _currentOrder.OrderLines.CollectionChanged += OnOrderLinesChanged;
                OnPropertyChanged(nameof(CurrentOrder));
            }
        }

        public event EventHandler? OrderLinesChanged;

        public OrderSessionService()
        {
            _currentOrder = new Order();
            _currentOrder.OrderLines.CollectionChanged += OnOrderLinesChanged;
        }

        public void StartNew()
        {
            CurrentOrder = new Order();
        }

        public void Complete()
        {
            CurrentOrder = new Order();
        }

        public void Cancel()
        {
            CurrentOrder = new Order();
        }

        private void OnOrderLinesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OrderLinesChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}

#nullable restore
