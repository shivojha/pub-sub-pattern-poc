# Pub-Sub Pattern POC

This project demonstrates a robust implementation of the Publish-Subscribe pattern in .NET, featuring event versioning, transformation, and persistence using EventStoreDB.

## Features

- **Event Versioning**: Support for multiple versions of events with automatic transformation
- **Event Persistence**: Events are stored in EventStoreDB for durability and replay capabilities
- **Flexible Subscriptions**: Subscribe to events with version-specific filters
- **Event Transformation**: Automatic transformation between event versions
- **Docker Support**: Easy deployment with Docker and Docker Compose

## Architecture

```mermaid
graph TD
    A[Publisher] -->|Publishes Events| B[EventAggregator]
    B -->|Transforms Events| C[Event Transformers]
    B -->|Stores Events| D[EventStoreDB]
    B -->|Routes Events| E[Event Handlers]
    E -->|Handles Events| F[OrderPlacedEventHandler]
    E -->|Handles Events| G[OrderPlacedEventHandler2]
    D -->|Replays Events| B
```

## Components

1. **EventAggregator**: Central hub for event publishing and subscription
   - Manages event subscriptions
   - Handles event transformation
   - Routes events to appropriate handlers
   - Persists events to EventStoreDB

2. **EventStoreDB**: Event store for persistence
   - Stores all events in an append-only log
   - Enables event replay
   - Provides durability and consistency

3. **Event Handlers**: Process specific event types
   - OrderPlacedEventHandler: Handles OrderPlacedEventV2 with USA shipping addresses
   - OrderPlacedEventHandler2: Handles OrderPlacedEvent V1 with specific OrderId

4. **Event Transformers**: Convert between event versions
   - Transforms OrderPlacedEvent (V1) to OrderPlacedEventV2 (V2)
   - Adds default shipping address for V1 events

## Event Flow

1. Publisher creates and publishes an event
2. EventAggregator receives the event
3. Event is transformed if necessary (e.g., V1 to V2)
4. Event is stored in EventStoreDB
5. Event is routed to subscribed handlers based on filters
6. Handlers process the event according to their logic

## Event Flow Sequence

```mermaid
sequenceDiagram
    participant P as Publisher
    participant EA as EventAggregator
    participant ET as Event Transformer
    participant ES as EventStoreDB
    participant H as Event Handler

    P->>EA: Publish Event V1
    EA->>ET: Transform Event
    ET-->>EA: Return Event V2
    EA->>ES: Store Event
    ES-->>EA: Confirm Storage
    EA->>H: Route Event
    H-->>EA: Process Complete
```

## Getting Started

### Prerequisites

- .NET 8.0 SDK
- Docker and Docker Compose
- EventStoreDB (included in Docker setup)

### Running the Application

1. Clone the repository
2. Navigate to the project directory
3. Run the application using Docker Compose:

```bash
docker compose up --build
```

The application will start and:
- Initialize EventStoreDB
- Set up event subscriptions
- Publish sample events
- Demonstrate event transformation and handling

### Configuration

The application uses the following configuration:

- EventStoreDB connection: `esdb://admin:changeit@eventstore:2113?tls=false`
- HTTP port: 5000
- EventStoreDB ports: 2113 (HTTP), 1113 (TCP)

## Event Examples

### Version 1 Event
```csharp
var orderEventV1 = new OrderPlacedEvent("12345", "C001", DateTime.Now);
```

### Version 2 Event
```csharp
var orderEventV2 = new OrderPlacedEventV2("67890", "C002", DateTime.UtcNow, "123 Main St, Anytown, USA");
```

## Event Handlers

### Handler 1
- Subscribes to OrderPlacedEventV2
- Filters for events with USA shipping addresses
- Processes events with shipping information

### Handler 2
- Subscribes to OrderPlacedEvent V1
- Filters for events with OrderId "12345"
- Processes basic order information

## Event Transformation

The system automatically transforms V1 events to V2 by adding a default shipping address:

```csharp
eventAggregator.RegisterTransformer<OrderPlacedEvent, OrderPlacedEventV2>(eventV1 => 
    new OrderPlacedEventV2(eventV1.OrderId, eventV1.CustomerId, eventV1.OrderDate, "Unknown Address"));
```

## Docker Configuration

The application uses Docker Compose to set up the following services:

1. **EventStoreDB**
   - Image: eventstore/eventstore:22.10.1-buster-slim
   - Ports: 2113 (HTTP), 1113 (TCP)
   - Configuration:
     - Single node cluster
     - Insecure mode (no TLS)
     - Enabled projections
     - Gossip on single node

2. **PubSub Application**
   - .NET 8.0 application
   - Port: 5000
   - Depends on EventStoreDB
   - Uses eventstore-network for communication

## Network Configuration

The services communicate through a dedicated Docker network:
- Network name: eventstore-network
- Driver: bridge
- Services:
  - eventstore (EventStoreDB)
  - pubsub-app (Application)

## Health Checks

EventStoreDB includes a health check to ensure it's ready before the application starts:
- Endpoint: http://localhost:2113/health/live
- Interval: 10s
- Timeout: 5s
- Retries: 5

