using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public interface IEventHandler<in T> where T : class
{
    Task HandleAsync(T @event);
}

public class OrderPlacedEventHandler : IEventHandler<OrderPlacedEvent>
{
    private readonly ILogger<OrderPlacedEventHandler> _logger;

    public OrderPlacedEventHandler(ILogger<OrderPlacedEventHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(OrderPlacedEvent @event)
    {
        try
        {
            _logger.LogInformation("Processing order placed event: {OrderId} for customer {CustomerId}", 
                @event.OrderId, @event.CustomerId);

            // Simulate some async work
            await Task.Delay(100);

            // Your business logic here
            Console.WriteLine($"Order placed: {@event.OrderId} for customer {@event.CustomerId} on {@event.OrderDate}");

            _logger.LogInformation("Successfully processed order placed event: {OrderId}", @event.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing order placed event: {OrderId}", @event.OrderId);
            throw; // Re-throw to let the event aggregator handle it
        }
    }
}