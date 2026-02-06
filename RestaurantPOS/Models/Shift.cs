using System;

namespace RestaurantPOS.Models
{
    public class Shift
    {
        public long ShiftId { get; set; }
        public Guid ShiftGuid { get; set; } = Guid.NewGuid();
        public DateTime StartDateTime { get; set; }
        public DateTime? EndDateTime { get; set; }
        public decimal OpeningCash { get; set; }
        public decimal? DeclaredCash { get; set; }
        public decimal? ExpectedCash { get; set; }
        public decimal? Difference { get; set; }
        public bool IsActive { get; set; }
        public string? UserId { get; set; }
        public string? Notes { get; set; }

        public Shift()
        {
            StartDateTime = DateTime.Now;
            IsActive = true;
        }

        public Shift(decimal openingCash)
        {
            StartDateTime = DateTime.Now;
            OpeningCash = openingCash;
            IsActive = true;
        }
    }
}
