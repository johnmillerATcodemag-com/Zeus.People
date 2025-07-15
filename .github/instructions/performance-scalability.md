# Performance and Scalability Instructions

## Overview

Performance and scalability guidelines for the Academic Management System ensuring optimal response times and horizontal scaling capabilities.

## Performance Requirements

### Response Time Targets

- **API Endpoints**: < 200ms for 95th percentile
- **Database Queries**: < 100ms for simple operations
- **Complex Reports**: < 2 seconds for 95th percentile
- **Page Load Times**: < 1 second for UI components

### Throughput Targets

- **Concurrent Users**: Support 1,000 concurrent users
- **API Requests**: 10,000 requests per minute
- **Database Connections**: Efficient connection pooling
- **Message Processing**: 1,000 events per second

## Scalability Architecture

### Horizontal Scaling Strategy

- **Stateless API Design**: No server-side session state
- **Database Read Replicas**: Separate read and write databases
- **Caching Layers**: Redis for distributed caching
- **Load Balancing**: Azure Load Balancer with health checks

### Vertical Scaling Considerations

- **CPU Optimization**: Async/await throughout the application
- **Memory Management**: Proper disposal of resources
- **Database Optimization**: Efficient queries and indexing
- **Connection Pooling**: Optimal database connection usage

## Caching Strategy

### Application-Level Caching

```csharp
public class CachedAcademicQueryHandler : IRequestHandler<GetAcademicQuery, AcademicDto>
{
    private readonly IMemoryCache _cache;
    private readonly IRequestHandler<GetAcademicQuery, AcademicDto> _inner;

    public async Task<AcademicDto> Handle(GetAcademicQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"academic_{request.EmpNr}";

        if (_cache.TryGetValue(cacheKey, out AcademicDto cached))
            return cached;

        var result = await _inner.Handle(request, cancellationToken);

        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(15));
        return result;
    }
}
```

### Cache Invalidation Strategy

- **Time-based expiration**: 15 minutes for read models
- **Event-based invalidation**: Clear cache on domain events
- **Tag-based invalidation**: Group related cache entries
- **Cache warming**: Pre-populate frequently accessed data

### Distributed Caching with Redis

```csharp
public class RedisCacheService : ICacheService
{
    private readonly IDatabase _database;

    public async Task SetAsync<T>(string key, T value, TimeSpan expiration)
    {
        var serialized = JsonSerializer.Serialize(value);
        await _database.StringSetAsync(key, serialized, expiration);
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var value = await _database.StringGetAsync(key);
        return value.HasValue ? JsonSerializer.Deserialize<T>(value) : default;
    }
}
```

## Database Optimization

### Query Optimization

- **Proper Indexing**: Index all foreign keys and query columns
- **Query Analysis**: Use SQL Server Query Store
- **Execution Plans**: Monitor and optimize slow queries
- **Connection Pooling**: Configure optimal pool sizes

### Database Schema Optimization

```sql
-- Indexes for Academic table
CREATE INDEX IX_Academic_DepartmentId ON Academic(DepartmentId);
CREATE INDEX IX_Academic_RoomId ON Academic(RoomId);
CREATE INDEX IX_Academic_Rank ON Academic(Rank);

-- Indexes for Events table
CREATE INDEX IX_Events_AggregateId ON Events(AggregateId);
CREATE INDEX IX_Events_Timestamp ON Events(Timestamp);
CREATE INDEX IX_Events_EventType ON Events(EventType);
```

### Read Model Optimization

- **Denormalized Views**: Pre-computed aggregations
- **Materialized Views**: For complex queries
- **Projection Updates**: Event-driven read model updates
- **Query-Specific Models**: Tailored for specific use cases

## Asynchronous Processing

### Command Processing

```csharp
[QueueTrigger("academic-commands")]
public async Task ProcessCommand(CommandMessage message)
{
    var command = JsonSerializer.Deserialize<ICommand>(message.Body);
    await _mediator.Send(command);
}
```

### Event Processing

```csharp
public class AcademicEventHandler : INotificationHandler<AcademicCreatedEvent>
{
    public async Task Handle(AcademicCreatedEvent notification, CancellationToken cancellationToken)
    {
        // Update read models asynchronously
        await _readModelService.UpdateAcademicProjectionAsync(notification);

        // Send notifications
        await _notificationService.SendWelcomeEmailAsync(notification.EmpNr);
    }
}
```

