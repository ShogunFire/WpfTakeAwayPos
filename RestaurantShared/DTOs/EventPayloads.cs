namespace RestaurantShared.DTOs;

public class ShiftStartedPayload
{
    public Guid ShiftId { get; set; }
    public DateTime StartDateTime { get; set; }
    public decimal OpeningCash { get; set; }
}

public class ShiftEndedPayload
{
    public Guid ShiftId { get; set; }
    public DateTime? EndDateTime { get; set; }
    public decimal? DeclaredCash { get; set; }
    public decimal? ExpectedCash { get; set; }
    public decimal? Difference { get; set; }
    public string? Notes { get; set; }
}

public class PaymentPayload
{
    public Guid? PaymentId { get; set; }
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public string? PaymentMethod { get; set; }
}

public class OrderPayload
{
    public Guid? OrderId { get; set; }
    public Guid? ShiftId { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal Remaining { get; set; }
    public decimal TotalChange { get; set; }
    public List<OrderLinePayload>? OrderLines { get; set; }
}

public class OrderLinePayload
{
    public Guid? MenuItemId { get; set; }
    public string? MenuItemName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public class CashTransactionPayload
{
    public Guid? TransactionGuid { get; set; }
    public Guid? ShiftId { get; set; }
    public string? Type { get; set; }
    public decimal Amount { get; set; }
    public string? Reason { get; set; }
    public string? Description { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsExpense { get; set; }
    public bool IsInventoryAdd { get; set; }
}

public class InventoryItemPayload
{
    public Guid? InventoryItemId { get; set; }
    public Guid? ShiftId { get; set; }
    public string? Name { get; set; }
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
    public string? Reason { get; set; }
    public decimal? TotalCost { get; set; }
    public bool PaidWithCash { get; set; }
}