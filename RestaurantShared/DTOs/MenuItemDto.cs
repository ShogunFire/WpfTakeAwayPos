using System;

namespace RestaurantShared.DTOs;

public class MenuItemDto
{
    public Guid Id { get; set; }
    public Guid IdCategory { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
}
