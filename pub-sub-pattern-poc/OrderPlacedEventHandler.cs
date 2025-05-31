public class OrderPlacedEventHandler
{
    public void Handle(OrderPlacedEvent orderPlacedEvent)
    {
        // Logic to handle the order placed event
        Console.WriteLine($"Order placed: {orderPlacedEvent.OrderId} for customer {orderPlacedEvent.CustomerId} on {orderPlacedEvent.OrderDate}");
    }
}