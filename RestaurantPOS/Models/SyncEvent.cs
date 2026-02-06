using System;

namespace RestaurantPOS.Models
{
    public class SyncEvent
    {
        public Guid Id { get; set; }
        public string Type { get; set; }
        public string Payload { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? SyncedAt { get; set; }
        public string DeviceId { get; set; }
    }
}
