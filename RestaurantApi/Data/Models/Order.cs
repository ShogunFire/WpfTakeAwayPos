namespace RestaurantApi.Data.Models;

public class Order
{
    public Guid Id { get; set; }
    public Guid? ShiftId { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal Remaining { get; set; }
    public decimal TotalChange { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
