// This file is part of the Pub-Sub Pattern Proof of Concept project.
// It implements a simple event aggregator for managing event subscriptions and publications.
// The EventAggregator class allows components to subscribe to events and publish them without needing to know about each other directly.
// This is a basic implementation and can be extended with features like thread safety, filtering, etc.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Linq; // Added for LINQ extension methods

public class EventAggregator : IEventAggregator
{
    private readonly ILogger<EventAggregator> _logger;
    private readonly object _lock = new object();
    // Modified to store delegates and their minimum supported version
    private readonly Dictionary<Type, List<(Delegate Handler, int MinimumVersion)>> _subscriptions = new();

    public EventAggregator(ILogger<EventAggregator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // Modified Subscribe method to accept a minimumVersion
    public void Subscribe<T>(Action<T> action, int minimumVersion = 1) where T : BaseEvent // Changed constraint to BaseEvent
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        lock (_lock)
        {
            var type = typeof(T);
            if (!_subscriptions.ContainsKey(type))
            {
                _subscriptions[type] = new List<(Delegate Handler, int MinimumVersion)>();
            }
            _subscriptions[type].Add((action, minimumVersion));
            _logger.LogInformation("Subscribed to event type: {EventType} with minimum version: {MinimumVersion}", type.Name, minimumVersion);
        }
    }

    public async Task PublishAsync<T>(T eventMessage) where T : BaseEvent // Changed constraint to BaseEvent
    {
        if (eventMessage == null)
            throw new ArgumentNullException(nameof(eventMessage));

        List<(Delegate Handler, int MinimumVersion)> handlersWithVersions;
        lock (_lock)
        {
            // Retrieve all handlers for the event type
            if (!_subscriptions.TryGetValue(eventMessage.GetType(), out handlersWithVersions))
            {
                _logger.LogWarning("No subscribers found for event type: {EventType}", eventMessage.GetType().Name);
                return;
            }
        }

        var tasks = new List<Task>();
        // Filter handlers based on the event version
        var relevantHandlers = handlersWithVersions.Where(h => eventMessage.Version >= h.MinimumVersion).ToList();

        if (!relevantHandlers.Any())
        {
             _logger.LogWarning("No subscribers found for event type: {EventType} that can handle version {EventVersion}", eventMessage.GetType().Name, eventMessage.Version);
             return;
        }

        foreach (var handlerWithVersion in relevantHandlers)
        {
            try
            {
                if (handlerWithVersion.Handler is Action<T> action)
                {
                    tasks.Add(Task.Run(() => action(eventMessage)));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing event {EventType} (version {EventVersion}) to handler", eventMessage.GetType().Name, eventMessage.Version);
            }
        }

        await Task.WhenAll(tasks);
    }

    // Modified Unsubscribe method
    public void Unsubscribe<T>(Action<T> action) where T : BaseEvent // Changed constraint to BaseEvent
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        lock (_lock)
        {
            var type = typeof(T);
            if (_subscriptions.TryGetValue(type, out var handlersWithVersions))
            {
                // Find the handler(s) to remove. Note: this will remove ALL registrations for this specific delegate instance.
                handlersWithVersions.RemoveAll(h => h.Handler == (Delegate)action);

                if (handlersWithVersions.Count == 0)
                {
                    _subscriptions.Remove(type);
                }
                _logger.LogInformation("Unsubscribed delegate from event type: {EventType}", type.Name);
            }
        }
    }
}

public interface IEventAggregator
{
    void Subscribe<T>(Action<T> action, int minimumVersion = 1) where T : BaseEvent;
    Task PublishAsync<T>(T eventMessage) where T : BaseEvent;
    void Unsubscribe<T>(Action<T> action) where T : BaseEvent;
}
