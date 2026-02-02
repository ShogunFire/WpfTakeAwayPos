using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;

namespace RestaurantPOS.Models
{
    public partial class MenuItem : ObservableObject
    {
        [ObservableProperty] private int menuItemId;
        [ObservableProperty] private Guid menuItemGuid = Guid.NewGuid();
        [ObservableProperty] private int categoryId;
        [ObservableProperty] private string name;
        [ObservableProperty] private decimal price;
        [ObservableProperty] private List<MenuItemComponent> components = new();


        public MenuItem(int menuItemId, int categoryId, string name, decimal price, List<MenuItemComponent> components = null)
        {
            MenuItemId = menuItemId;
            CategoryId = categoryId;
            Name = name;
            Price = price;
            Components = components ?? new List<MenuItemComponent>();
        }

        public override string ToString() => $"{Name}: ${Price:F2}";
    }
}