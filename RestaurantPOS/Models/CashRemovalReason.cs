namespace RestaurantPOS.Models
{
    public class CashRemovalReason
    {
        public string Reason { get; }
        public string Description { get; }

        public CashRemovalReason(string reason, string description)
        {
            Reason = reason;
            Description = description;
        }
    }
}
