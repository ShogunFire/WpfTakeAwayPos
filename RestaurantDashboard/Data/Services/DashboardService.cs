using RestaurantDashboard.Data.Models;

namespace RestaurantDashboard.Data.Services;

public class DashboardService
{
    private readonly List<Order> _orders;
    // Items with weighted popularity (higher weight = more popular)
    private readonly (string name, int weight)[] _menuItems = new[]
    {
        ("Sushi Deluxe", 25),
        ("Chicken Katsu", 30),
        ("Tempura Roll", 20),
        ("Gyoza", 15),
        ("Miso Soup", 12),
        ("Ramen Tonkotsu", 28),
        ("Edamame", 8),
        ("California Roll", 18),
        ("Salmon Sashimi", 22),
        ("Teriyaki Chicken", 16)
    };
    private readonly string[] _restaurants = new[] { "restaurant1", "restaurant2", "restaurant3" };

    public DashboardService()
    {
        _orders = GenerateMockOrders();
    }

    private List<Order> GenerateMockOrders()
    {
        var orders = new List<Order>();
        var random = new Random(42);
        var today = DateTime.Now.Date;
        var startDate = today.AddMonths(-2);

        var restaurantNames = new Dictionary<string, string>
        {
            { "restaurant1", "Restaurant 1" },
            { "restaurant2", "Restaurant 2" },
            { "restaurant3", "Restaurant 3" }
        };

        // Calculate total weight for weighted random selection
        var totalWeight = _menuItems.Sum(m => m.weight);

        var orderId = 1;
        for (var date = startDate; date <= today; date = date.AddDays(1))
        {
            foreach (var restaurantId in _restaurants)
            {
                // Random number of orders per day per restaurant (5-20)
                var ordersPerDay = random.Next(5, 21);
                for (int i = 0; i < ordersPerDay; i++)
                {
                    var amount = Math.Round((decimal)(random.Next(1500, 5000) / 100.0), 2);
                    var item = GetWeightedRandomItem(random, totalWeight);
                    var time = date.AddHours(random.Next(11, 22)).AddMinutes(random.Next(0, 60));

                    orders.Add(new Order
                    {
                        Id = orderId++,
                        RestaurantId = restaurantId,
                        RestaurantName = restaurantNames[restaurantId],
                        OrderDate = time,
                        Amount = amount,
                        ItemName = item
                    });
                }
            }
        }

        return orders;
    }

    public DashboardMetrics GetMetrics(DateTime startDate, DateTime endDate, string? restaurantId)
    {
        var filtered = FilterOrders(startDate, endDate, restaurantId);
        
        var totalSales = filtered.Sum(o => o.Amount);
        var totalOrders = filtered.Count;
        var averageCheck = totalOrders > 0 ? totalSales / totalOrders : 0;

        return new DashboardMetrics
        {
            TotalSales = totalSales,
            TotalOrders = totalOrders,
            AverageCheck = Math.Round(averageCheck, 2)
        };
    }

    public List<SalesDataPoint> GetSalesOverTime(DateTime startDate, DateTime endDate, string? restaurantId)
    {
        var filtered = FilterOrders(startDate, endDate, restaurantId);
        
        var salesByDay = filtered
            .GroupBy(o => o.OrderDate.Date)
            .OrderBy(g => g.Key)
            .Select(g => new SalesDataPoint
            {
                Date = g.Key.ToString("MMM dd"),
                Sales = g.Sum(o => o.Amount)
            })
            .ToList();

        return salesByDay;
    }

    public List<TopItem> GetTopSellingItems(DateTime startDate, DateTime endDate, string? restaurantId)
    {
        var filtered = FilterOrders(startDate, endDate, restaurantId);
        var totalOrders = filtered.Count;

        var topItems = filtered
            .GroupBy(o => o.ItemName)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => new TopItem
            {
                Name = g.Key,
                Quantity = g.Count(),
                Percentage = totalOrders > 0 ? (int)Math.Round((double)g.Count() / totalOrders * 100) : 0
            })
            .ToList();

        return topItems;
    }

    private string GetWeightedRandomItem(Random random, int totalWeight)
    {
        var randomValue = random.Next(0, totalWeight);
        var cumulativeWeight = 0;

        foreach (var (name, weight) in _menuItems)
        {
            cumulativeWeight += weight;
            if (randomValue < cumulativeWeight)
            {
                return name;
            }
        }

        return _menuItems[0].name;
    }

    private List<Order> FilterOrders(DateTime startDate, DateTime endDate, string? restaurantId)
    {
        var filtered = _orders
            .Where(o => o.OrderDate.Date >= startDate && o.OrderDate.Date <= endDate);

        if (!string.IsNullOrEmpty(restaurantId))
        {
            filtered = filtered.Where(o => o.RestaurantId == restaurantId);
        }

        return filtered.ToList();
    }
}

