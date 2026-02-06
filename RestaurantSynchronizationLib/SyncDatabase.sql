-- RestaurantPOS SQLite Database - Event Synchronization Schema
-- This script sets up the tables needed for event synchronization
-- WARNING: Assumes RestaurantPOS already has Order, Payment, and InventoryItem tables

-- Create SyncEvents table to track events that need to be synchronized to the API
CREATE TABLE IF NOT EXISTS SyncEvents (
    Id BLOB PRIMARY KEY,         -- GUID stored as binary data (16 bytes)
    Type TEXT NOT NULL,          -- Event type: order_created, payment_processed, inventory_item_updated, etc.
    Payload TEXT,                -- JSON payload containing event data
    CreatedAt DATETIME NOT NULL, -- When the event was created (UTC)
    SyncedAt DATETIME,           -- When the event was successfully synced to API (NULL if not synced)
    DeviceId TEXT                -- Identifier of the device that created the event
);

-- Index for fast queries of unsynced events
CREATE INDEX IF NOT EXISTS idx_SyncEvents_SyncedAt 
ON SyncEvents(SyncedAt);

-- Index for fast queries by event type
CREATE INDEX IF NOT EXISTS idx_SyncEvents_Type 
ON SyncEvents(Type);

-- Index for finding events created in a date range
CREATE INDEX IF NOT EXISTS idx_SyncEvents_CreatedAt 
ON SyncEvents(CreatedAt);

-- Optional: Create SyncEventLog table for auditing sync attempts
CREATE TABLE IF NOT EXISTS SyncEventLogs (
    Id BLOB PRIMARY KEY,         -- GUID stored as binary data (16 bytes)
    EventId BLOB NOT NULL,       -- GUID reference to SyncEvents
    AttemptNumber INTEGER NOT NULL,
    Timestamp DATETIME NOT NULL,
    Success BOOLEAN NOT NULL,
    AlreadyProcessed BOOLEAN,
    ErrorMessage TEXT,
    FOREIGN KEY(EventId) REFERENCES SyncEvents(Id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_SyncEventLogs_EventId 
ON SyncEventLogs(EventId);

CREATE INDEX IF NOT EXISTS idx_SyncEventLogs_Timestamp 
ON SyncEventLogs(Timestamp);

-- View to show sync status
CREATE VIEW IF NOT EXISTS v_SyncStatus AS
SELECT 
    (SELECT COUNT(*) FROM SyncEvents WHERE SyncedAt IS NULL) as PendingEvents,
    (SELECT COUNT(*) FROM SyncEvents WHERE SyncedAt IS NOT NULL) as SyncedEvents,
    (SELECT COUNT(*) FROM SyncEvents) as TotalEvents,
    (SELECT MAX(CreatedAt) FROM SyncEvents) as LastEventCreated,
    (SELECT MAX(SyncedAt) FROM SyncEvents) as LastEventSynced;

-- View to show events pending sync
CREATE VIEW IF NOT EXISTS v_PendingEvents AS
SELECT 
    Id,
    Type,
    CreatedAt,
    DeviceId,
    DATETIME('now') - CreatedAt as PendingDuration
FROM SyncEvents
WHERE SyncedAt IS NULL
ORDER BY CreatedAt ASC;

-- View to show recent sync activity
CREATE VIEW IF NOT EXISTS v_RecentActivity AS
SELECT 
    EventId,
    Type,
    CreatedAt,
    SyncedAt,
    CAST((CAST((julianday(SyncedAt) - julianday(CreatedAt)) * 24 * 60 * 60 AS INTEGER)) AS TEXT) || ' seconds' as SyncDuration,
    DeviceId
FROM SyncEvents
WHERE SyncedAt IS NOT NULL
ORDER BY SyncedAt DESC
LIMIT 100;

-- Function to create a sync event (call this after creating Order, Payment, or InventoryItem)
-- Usage in C#:
--   var eventId = Guid.NewGuid().ToString();
--   var payload = JsonSerializer.Serialize(new { OrderId = order.Id, Amount = order.Total });
--   INSERT INTO SyncEvents (Id, Type, Payload, CreatedAt, DeviceId)
--   VALUES (eventId, 'order_created', payload, DateTime.UtcNow, 'POS-01');

-- Maintenance: Delete old synced events (older than 30 days)
-- Run periodically:
-- DELETE FROM SyncEvents 
-- WHERE SyncedAt IS NOT NULL 
-- AND SyncedAt < DATETIME('now', '-30 days');

-- Maintenance: Check for failed syncs
-- SELECT Id, Type, CreatedAt, COUNT(*) as FailureCount
-- FROM SyncEventLogs
-- WHERE Success = 0
-- GROUP BY EventId
-- HAVING COUNT(*) > 3
-- ORDER BY CreatedAt DESC;
