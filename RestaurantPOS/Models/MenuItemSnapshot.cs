using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;

namespace RestaurantPOS.Models
{
    public partial class MenuItemSnapshot : ObservableObject
    {
        [ObservableProperty] private Guid originalMenuItemId;
        [ObservableProperty] private string name;
        [ObservableProperty] private decimal price;
        [ObservableProperty] private List<MenuItemComponentSnapshot> components = new();

       

        public MenuItemSnapshot(Guid originalMenuItemId, string name, decimal price, List<MenuItemComponentSnapshot> components = null)
        {
            OriginalMenuItemId = originalMenuItemId;
            Name = name;
            Price = price;
            Components = components ?? new List<MenuItemComponentSnapshot>();
        }
    }
}
