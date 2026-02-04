using Microsoft.Data.Sqlite;
using RestaurantPOS.Data;
using RestaurantPOS.Models;
using RestaurantPOS.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace RestaurantPOS.Services
{
    public class CashControlService : ICashControlService
    {
        private readonly IShiftService _shiftService;
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

        public CashControlService(IShiftService shiftService)
        {
            _shiftService = shiftService;
            LoadTransactions();
            if (_transactions.Count == 0)
            {
                AddOpeningFloat();
            }
        }

        public void RecordSale(decimal amount)
        {
            if (amount <= 0) return;
            InsertTransaction(new CashTransaction(CashTransactionType.Sale, amount));
        }

        public void RemoveCash(decimal amount, string reason)
        {
            if (amount <= 0) return;
            InsertTransaction(new CashTransaction(CashTransactionType.Removal, amount, reason));
        }

        public void AddCash(decimal amount, string reason)
        {
            if (amount <= 0) return;
            InsertTransaction(new CashTransaction(CashTransactionType.Addition, amount, reason));
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
            ClearTransactions();
            AddOpeningFloat();
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

        private void AddOpeningFloat()
        {
            InsertTransaction(new CashTransaction(CashTransactionType.OpeningFloat, OpeningFloat, "Shift Opening"));
        }

        private void LoadTransactions()
        {
            using var connection = SqliteDb.CreateConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT ShiftId, Timestamp, Type, Amount, Reason, Description FROM CashTransactions ORDER BY Timestamp";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var shiftId = reader.IsDBNull(0) ? (long?)null : reader.GetInt64(0);
                var timestampText = reader.GetString(1);
                var type = (CashTransactionType)reader.GetInt32(2);
                var amount = Convert.ToDecimal(reader.GetValue(3));
                var reason = reader.IsDBNull(4) ? null : reader.GetString(4);
                var description = reader.IsDBNull(5) ? null : reader.GetString(5);

                var transaction = new CashTransaction(type, amount, reason)
                {
                    ShiftId = shiftId,
                    Timestamp = DateTime.Parse(timestampText, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                    Description = description
                };

                _transactions.Add(transaction);
            }
        }

        private void InsertTransaction(CashTransaction transaction)
        {
            transaction.ShiftId = _shiftService.GetActiveShiftId();
            
            using var connection = SqliteDb.CreateConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"INSERT INTO CashTransactions (ShiftId, Timestamp, Type, Amount, Reason, Description)
                                VALUES (@ShiftId, @Timestamp, @Type, @Amount, @Reason, @Description);";
            cmd.Parameters.AddWithValue("@ShiftId", transaction.ShiftId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Timestamp", transaction.Timestamp.ToString("O"));
            cmd.Parameters.AddWithValue("@Type", (int)transaction.Type);
            cmd.Parameters.AddWithValue("@Amount", transaction.Amount);
            cmd.Parameters.AddWithValue("@Reason", transaction.Reason ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Description", transaction.Description ?? (object)DBNull.Value);
            cmd.ExecuteNonQuery();

            _transactions.Add(transaction);
        }

        private void ClearTransactions()
        {
            using var connection = SqliteDb.CreateConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM CashTransactions";
            cmd.ExecuteNonQuery();
        }
    }
}
