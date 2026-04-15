namespace RestaurantApi.Data.Models;

public class CashTransaction
{
    public Guid Id { get; set; }
    public Guid? ShiftId { get; set; }
    public Guid LocationId { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Reason { get; set; }
    public string? Description { get; set; }
    public DateTime OccurredAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
