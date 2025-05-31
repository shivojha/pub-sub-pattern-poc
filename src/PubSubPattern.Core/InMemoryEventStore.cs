using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

public class InMemoryEventStore : IEventStore
{
    private readonly List<BaseEvent> _events = new List<BaseEvent>();
    private readonly object _lock = new object();

    public Task AppendAsync(BaseEvent @event)
    {
        if (@event == null)
        {
            throw new ArgumentNullException(nameof(@event));
        }

        lock (_lock)
        {
            _events.Add(@event);
        }

        // In-memory operation, so just return completed task
        return Task.CompletedTask;
    }

    public async IAsyncEnumerable<BaseEvent> ReadEventsAsync(string streamId, long startPosition)
    {
        // For simplicity, we'll ignore streamId and startPosition for now and return all events
        // In a real implementation, you would filter based on streamId and position.
        
        List<BaseEvent> eventsToRead;
        lock (_lock)
        {
            // Return a copy to avoid issues with external enumeration while events are being added
            eventsToRead = new List<BaseEvent>(_events);
        }

        foreach (var @event in eventsToRead)
        {
            // Simulate asynchronous reading
            await Task.Yield(); 
            yield return @event;
        }
    }
} 