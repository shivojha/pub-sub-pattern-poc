// This file is part of the Pub-Sub Pattern Proof of Concept project.
// It implements a simple event aggregator for managing event subscriptions and publications.
// The EventAggregator class allows components to subscribe to events and publish them without needing to know about each other directly.
// This is a basic implementation and can be extended with features like thread safety, filtering, etc.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Linq; // Added for LINQ extension methods
using System.Diagnostics; // Added for Stopwatch

public class EventAggregator : IEventAggregator
{
    private readonly ILogger<EventAggregator> _logger;
    private readonly object _lock = new object();
    // Modified to store delegates, their minimum supported version, and an optional filter predicate
    private readonly Dictionary<Type, List<(Delegate Handler, int MinimumVersion, object Filter)>> _subscriptions = new();
    // Dictionary to store event transformers: SourceType -> List<(TargetType, TransformerFunc)>>
    private readonly Dictionary<Type, List<(Type TargetType, object Transformer)>> _transformers = new();

    private readonly IEventStore _eventStore;

    public EventAggregator(ILogger<EventAggregator> logger, IEventStore eventStore)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
    }

    // Modified Subscribe method to accept a minimumVersion and an optional filter
    public void Subscribe<T>(Action<T> action, int minimumVersion = 1, Func<T, bool> filter = null) where T : BaseEvent
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        lock (_lock)
        {
            var type = typeof(T);
            if (!_subscriptions.ContainsKey(type))
            {
                _subscriptions[type] = new List<(Delegate Handler, int MinimumVersion, object Filter)>();
            }
            _subscriptions[type].Add((action, minimumVersion, filter));
            _logger.LogInformation("Subscribed to event type: {EventType} with minimum version: {MinimumVersion}{FilterStatus}", 
                               type.Name, minimumVersion, filter == null ? " (no filter)" : " (with filter)");
        }
    }

    public async Task PublishAsync<T>(T eventMessage) where T : BaseEvent
    {
        // Initial call, create a new set of processed types and set isReplay to false
        await PublishInternalAsync(eventMessage, new HashSet<Type>(), isReplay: false);
    }

    private async Task PublishInternalAsync<T>(T eventMessage, HashSet<Type> processedTypes, bool isReplay) where T : BaseEvent
    {
        if (eventMessage == null)
            throw new ArgumentNullException(nameof(eventMessage));

        // Persist the event using the event store only if it's not a replay
        if (!isReplay)
        {
            await _eventStore.AppendAsync(eventMessage);
            _logger.LogInformation("Event {EventType} (version {EventVersion}) persisted to event store.", eventMessage.GetType().Name, eventMessage.Version);
        }
        else
        {
             _logger.LogInformation("Processing replayed event {EventType} (version {EventVersion})", eventMessage.GetType().Name, eventMessage.Version);
        }

        var eventType = eventMessage.GetType();

        // Prevent infinite transformation loops
        if (processedTypes.Contains(eventType))
        {
            _logger.LogWarning("Skipping publishing event type {EventType} to avoid infinite loop", eventType.Name);
            return;
        }

        // Add the current event type to the processed set
        processedTypes.Add(eventType);

        List<(Delegate Handler, int MinimumVersion, object Filter)> handlersWithDetails;
        lock (_lock)
        {
            // Retrieve all handlers for the event type (using the base type to get all versions)
            // Note: Handlers are registered against the concrete event type, not BaseEvent in the dictionary key
            // We need to get handlers for the exact type or potential base types if we change subscription storage
            // For now, assuming subscription is for the concrete type T passed to Subscribe.
            // To handle handlers subscribing to BaseEvent, we would need to iterate through all subscriptions.
            // Let's stick to the current dictionary structure which maps concrete type to handlers.
            if (!_subscriptions.TryGetValue(eventType, out handlersWithDetails))
            {
                 // If no direct subscribers, check if there are subscribers for BaseEvent that can handle this specific event type
                 if (!_subscriptions.TryGetValue(typeof(BaseEvent), out handlersWithDetails))
                 {
                    _logger.LogWarning("No subscribers found for event type: {EventType}", eventType.Name);
                 }
            }
        }

        if (handlersWithDetails != null)
        {
            var tasks = new List<Task>();
            
            // Filter handlers based on the event version and the filter predicate
            var relevantHandlers = handlersWithDetails.Where(h => 
                                                    eventMessage.Version >= h.MinimumVersion && 
                                                    (h.Filter == null || ((Func<T, bool>)h.Filter)(eventMessage)))
                                               .ToList();

            if (!relevantHandlers.Any())
            {
                 _logger.LogWarning("No subscribers found for event type: {EventType} (version {EventVersion}) that match the criteria", eventType.Name, eventMessage.Version);
            }
            else
            {
                 foreach (var handlerWithDetails in relevantHandlers)
                 {
                     var handlerType = handlerWithDetails.Handler.Method.DeclaringType?.Name ?? "UnknownHandler";
                     var stopwatch = new Stopwatch();
                     stopwatch.Start();

                     try
                     {
                         if (handlerWithDetails.Handler is Action<T> action)
                         {
                             tasks.Add(Task.Run(() => action(eventMessage)).ContinueWith(t => 
                             {
                                 stopwatch.Stop();
                                 if (t.IsCompletedSuccessfully)
                                 {
                                     _logger.LogInformation("Handler {HandlerType} successfully processed event {EventType} (version {EventVersion}) in {Elapsed} ms", 
                                         handlerType, eventType.Name, eventMessage.Version, stopwatch.ElapsedMilliseconds);
                                 }
                                 else if (t.IsFaulted)
                                 {
                                     _logger.LogError(t.Exception, "Handler {HandlerType} failed to process event {EventType} (version {EventVersion}) after {Elapsed} ms", 
                                         handlerType, eventType.Name, eventMessage.Version, stopwatch.ElapsedMilliseconds);
                                 }
                             }));
                         }
                         // Handle handlers subscribed to BaseEvent but receiving a derived type
                         else if (handlerWithDetails.Handler is Action<BaseEvent> baseAction && eventMessage is BaseEvent)
                         {
                              tasks.Add(Task.Run(() => baseAction(eventMessage)).ContinueWith(t =>
                              {
                                   stopwatch.Stop();
                                   if (t.IsCompletedSuccessfully)
                                   {
                                       _logger.LogInformation("Handler {HandlerType} successfully processed event {EventType} (version {EventVersion}) in {Elapsed} ms", 
                                           handlerType, eventType.Name, eventMessage.Version, stopwatch.ElapsedMilliseconds);
                                   }
                                   else if (t.IsFaulted)
                                   {
                                       _logger.LogError(t.Exception, "Handler {HandlerType} failed to process event {EventType} (version {EventVersion}) after {Elapsed} ms", 
                                           handlerType, eventType.Name, eventMessage.Version, stopwatch.ElapsedMilliseconds);
                                   }
                              }));
                         }
                     }
                     catch (Exception ex)
                     {
                         stopwatch.Stop(); // Ensure stopwatch is stopped even if starting the task throws
                         _logger.LogError(ex, "Error starting task for handler {HandlerType} processing event {EventType} (version {EventVersion}) after {Elapsed} ms", 
                             handlerType, eventType.Name, eventMessage.Version, stopwatch.ElapsedMilliseconds);
                     }
                 }
                await Task.WhenAll(tasks);
            }
        }

        // Apply transformations and publish transformed events
        List<(Type TargetType, object Transformer)> transformersForSourceType = null;
        lock (_lock)
        {
            if (_transformers.TryGetValue(eventType, out var foundTransformers))
            {
                transformersForSourceType = new List<(Type TargetType, object Transformer)>(foundTransformers);
            }
        }

        if (transformersForSourceType != null)
        {
            foreach (var transformerDetail in transformersForSourceType)
            {
                try
                {
                    // Assuming the transformer is Func<T, TTarget>
                    var transformer = transformerDetail.Transformer;
                    var targetType = transformerDetail.TargetType;

                    // Use dynamic to invoke the generic transformer with the correct types
                    dynamic dynamicTransformer = transformer;
                    dynamic transformedEvent = dynamicTransformer((dynamic)eventMessage);

                    // Recursively publish the transformed event
                    // Need to cast the transformed event to BaseEvent to call PublishInternalAsync
                    if (transformedEvent is BaseEvent transformedBaseEvent)
                    {
                         _logger.LogInformation("Applying transformation from {SourceType} to {TargetType}", eventType.Name, targetType.Name);
                         await PublishInternalAsync(transformedBaseEvent, processedTypes, isReplay);
                    }
                    else
                    {
                         _logger.LogError("Transformation from {SourceType} resulted in a type {TargetType} that does not inherit from BaseEvent", eventType.Name, targetType.Name);
                    }
                }
                catch (Exception ex)
                {
                     _logger.LogError(ex, "Error applying transformation from {SourceType}", eventType.Name);
                }
            }
        }
    }

    // Modified Unsubscribe method to consider the filter
    public void Unsubscribe<T>(Action<T> action, Func<T, bool> filter = null) where T : BaseEvent
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        lock (_lock)
        {
            var type = typeof(T);
            if (_subscriptions.TryGetValue(type, out var handlersWithDetails))
            {
                // Find the handler(s) to remove based on delegate instance and filter
                handlersWithDetails.RemoveAll(h => h.Handler == (Delegate)action && Equals(h.Filter, filter));

                if (handlersWithDetails.Count == 0)
                {
                    _subscriptions.Remove(type);
                }
                _logger.LogInformation("Unsubscribed delegate from event type: {EventType}{FilterStatus}", 
                               type.Name, filter == null ? " (no filter)" : " (with filter)");
            }
        }
    }

    public void RegisterTransformer<TSource, TTarget>(Func<TSource, TTarget> transformer) 
        where TSource : BaseEvent
        where TTarget : BaseEvent
    {
        if (transformer == null)
            throw new ArgumentNullException(nameof(transformer));

        lock (_lock)
        {
            var sourceType = typeof(TSource);
            if (!_transformers.ContainsKey(sourceType))
            {
                _transformers[sourceType] = new List<(Type TargetType, object Transformer)>();
            }
            _transformers[sourceType].Add((typeof(TTarget), transformer));
            _logger.LogInformation("Registered transformer from {SourceType} to {TargetType}", sourceType.Name, typeof(TTarget).Name);
        }
    }

    public async Task ReplayEventsAsync()
    {
        _logger.LogInformation("Starting event replay from event store.");

        // Read events from the event store (assuming a single stream for simplicity)
        // In a real application, you might read from specific streams or with filters.
        await foreach (var eventToReplay in _eventStore.ReadEventsAsync("global-stream", 0))
        {
            // Use the non-generic PublishInternalAsync to handle different event types
            // We need to call the internal method with isReplay set to true
            var publishMethod = typeof(EventAggregator).GetMethod("PublishInternalAsync", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            // Create a new HashSet for each replay to avoid infinite loops within the replay of a single event
            // Pass isReplay as true
            // Pass a new HashSet<Type>() for each event replay to avoid issues if replaying the same event type multiple times
            await (Task)publishMethod.MakeGenericMethod(eventToReplay.GetType())
                                    .Invoke(this, new object[] { eventToReplay, new HashSet<Type>(), true });
        }

        _logger.LogInformation("Event replay completed.");
    }
}

public interface IEventAggregator
{
    void Subscribe<T>(Action<T> action, int minimumVersion = 1, Func<T, bool> filter = null) where T : BaseEvent;
    Task PublishAsync<T>(T eventMessage) where T : BaseEvent;
    void Unsubscribe<T>(Action<T> action, Func<T, bool> filter = null) where T : BaseEvent;
    void RegisterTransformer<TSource, TTarget>(Func<TSource, TTarget> transformer) 
        where TSource : BaseEvent
        where TTarget : BaseEvent;
    Task ReplayEventsAsync();
}
