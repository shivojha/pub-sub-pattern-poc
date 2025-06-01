using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using EventStore.Client;
using Microsoft.Extensions.Logging;

public class EventStoreDBEventStore : IEventStore
{
    private readonly EventStoreClient _client;
    private readonly ILogger<EventStoreDBEventStore> _logger;
    private const string DefaultStreamPrefix = "pubsub-";

    public EventStoreDBEventStore(EventStoreClient client, ILogger<EventStoreDBEventStore> logger)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task AppendAsync(BaseEvent @event)
    {
        if (@event == null)
        {
            throw new ArgumentNullException(nameof(@event));
        }

        try
        {
            var eventType = @event.GetType().Name;
            var streamName = $"{DefaultStreamPrefix}{eventType}";
            
            // Serialize the event
            var eventData = JsonSerializer.SerializeToUtf8Bytes(@event);
            
            // Create EventStore event
            var eventStoreEvent = new EventData(
                Uuid.NewUuid(),
                eventType,
                eventData,
                null, // No metadata for now
                "application/json"
            );

            // Append to stream
            await _client.AppendToStreamAsync(
                streamName,
                StreamState.Any,
                new[] { eventStoreEvent }
            );

            _logger.LogInformation("Event {EventType} (version {EventVersion}) appended to stream {StreamName}",
                eventType, @event.Version, streamName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error appending event {EventType} to EventStoreDB", @event.GetType().Name);
            throw;
        }
    }

    public async IAsyncEnumerable<BaseEvent> ReadEventsAsync(string streamId, long startPosition)
    {
        var streamName = $"{DefaultStreamPrefix}{streamId}";
        IAsyncEnumerable<ResolvedEvent> result;
        
        try
        {
            result = _client.ReadStreamAsync(
                Direction.Forwards,
                streamName,
                StreamPosition.FromInt64(startPosition)
            );
        }
        catch (StreamNotFoundException)
        {
            _logger.LogInformation("Stream {StreamName} does not exist yet. No events to replay.", streamName);
            yield break;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading events from stream {StreamId}", streamId);
            throw;
        }

        await foreach (var resolvedEvent in result)
        {
            var eventType = Type.GetType(resolvedEvent.Event.EventType);
            if (eventType != null && typeof(BaseEvent).IsAssignableFrom(eventType))
            {
                var @event = JsonSerializer.Deserialize(
                    resolvedEvent.Event.Data.Span,
                    eventType
                ) as BaseEvent;

                if (@event != null)
                {
                    yield return @event;
                }
            }
        }
    }
}