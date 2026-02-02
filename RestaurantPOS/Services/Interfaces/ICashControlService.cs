using RestaurantPOS.Models;
using System;
using System.Collections.Generic;

namespace RestaurantPOS.Services.Interfaces
{
    public interface ICashControlService
    {
        decimal OpeningFloat { get; }
        decimal ExpectedCash { get; }
        decimal ActualCash { get; set; }
        decimal Difference { get; }
        bool IsCounted { get; }
        
        void RecordSale(decimal amount);
        void RemoveCash(decimal amount, string reason);
        void AddCash(decimal amount, string reason);
        void SetActualCash(decimal amount);
        void ResetShift();
        
        IEnumerable<CashTransaction> GetTransactions();
        IEnumerable<CashTransaction> GetTransactionsByDate(DateTime date);
    }
}
