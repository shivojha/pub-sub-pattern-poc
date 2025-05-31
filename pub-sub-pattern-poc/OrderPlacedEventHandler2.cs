// See https://aka.ms/new-console-template for more information


// Simulating an order placed event

internal class OrderPlacedEventHandler2
{
    public OrderPlacedEventHandler2()
    {
    }

    internal void Handle(OrderPlacedEvent @event)
    {
        // Logic to handle the order placed event
        Console.WriteLine($"Order placed in handler 2: {@event.OrderId} for customer {@event.CustomerId} on {@event.OrderDate}");
    }
}