## Detailed EventStoreDB Configuration

### Connection Settings
```csharp
var settings = EventStoreClientSettings.Create(connectionString);
settings.ConnectivitySettings.Insecure = true;
```

### Docker Environment Variables
```yaml
environment:
  - EVENTSTORE_CLUSTER_SIZE=1
  - EVENTSTORE_RUN_PROJECTIONS=All
  - EVENTSTORE_START_STANDARD_PROJECTIONS=true
  - EVENTSTORE_INSECURE=true
  - EVENTSTORE_ENABLE_EXTERNAL_TCP=true
  - EVENTSTORE_ENABLE_ATOM_PUB_OVER_HTTP=true
  - EVENTSTORE_EXT_TCP_PORT=1113
  - EVENTSTORE_HTTP_PORT=2113
  - EVENTSTORE_DISCOVER_VIA_DNS=false
  - EVENTSTORE_GOSSIP_ON_SINGLE_NODE=true
```

### Event Storage
- Events are stored in an append-only log
- Each event is stored with metadata including:
  - Event type
  - Version
  - Timestamp
  - Stream name
  - Event number

### Stream Configuration
- Global stream: `pubsub-global-stream`
- Event format: JSON
- Metadata: Event type and version information

## Advanced Troubleshooting

### 1. Connection Issues

#### Symptoms
- `Grpc.Core.RpcException: Status(StatusCode="DeadlineExceeded")`
- `Failed to discover candidate in 10 attempts`
- Connection refused errors

#### Solutions
1. Check EventStoreDB container status:
   ```bash
   docker compose ps eventstore
   ```

2. Verify network connectivity:
   ```bash
   docker network inspect pub-sub-pattern-poc_eventstore-network
   ```

3. Test EventStoreDB health:
   ```bash
   curl http://localhost:2113/health/live
   ```

4. Check DNS resolution:
   ```bash
   docker exec pubsub-app ping eventstore
   ```

### 2. Event Storage Issues

#### Symptoms
- Events not being stored
- Stream not found errors
- Append failures

#### Solutions
1. Verify stream existence:
   ```bash
   curl http://localhost:2113/streams/pubsub-global-stream
   ```

2. Check event store logs:
   ```bash
   docker compose logs eventstore
   ```

3. Verify permissions:
   ```bash
   curl -u admin:changeit http://localhost:2113/streams/pubsub-global-stream
   ```

### 3. Performance Issues

#### Symptoms
- Slow event processing
- High latency
- Connection timeouts

#### Solutions
1. Monitor EventStoreDB metrics:
   ```bash
   curl http://localhost:2113/stats
   ```

2. Check system resources:
   ```bash
   docker stats eventstore
   ```

3. Verify network performance:
   ```bash
   docker exec pubsub-app ping -c 10 eventstore
   ```

## Network Configuration Details

### Docker Network Setup
```yaml
networks:
  eventstore-network:
    driver: bridge
    ipam:
      driver: default
      config:
        - subnet: 172.28.0.0/16
```

### Service Network Configuration
```yaml
services:
  eventstore:
    networks:
      eventstore-network:
        aliases:
          - eventstore
  pubsub-app:
    networks:
      eventstore-network:
        aliases:
          - pubsub-app
```

### Port Mappings
- EventStoreDB HTTP: 2113
- EventStoreDB TCP: 1113
- Application: 5000

### Network Security
- Internal communication over bridge network
- No external access to EventStoreDB
- Application exposed on port 5000

## Monitoring and Maintenance

### Health Checks
```yaml
healthcheck:
  test: ["CMD", "curl", "-f", "http://localhost:2113/health/live"]
  interval: 10s
  timeout: 5s
  retries: 5
```

### Logging
- EventStoreDB logs: `/var/log/eventstore`
- Application logs: Docker container logs
- Access logs: HTTP endpoints

### Backup and Recovery
1. Backup EventStoreDB data:
   ```bash
   docker cp eventstore:/var/lib/eventstore ./backup
   ```

2. Restore from backup:
   ```bash
   docker cp ./backup eventstore:/var/lib/eventstore
   ```

### Performance Tuning
1. EventStoreDB settings:
   - Chunk size: 268435456 bytes
   - Cached chunks: -1 (auto)
   - Max append size: 1048576 bytes

2. Application settings:
   - Connection timeout: 10 seconds
   - Retry attempts: 3
   - Batch size: 100 events

## Development Workflow

### Local Development
1. Start services:
   ```bash
   docker compose up -d
   ```

2. Monitor logs:
   ```bash
   docker compose logs -f
   ```

3. Rebuild application:
   ```bash
   docker compose up --build pubsub-app
   ```

### Testing
1. Unit tests:
   ```bash
   dotnet test
   ```

2. Integration tests:
   ```bash
   docker compose -f docker-compose.test.yml up
   ```

### Deployment
1. Build images:
   ```bash
   docker compose build
   ```

2. Push images:
   ```bash
   docker compose push
   ```

3. Deploy:
   ```bash
   docker compose up -d
   ``` 