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

try
{
    // Subscribe handlers
    var handler1 = serviceProvider.GetRequiredService<OrderPlacedEventHandler>();
    var handler2 = serviceProvider.GetRequiredService<OrderPlacedEventHandler2>();

    // Subscribe handlers to BaseEvent to receive all versions
    eventAggregator.Subscribe<BaseEvent>(async @event => await handler1.HandleAsync(@event));
    eventAggregator.Subscribe<BaseEvent>(async @event => await handler2.HandleAsync(@event));

    // Publish a Version 1 event
    logger.LogInformation("Publishing OrderPlacedEvent (Version 1)...");
    var orderEventV1 = new OrderPlacedEvent("12345", "C001", DateTime.Now);
    await eventAggregator.PublishAsync(orderEventV1);

    Console.WriteLine("-----------------------------------------");

    // Publish a Version 2 event
    logger.LogInformation("Publishing OrderPlacedEventV2 (Version 2)...");
    var orderEventV2 = new OrderPlacedEventV2("67890", "C002", DateTime.UtcNow, "123 Main St, Anytown, USA");
    await eventAggregator.PublishAsync(orderEventV2);

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
