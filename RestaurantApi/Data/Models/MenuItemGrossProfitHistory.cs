namespace RestaurantApi.Data.Models;

public class MenuItemGrossProfitHistory
{
    public Guid Id { get; set; }
    public Guid MenuItemId { get; set; }
    public decimal Price { get; set; }
    public decimal UnitCost { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal GrossMargin { get; set; }
    public DateTime SnapshotDate { get; set; }
    public DateTime CreatedAt { get; set; }
}
