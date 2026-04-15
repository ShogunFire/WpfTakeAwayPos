namespace RestaurantApi.Data.Models;

public class Expense
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ExpenseCategoryId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime ExpenseDate { get; set; }
    public Guid LocationId { get; set; }
    public Guid? ShiftId { get; set; }
    
    // Optional links to source records
    public Guid? InventoryCostRecordId { get; set; }
    public Guid? CashTransactionId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    
    // Navigation properties
    public ExpenseCategory? ExpenseCategory { get; set; }
}