### Background Services

```csharp
public class EventProjectionService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var events = await _eventStore.GetUnprocessedEventsAsync();

            foreach (var evt in events)
            {
                await ProcessEventAsync(evt);
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}
```

## Performance Monitoring

### Key Performance Indicators (KPIs)

- **Response Time Percentiles**: 50th, 95th, 99th percentiles
- **Throughput Metrics**: Requests per second
- **Error Rates**: 4xx and 5xx response rates
- **Resource Utilization**: CPU, memory, disk I/O

### Application Insights Configuration

```csharp
services.AddApplicationInsightsTelemetry(options =>
{
    options.EnableAdaptiveSampling = true;
    options.EnableQuickPulseMetricStream = true;
    options.EnablePerformanceCounterCollectionModule = true;
});

services.Configure<TelemetryConfiguration>(config =>
{
    config.TelemetryInitializers.Add(new CustomTelemetryInitializer());
});
```

### Custom Metrics

```csharp
public class PerformanceMetrics
{
    private readonly TelemetryClient _telemetryClient;

    public void TrackCommandDuration(string commandName, TimeSpan duration)
    {
        _telemetryClient.TrackMetric($"Command.{commandName}.Duration", duration.TotalMilliseconds);
    }

    public void TrackQueryDuration(string queryName, TimeSpan duration)
    {
        _telemetryClient.TrackMetric($"Query.{queryName}.Duration", duration.TotalMilliseconds);
    }
}
```

## Load Testing

### Load Test Scenarios

1. **Normal Load**: Simulate typical daily usage
2. **Peak Load**: Simulate end-of-semester registration
3. **Stress Test**: Push system beyond normal capacity
4. **Spike Test**: Sudden increase in load
5. **Volume Test**: Large amounts of data processing

### NBomber Load Test Example

```csharp
var scenario = Scenario.Create("academic_crud", async context =>
{
    var response = await httpClient.PostAsync("/api/academics", new StringContent(
        JsonSerializer.Serialize(new CreateAcademicRequest
        {
            EmpNr = Random.Next(100000, 999999),
            EmpName = $"Academic {Random.Next(1000)}",
            Rank = "L",
            DepartmentName = "Computer Science"
        }), Encoding.UTF8, "application/json"));

    return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
})
.WithLoadSimulations(
    Simulation.InjectPerSec(rate: 100, during: TimeSpan.FromMinutes(5))
);
```

## Optimization Techniques

### API Optimization

- **Response Compression**: Enable Gzip compression
- **Minimal APIs**: Use minimal API for simple endpoints
- **Pagination**: Implement cursor-based pagination
- **Field Selection**: Allow clients to specify required fields

### Memory Optimization

- **Object Pooling**: Reuse expensive objects
- **String Interning**: For frequently used strings
- **Lazy Loading**: Load data only when needed
- **Disposal Patterns**: Proper resource cleanup

### Network Optimization

- **HTTP/2**: Enable HTTP/2 for better performance
- **CDN**: Use Azure CDN for static content
- **Connection Pooling**: Optimize HTTP client usage
- **Timeout Configuration**: Appropriate timeout values

## Scalability Patterns

### CQRS Scaling Benefits

- **Read/Write Separation**: Scale reads and writes independently
- **Event Sourcing**: Replay events for new read models
- **Eventual Consistency**: Accept temporary inconsistency for performance
- **Polyglot Persistence**: Use optimal storage for each use case

### Microservices Considerations

- **Service Boundaries**: Align with domain boundaries
- **Database Per Service**: Avoid shared databases
- **Inter-Service Communication**: Use async messaging
- **Circuit Breaker**: Handle service failures gracefully

### Auto-Scaling Configuration

```json
{
  "autoScaling": {
    "minInstances": 2,
    "maxInstances": 10,
    "scaleOutCooldown": "PT5M",
    "scaleInCooldown": "PT10M",
    "rules": [
      {
        "metricName": "CpuPercentage",
        "threshold": 70,
        "direction": "increase",
        "type": "scaleOut"
      },
      {
        "metricName": "MemoryPercentage",
        "threshold": 80,
        "direction": "increase",
        "type": "scaleOut"
      }
    ]
  }
}
```
