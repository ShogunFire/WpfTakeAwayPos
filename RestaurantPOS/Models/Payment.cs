using System;

public class Payment
{
    public Guid PaymentGuid { get; set; } = Guid.NewGuid();
    public Guid OrderGuid { get; set; }
    public int PaymentId { get; set; }
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; }

    public bool ProcessPayment()
    {
        // Logic to process the payment
        // This could involve calling a payment gateway or updating the order status
        return true; // Return true if payment is successful
    }
}