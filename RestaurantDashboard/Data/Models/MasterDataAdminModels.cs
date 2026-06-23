namespace RestaurantDashboard.Data.Models;

public class MasterCategory
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class MasterInventoryItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal CurrentUnitCost { get; set; }
}

public class MasterMenuItem
{
    public Guid Id { get; set; }
    public Guid IdCategory { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
}

public class MasterMenuItemComponent
{
    public Guid Id { get; set; }
    public Guid MenuItemId { get; set; }
    public Guid InventoryItemId { get; set; }
    public decimal Quantity { get; set; }
}

public class MasterMenuItemComponentView
{
    public Guid Id { get; set; }
    public Guid MenuItemId { get; set; }
    public string MenuItemName { get; set; } = string.Empty;
    public Guid InventoryItemId { get; set; }
    public string InventoryItemName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
}
