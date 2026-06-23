using Microsoft.Data.Sqlite;
using RestaurantPOS.Data;
using RestaurantPOS.Models;
using RestaurantPOS.Services.Interfaces;
using RestaurantShared.DTOs;
using RestaurantPOS.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace RestaurantPOS.Services
{
    public class CashControlService : ICashControlService
    {
        private readonly IShiftService _shiftService;
        private readonly ISyncEventService _syncEventService;
            private readonly PosSettings _settings;
        private readonly List<CashTransaction> _transactions = new();
        private decimal _actualCash;
        private bool _isCounted;

        public decimal OpeningFloat { get; private set; } = 200m; // Default opening float
        
        public decimal ExpectedCash
        {
            get
            {
                var currentShiftTransactions = GetActiveShiftTransactions();
                return OpeningFloat + currentShiftTransactions
                    .Where(t => t.Type == CashTransactionType.Sale || t.Type == CashTransactionType.Addition)
                    .Sum(t => t.Amount)
                    - currentShiftTransactions
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

    public CashControlService(IShiftService shiftService, ISyncEventService syncEventService, PosSettings settings)
        {
            _shiftService = shiftService;
            _syncEventService = syncEventService;
                        _settings = settings;
            LoadTransactions();
            EnsureOpeningFloatForActiveShift();
        }

        public void RecordSale(decimal amount)
        {
            if (amount <= 0) return;
            InsertTransaction(new CashTransaction(CashTransactionType.Sale, amount));
        }

        public void RemoveCash(decimal amount, string reason, bool isExpense = false, Guid? relatedInventoryCostRecordId = null)
        {
            if (amount <= 0) return;
            InsertTransaction(new CashTransaction(CashTransactionType.Removal, amount, reason, isExpense, relatedInventoryCostRecordId));
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
            _actualCash = 0;
            _isCounted = false;
            SyncOpeningFloatFromShift();
            EnsureOpeningFloatForActiveShift();
        }

        public IEnumerable<CashTransaction> GetTransactions()
        {
            return GetActiveShiftTransactions()
                .OrderByDescending(t => t.Timestamp);
        }

        public IEnumerable<CashTransaction> GetTransactionsByDate(DateTime date)
        {
            return _transactions
                .Where(t => t.Timestamp.Date == date.Date)
                .OrderByDescending(t => t.Timestamp);
        }

        public IEnumerable<CashTransaction> GetTransactionsByShift(long shiftId)
        {
            return _transactions
                .Where(t => t.ShiftId == shiftId)
                .OrderByDescending(t => t.Timestamp);
        }

        private IEnumerable<CashTransaction> GetActiveShiftTransactions()
        {
            var activeShiftId = _shiftService.GetActiveShiftId();
            if (activeShiftId <= 0)
            {
                return Enumerable.Empty<CashTransaction>();
            }

            return _transactions.Where(t => t.ShiftId == activeShiftId);
        }

        private void EnsureOpeningFloatForActiveShift()
        {
            var activeShiftId = _shiftService.GetActiveShiftId();
            if (activeShiftId <= 0)
            {
                return;
            }

            SyncOpeningFloatFromShift();

            var hasOpeningFloat = _transactions.Any(t => t.ShiftId == activeShiftId && t.Type == CashTransactionType.OpeningFloat);
            if (!hasOpeningFloat)
            {
                AddOpeningFloat();
            }
        }

        private void SyncOpeningFloatFromShift()
        {
            var activeShift = _shiftService.GetActiveShift();
            if (activeShift != null && activeShift.OpeningCash > 0)
            {
                OpeningFloat = activeShift.OpeningCash;
            }
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
            cmd.CommandText = "SELECT TransactionGuid, ShiftId, Timestamp, Type, Amount, Reason, Description, RelatedInventoryCostRecordId FROM CashTransactions ORDER BY Timestamp";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var transactionGuid = reader.IsDBNull(0) ? Guid.NewGuid() : Guid.Parse(reader.GetString(0));
                var shiftId = reader.IsDBNull(1) ? (long?)null : reader.GetInt64(1);
                var timestampText = reader.GetString(2);
                var type = (CashTransactionType)reader.GetInt32(3);
                var amount = Convert.ToDecimal(reader.GetValue(4));
                var reason = reader.IsDBNull(5) ? null : reader.GetString(5);
                var description = reader.IsDBNull(6) ? null : reader.GetString(6);
                var relatedInventoryCostRecordId = reader.IsDBNull(7) ? (Guid?)null : Guid.Parse(reader.GetString(7));

                var transaction = new CashTransaction(type, amount, reason)
                {
                    TransactionGuid = transactionGuid,
                    ShiftId = shiftId,
                    Timestamp = DateTime.Parse(timestampText, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                    Description = description,
                    RelatedInventoryCostRecordId = relatedInventoryCostRecordId
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
            cmd.CommandText = @"INSERT INTO CashTransactions (TransactionGuid, ShiftId, Timestamp, Type, Amount, Reason, Description, RelatedInventoryCostRecordId)
                                VALUES (@TransactionGuid, @ShiftId, @Timestamp, @Type, @Amount, @Reason, @Description, @RelatedInventoryCostRecordId);";
            cmd.Parameters.AddWithValue("@TransactionGuid", transaction.TransactionGuid.ToString());
            cmd.Parameters.AddWithValue("@ShiftId", transaction.ShiftId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Timestamp", transaction.Timestamp.ToString("O"));
            cmd.Parameters.AddWithValue("@Type", (int)transaction.Type);
            cmd.Parameters.AddWithValue("@Amount", transaction.Amount);
            cmd.Parameters.AddWithValue("@Reason", transaction.Reason ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Description", transaction.Description ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@RelatedInventoryCostRecordId", transaction.RelatedInventoryCostRecordId?.ToString() ?? (object)DBNull.Value);
            cmd.ExecuteNonQuery();

            _transactions.Add(transaction);

            _syncEventService.CreateEvent(EventTypes.CashTransactionCreated, new CashTransactionPayload
            {
                TransactionGuid = transaction.TransactionGuid,
                ShiftId = null,
                Type = transaction.Type.ToString(),
                Amount = transaction.Amount,
                Reason = transaction.Reason,
                Description = transaction.Description,
                Timestamp = transaction.Timestamp,
                IsExpense = transaction.IsExpense,
                RelatedInventoryCostRecordId = transaction.RelatedInventoryCostRecordId
            });
        }

        // NOTE: We keep historical transactions per shift. No global delete.
    }
}
