namespace RestaurantShared.DTOs;

/// <summary>
/// Event types supported by the system
/// </summary>
public static class EventTypes
{
    public const string InventoryItemAdded = "inventory_item_added";
    public const string InventoryItemRemoved = "inventory_item_removed";
    public const string OrderCompleted = "order_completed";
    public const string PaymentProcessed = "payment_processed";
    public const string CashTransactionCreated = "cash_transaction_created";
    public const string ShiftStarted = "shift_started";
    public const string ShiftEnded = "shift_ended";
}
