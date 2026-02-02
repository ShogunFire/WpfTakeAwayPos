using RestaurantDashboard.Components.Pages;

public class FakeDataService
{
    public List<Restaurant> Restaurants { get; } =
    [
        new() { Id = 1, Name = "Downtown" },
        new() { Id = 2, Name = "Airport" }
    ];

    public List<MenuCategory> Categories { get; } =
    [
        new() { Id = 1, Name = "Burgers" },
        new() { Id = 2, Name = "Pizzas" },
        new() { Id = 3, Name = "Beverages" }
    ];

    public List<MenuItem> MenuItems { get; } =
    [
        new() { Id = 1, Name = "Cheeseburger", Price = 8.50m, CategoryId = 1 },
        new() { Id = 2, Name = "Pepperoni Pizza", Price = 12.00m, CategoryId = 2 },
        new() { Id = 3, Name = "Cola", Price = 2.50m, CategoryId = 3 }
    ];

    public List<Order> Orders { get; } =
    [
        new() { Id = 1001, OrderDate = DateTime.Today, Amount = 25.00m },
        new() { Id = 1002, OrderDate = DateTime.Today, Amount = 18.50m }
    ];
}