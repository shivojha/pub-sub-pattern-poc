using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

// See https://aka.ms/new-console-template for more information


// Simulating an order placed event

public class OrderPlacedEventHandler2 : IEventHandler<OrderPlacedEvent>
{
    private readonly ILogger<OrderPlacedEventHandler2> _logger;

    public OrderPlacedEventHandler2(ILogger<OrderPlacedEventHandler2> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(OrderPlacedEvent @event)
    {
        try
        {
            _logger.LogInformation("Processing order placed event in handler 2: {OrderId} for customer {CustomerId}", 
                @event.OrderId, @event.CustomerId);

            // Simulate some async work
            await Task.Delay(100);

            // Your business logic here
            Console.WriteLine($"Order placed in handler 2: {@event.OrderId} for customer {@event.CustomerId} on {@event.OrderDate}");

            _logger.LogInformation("Successfully processed order placed event in handler 2: {OrderId}", @event.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing order placed event in handler 2: {OrderId}", @event.OrderId);
            throw; // Re-throw to let the event aggregator handle it
        }
    }
}
