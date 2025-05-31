using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var services = new ServiceCollection();

// Configure logging
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});

// Register services
services.AddSingleton<IEventAggregator, EventAggregator>();
services.AddTransient<OrderPlacedEventHandler>();
services.AddTransient<OrderPlacedEventHandler2>();

var serviceProvider = services.BuildServiceProvider();
var eventAggregator = serviceProvider.GetRequiredService<IEventAggregator>();
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

// Register transformer from OrderPlacedEvent (V1) to OrderPlacedEventV2 (V2)
eventAggregator.RegisterTransformer<OrderPlacedEvent, OrderPlacedEventV2>(eventV1 => 
    new OrderPlacedEventV2(eventV1.OrderId, eventV1.CustomerId, eventV1.OrderDate, "Unknown Address")); // Provide a default/placeholder shipping address
logger.LogInformation("Registered transformer from OrderPlacedEvent (V1) to OrderPlacedEventV2 (V2).");

try
{
    // Subscribe handlers
    var handler1 = serviceProvider.GetRequiredService<OrderPlacedEventHandler>();
    var handler2 = serviceProvider.GetRequiredService<OrderPlacedEventHandler2>();

    // Subscribe handler1 to receive OrderPlacedEventV2 with ShippingAddress in USA
    eventAggregator.Subscribe<BaseEvent>(async @event => await handler1.HandleAsync(@event), 
        filter: (@event) => @event is OrderPlacedEventV2 v2 && v2.ShippingAddress.Contains("USA"));

    // Subscribe handler2 to receive OrderPlacedEvent V1 with OrderId "12345"
    eventAggregator.Subscribe<BaseEvent>(async @event => await handler2.HandleAsync(@event), 
        filter: (@event) => @event is OrderPlacedEvent v1 && v1.OrderId == "12345");

    // Publish a Version 1 event
    logger.LogInformation("Publishing OrderPlacedEvent (Version 1)...");
    var orderEventV1 = new OrderPlacedEvent("12345", "C001", DateTime.Now);
    await eventAggregator.PublishAsync(orderEventV1);

    Console.WriteLine("-----------------------------------------");

    // Publish a Version 2 event
    logger.LogInformation("Publishing OrderPlacedEventV2 (Version 2)...");
    var orderEventV2 = new OrderPlacedEventV2("67890", "C002", DateTime.UtcNow, "123 Main St, Anytown, USA");
    await eventAggregator.PublishAsync(orderEventV2);

    Console.WriteLine("-----------------------------------------");

    // Publish another Version 2 event that should not be handled by handler1
    logger.LogInformation("Publishing another OrderPlacedEventV2 (Version 2) without USA address...");
    var orderEventV2_non_usa = new OrderPlacedEventV2("98765", "C003", DateTime.UtcNow, "456 Oak Ave, Toronto, Canada");
    await eventAggregator.PublishAsync(orderEventV2_non_usa);

    logger.LogInformation("End of program execution. Press any key to exit.");
    Console.ReadLine();
}
catch (Exception ex)
{
    logger.LogError(ex, "An error occurred during program execution");
}
finally
{
    // Cleanup
    if (serviceProvider is IDisposable disposable)
    {
        disposable.Dispose();
    }
}
