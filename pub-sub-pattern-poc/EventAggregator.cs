// This file is part of the Pub-Sub Pattern Proof of Concept project.
// It implements a simple event aggregator for managing event subscriptions and publications.
// The EventAggregator class allows components to subscribe to events and publish them without needing to know about each other directly.
// This is a basic implementation and can be extended with features like thread safety, filtering, etc.
public class EventAggregator
{

    private readonly Dictionary<Type, List<Delegate>> _subscriptions = new();

    public EventAggregator()
    {
    }
    public void Subscribe<T>(Action<T> action)
    {
        // Implementation for subscribing to an event
        if (!_subscriptions.ContainsKey(typeof(T)))
        {
            _subscriptions[typeof(T)] = new List<Delegate> { action };
        }
        else
        {
            _subscriptions[typeof(T)].Add(action);
        }
    }

    public void Publish<T>(T eventMessage)
    {
        // Implementation for publishing an event
        if (_subscriptions.ContainsKey(eventMessage.GetType()))
        {
            foreach (var action in _subscriptions[eventMessage.GetType()])
            {
                ((Action<T>)action)(eventMessage);
            }
        }
    }
}
