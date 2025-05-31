using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

// See https://aka.ms/new-console-template for more information


// Simulating an order placed event

public class OrderPlacedEventHandler2 : IEventHandler<BaseEvent>
{
    private readonly ILogger<OrderPlacedEventHandler2> _logger;

    public OrderPlacedEventHandler2(ILogger<OrderPlacedEventHandler2> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(BaseEvent @event)
    {
        try
        {
            // Check the actual type and version of the event
            if (@event is OrderPlacedEvent orderPlacedEventV1)
            {
                _logger.LogInformation("Processing OrderPlacedEvent V1 in handler 2 (version {EventVersion}): {OrderId} for customer {CustomerId}", 
                    orderPlacedEventV1.Version, orderPlacedEventV1.OrderId, orderPlacedEventV1.CustomerId);

                // Simulate some async work
                await Task.Delay(100);

                // Logic for version 1
                Console.WriteLine($"Order placed in handler 2 (version {orderPlacedEventV1.Version}) processed by Handler 2: {orderPlacedEventV1.OrderId} for customer {orderPlacedEventV1.CustomerId} on {orderPlacedEventV1.OrderDate}");

                _logger.LogInformation("Successfully processed OrderPlacedEvent V1 in handler 2 (version {EventVersion}): {OrderId}", orderPlacedEventV1.Version, orderPlacedEventV1.OrderId);
            }
            else if (@event is OrderPlacedEventV2 orderPlacedEventV2)
            {
                 _logger.LogInformation("Processing OrderPlacedEvent V2 in handler 2 (version {EventVersion}): {OrderId} for customer {CustomerId} with shipping address {ShippingAddress}", 
                    orderPlacedEventV2.Version, orderPlacedEventV2.OrderId, orderPlacedEventV2.CustomerId, orderPlacedEventV2.ShippingAddress);

                // Simulate some async work
                await Task.Delay(100);

                // Logic for version 2
                 Console.WriteLine($"Order placed in handler 2 (version {orderPlacedEventV2.Version}) processed by Handler 2: {orderPlacedEventV2.OrderId} for customer {orderPlacedEventV2.CustomerId} on {orderPlacedEventV2.OrderDate} to {orderPlacedEventV2.ShippingAddress}");

                 _logger.LogInformation("Successfully processed OrderPlacedEvent V2 in handler 2 (version {EventVersion}): {OrderId}", orderPlacedEventV2.Version, orderPlacedEventV2.OrderId);
            }
             else
            {
                 _logger.LogWarning("Received unhandled event type {EventType} in OrderPlacedEventHandler2", @event.GetType().Name);
            }
        }
        catch (Exception ex)
        {
            // Note: Error logging might need to be more specific based on the event type/version
            _logger.LogError(ex, "Error processing event in OrderPlacedEventHandler2");
            throw; // Re-throw to let the event aggregator handle it
        }
    }
}
