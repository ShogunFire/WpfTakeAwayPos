using RestaurantPOS.Models;
using RestaurantPOS.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RestaurantPOS.Services
{
    public class CashControlService : ICashControlService
    {
        private readonly List<CashTransaction> _transactions = new();
        private decimal _actualCash;
        private bool _isCounted;

        public decimal OpeningFloat { get; private set; } = 200m; // Default opening float
        
        public decimal ExpectedCash
        {
            get
            {
                return OpeningFloat + _transactions
                    .Where(t => t.Type == CashTransactionType.Sale || t.Type == CashTransactionType.Addition)
                    .Sum(t => t.Amount)
                    - _transactions
                    .Where(t => t.Type == CashTransactionType.Removal)
                    .Sum(t => t.Amount);
            }
        }

        public decimal ActualCash
        {
            get => _actualCash;
            set
            {
                _actualCash = value;
                _isCounted = true;
            }
        }

        public decimal Difference => IsCounted ? ActualCash - ExpectedCash : 0m;

        public bool IsCounted => _isCounted;

        public CashControlService()
        {
            // Initialize with opening float transaction
            _transactions.Add(new CashTransaction(CashTransactionType.OpeningFloat, OpeningFloat, "Shift Opening"));
        }

        public void RecordSale(decimal amount)
        {
            if (amount <= 0) return;
            _transactions.Add(new CashTransaction(CashTransactionType.Sale, amount));
        }

        public void RemoveCash(decimal amount, string reason)
        {
            if (amount <= 0) return;
            _transactions.Add(new CashTransaction(CashTransactionType.Removal, amount, reason));
        }

        public void AddCash(decimal amount, string reason)
        {
            if (amount <= 0) return;
            _transactions.Add(new CashTransaction(CashTransactionType.Addition, amount, reason));
        }

        public void SetActualCash(decimal amount)
        {
            ActualCash = amount;
        }

        public void ResetShift()
        {
            _transactions.Clear();
            _actualCash = 0;
            _isCounted = false;
            _transactions.Add(new CashTransaction(CashTransactionType.OpeningFloat, OpeningFloat, "Shift Opening"));
        }

        public IEnumerable<CashTransaction> GetTransactions()
        {
            return _transactions.OrderByDescending(t => t.Timestamp);
        }

        public IEnumerable<CashTransaction> GetTransactionsByDate(DateTime date)
        {
            return _transactions
                .Where(t => t.Timestamp.Date == date.Date)
                .OrderByDescending(t => t.Timestamp);
        }
    }
}
