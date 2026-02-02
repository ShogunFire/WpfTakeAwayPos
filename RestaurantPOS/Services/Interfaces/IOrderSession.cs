using RestaurantPOS.Models;
using System;
using System.ComponentModel;

#nullable enable

namespace RestaurantPOS.Services.Interfaces
{
    /// <summary>
    /// Manages the current order session throughout the application lifecycle.
    /// Ensures there is always a single "current order" being worked on.
    /// </summary>
    public interface IOrderSession :INotifyPropertyChanged
    {
        /// <summary>
        /// Gets the current active order.
        /// </summary>
        Order CurrentOrder { get; }

        /// <summary>
        /// Raised when the order's lines collection changes.
        /// </summary>
        event EventHandler? OrderLinesChanged;

        /// <summary>
        /// Starts a new order, replacing any existing current order.
        /// </summary>
        void StartNew();

        /// <summary>
        /// Completes the current order and starts a new one.
        /// </summary>
        void Complete();

        /// <summary>
        /// Cancels the current order and starts a new one.
        /// </summary>
        void Cancel();
    }
}

#nullable restore
