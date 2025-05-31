// This file is part of the Pub-Sub Pattern Proof of Concept project.
// It implements a simple event aggregator for managing event subscriptions and publications.
// The EventAggregator class allows components to subscribe to events and publish them without needing to know about each other directly.
// This is a basic implementation and can be extended with features like thread safety, filtering, etc.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public class EventAggregator : IEventAggregator
{
    private readonly ILogger<EventAggregator> _logger;
    private readonly object _lock = new object();
    private readonly Dictionary<Type, List<Delegate>> _subscriptions = new();

    public EventAggregator(ILogger<EventAggregator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void Subscribe<T>(Action<T> action) where T : class
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        lock (_lock)
        {
            var type = typeof(T);
            if (!_subscriptions.ContainsKey(type))
            {
                _subscriptions[type] = new List<Delegate> { action };
            }
            else
            {
                _subscriptions[type].Add(action);
            }
            _logger.LogInformation("Subscribed to event type: {EventType}", type.Name);
        }
    }

    public async Task PublishAsync<T>(T eventMessage) where T : class
    {
        if (eventMessage == null)
            throw new ArgumentNullException(nameof(eventMessage));

        List<Delegate> handlers;
        lock (_lock)
        {
            if (!_subscriptions.TryGetValue(eventMessage.GetType(), out handlers))
            {
                _logger.LogWarning("No subscribers found for event type: {EventType}", eventMessage.GetType().Name);
                return;
            }
        }

        var tasks = new List<Task>();
        foreach (var handler in handlers)
        {
            try
            {
                if (handler is Action<T> action)
                {
                    tasks.Add(Task.Run(() => action(eventMessage)));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing event {EventType} to handler", eventMessage.GetType().Name);
            }
        }

        await Task.WhenAll(tasks);
    }

    public void Unsubscribe<T>(Action<T> action) where T : class
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        lock (_lock)
        {
            var type = typeof(T);
            if (_subscriptions.ContainsKey(type))
            {
                _subscriptions[type].Remove(action);
                if (_subscriptions[type].Count == 0)
                {
                    _subscriptions.Remove(type);
                }
                _logger.LogInformation("Unsubscribed from event type: {EventType}", type.Name);
            }
        }
    }
}

public interface IEventAggregator
{
    void Subscribe<T>(Action<T> action) where T : class;
    Task PublishAsync<T>(T eventMessage) where T : class;
    void Unsubscribe<T>(Action<T> action) where T : class;
}
