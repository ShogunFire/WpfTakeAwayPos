using CommunityToolkit.Mvvm.ComponentModel;

namespace RestaurantPOS.Models
{
    public partial class OrderLine : ObservableObject
    {
        [ObservableProperty] private MenuItemSnapshot item;
        [ObservableProperty] private int quantity = 1;

        public OrderLine() { }

        public OrderLine(MenuItemSnapshot item, int quantity = 1)
        {
            Item = item;
            Quantity = quantity;
        }
    }
}
