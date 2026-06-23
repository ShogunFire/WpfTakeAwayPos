using System;

namespace RestaurantPOS.Models
{
    public class CashTransaction
    {
        public Guid TransactionGuid { get; set; }
        public long? ShiftId { get; set; }
        public DateTime Timestamp { get; set; }
        public CashTransactionType Type { get; set; }
        public decimal Amount { get; set; }
        public string Reason { get; set; }
        public string Description { get; set; }
        public bool IsExpense { get; set; }

        public CashTransaction(CashTransactionType type, decimal amount, string reason = null, bool isExpense = false)
        {
            TransactionGuid = Guid.NewGuid();
            Timestamp = DateTime.Now;
            Type = type;
            Amount = amount;
            Reason = reason;
            IsExpense = isExpense;
            Description = GenerateDescription();
        }

        private string GenerateDescription()
        {
            return Type switch
            {
                CashTransactionType.Sale => $"Cash Sale",
                CashTransactionType.Removal => $"Cash Removal: {Reason}",
                CashTransactionType.Addition => $"Cash Addition: {Reason}",
                CashTransactionType.OpeningFloat => "Opening Float",
                _ => "Unknown Transaction"
            };
        }
    }

    public enum CashTransactionType
    {
        Sale,
        Removal,
        Addition,
        OpeningFloat
    }
}
