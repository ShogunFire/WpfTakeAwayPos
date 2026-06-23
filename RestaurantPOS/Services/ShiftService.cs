using Microsoft.Data.Sqlite;
using RestaurantPOS.Configuration;
using RestaurantPOS.Data;
using RestaurantPOS.Models;
using RestaurantPOS.Services.Interfaces;
using RestaurantShared.DTOs;
using System;
using System.Globalization;

namespace RestaurantPOS.Services
{
    public class ShiftService : IShiftService
    {
        private Shift _activeShift;
        private readonly ISyncEventService _syncEventService;
        private readonly PosSettings _settings;

        public ShiftService(ISyncEventService syncEventService, PosSettings settings)
        {
            _syncEventService = syncEventService;
            _settings = settings;
            _activeShift = LoadActiveShift();
        }

        public Shift GetActiveShift()
        {
            return _activeShift;
        }

        public long GetActiveShiftId()
        {
            return _activeShift?.ShiftId ?? 0;
        }

        public Shift StartNewShift(decimal openingCash)
        {
            // End any active shift before starting a new one
            if (_activeShift != null && _activeShift.IsActive)
            {
                EndShift(0, 0, "Auto-closed for new shift");
            }

            var shift = new Shift(openingCash);

            using var connection = SqliteDb.CreateConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"INSERT INTO Shifts (ShiftGuid, StartDateTime, OpeningCash, IsActive)
                                VALUES (@ShiftGuid, @StartDateTime, @OpeningCash, @IsActive);
                                SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("@ShiftGuid", shift.ShiftGuid.ToString());
            cmd.Parameters.AddWithValue("@StartDateTime", shift.StartDateTime.ToString("O"));
            cmd.Parameters.AddWithValue("@OpeningCash", shift.OpeningCash);
            cmd.Parameters.AddWithValue("@IsActive", 1);
            shift.ShiftId = (long)cmd.ExecuteScalar();

            _activeShift = shift;

            // Create shift started event
            _syncEventService.CreateEvent(EventTypes.ShiftStarted, new ShiftStartedPayload
            {
                ShiftId = shift.ShiftGuid,
                StartDateTime = shift.StartDateTime,
                OpeningCash = shift.OpeningCash
            });

            return shift;
        }

        public Shift EndShift(decimal declaredCash, decimal expectedCash, string notes = null)
        {
            if (_activeShift == null || !_activeShift.IsActive)
                return null;

            _activeShift.EndDateTime = DateTime.Now;
            _activeShift.DeclaredCash = declaredCash;
            _activeShift.ExpectedCash = expectedCash;
            _activeShift.Difference = declaredCash - expectedCash;
            _activeShift.Notes = notes;
            _activeShift.IsActive = false;

            using var connection = SqliteDb.CreateConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"UPDATE Shifts
                                SET EndDateTime = @EndDateTime,
                                    DeclaredCash = @DeclaredCash,
                                    ExpectedCash = @ExpectedCash,
                                    Difference = @Difference,
                                    IsActive = @IsActive,
                                    Notes = @Notes
                                WHERE ShiftId = @ShiftId;";
            cmd.Parameters.AddWithValue("@EndDateTime", _activeShift.EndDateTime.Value.ToString("O"));
            cmd.Parameters.AddWithValue("@DeclaredCash", _activeShift.DeclaredCash.Value);
            cmd.Parameters.AddWithValue("@ExpectedCash", _activeShift.ExpectedCash.Value);
            cmd.Parameters.AddWithValue("@Difference", _activeShift.Difference.Value);
            cmd.Parameters.AddWithValue("@IsActive", 0);
            cmd.Parameters.AddWithValue("@Notes", notes ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ShiftId", _activeShift.ShiftId);
            cmd.ExecuteNonQuery();

            // Create shift ended event
            _syncEventService.CreateEvent(EventTypes.ShiftEnded, new ShiftEndedPayload
            {
                ShiftId = _activeShift.ShiftGuid,
                EndDateTime = _activeShift.EndDateTime,
                DeclaredCash = _activeShift.DeclaredCash,
                ExpectedCash = _activeShift.ExpectedCash,
                Difference = _activeShift.Difference,
                Notes = _activeShift.Notes
            });

            return _activeShift;
        }

        private Shift LoadActiveShift()
        {
            using var connection = SqliteDb.CreateConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"SELECT ShiftId, ShiftGuid, StartDateTime, EndDateTime, OpeningCash, DeclaredCash, 
                                       ExpectedCash, Difference, IsActive, UserId, Notes 
                                FROM Shifts 
                                WHERE IsActive = 1 
                                LIMIT 1";
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new Shift
                {
                    ShiftId = reader.GetInt64(0),
                    ShiftGuid = reader.IsDBNull(1) ? Guid.NewGuid() : Guid.Parse(reader.GetString(1)),
                    StartDateTime = DateTime.Parse(reader.GetString(2), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                    EndDateTime = reader.IsDBNull(3) ? null : DateTime.Parse(reader.GetString(3), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                    OpeningCash = Convert.ToDecimal(reader.GetValue(4)),
                    DeclaredCash = reader.IsDBNull(5) ? null : Convert.ToDecimal(reader.GetValue(5)),
                    ExpectedCash = reader.IsDBNull(6) ? null : Convert.ToDecimal(reader.GetValue(6)),
                    Difference = reader.IsDBNull(7) ? null : Convert.ToDecimal(reader.GetValue(7)),
                    IsActive = reader.GetInt32(8) == 1,
                    UserId = reader.IsDBNull(9) ? null : reader.GetString(9),
                    Notes = reader.IsDBNull(10) ? null : reader.GetString(10)
                };
            }

            return null;
        }
    }
}
