namespace RestaurantApi.Data.Models;

public class Order
{
    public Guid Id { get; set; }
    public Guid? ShiftId { get; set; }
    public Guid LocationId { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal Remaining { get; set; }
    public decimal TotalChange { get; set; }
    public decimal TotalCOGS { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal ProfitMargin { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
