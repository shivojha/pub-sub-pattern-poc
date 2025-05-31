using System.Threading.Tasks;
using System.Collections.Generic;

public interface IEventStore
{
    /// <summary>
    /// Appends an event to the event store.
    /// </summary>
    /// <param name="event">The event to append.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    Task AppendAsync(BaseEvent @event);

    /// <summary>
    /// Reads events from a specific stream in the event store.
    /// </summary>
    /// <param name="streamId">The ID of the stream to read from.</param>
    /// <param name="startPosition">The starting position (inclusive) in the stream.</param>
    /// <returns>An asynchronous enumerable of events from the stream.</returns>
    IAsyncEnumerable<BaseEvent> ReadEventsAsync(string streamId, long startPosition);
} 