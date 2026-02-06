using System;

namespace RestaurantPOS.Configuration;

public class PosSettings
{
    public Uri ApiBaseAddress { get; set; } = new Uri("https://localhost:7106/");
    public Guid LocationId { get; set; } = Guid.Empty;
}
