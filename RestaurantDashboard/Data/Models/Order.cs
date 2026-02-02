public class Order
{
    public int Id { get; set; }
    public string RestaurantId { get; set; } = "";
    public string RestaurantName { get; set; } = "";
    public DateTime OrderDate { get; set; }
    public decimal Amount { get; set; }
    public string ItemName { get; set; } = "";
}