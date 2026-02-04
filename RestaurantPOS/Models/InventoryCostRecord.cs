using System;

namespace RestaurantPOS.Models
{
    /// <summary>
    /// Represents a single inventory purchase/delivery record
    /// </summary>
    public class InventoryCostRecord
    {
        public Guid Id { get; set; }
        public long? ShiftId { get; set; }
        public Guid InventoryItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public decimal QuantityReceived { get; set; }
        public decimal TotalCost { get; set; }
        public decimal UnitCost => QuantityReceived > 0 ? TotalCost / QuantityReceived : 0;
        public DateTime RecordedDate { get; set; }
        public string? Notes { get; set; }

        public InventoryCostRecord()
        {
            Id = Guid.NewGuid();
            RecordedDate = DateTime.Now;
        }
    }
}
