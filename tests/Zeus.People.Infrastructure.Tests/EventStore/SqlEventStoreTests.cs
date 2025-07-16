using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Zeus.People.Domain.Events;
using Zeus.People.Domain.ValueObjects;
using Zeus.People.Infrastructure.EventStore;

namespace Zeus.People.Infrastructure.Tests.EventStore;

public class SqlEventStoreTests : IDisposable
{
    private readonly EventStoreContext _context;
    private readonly SqlEventStore _eventStore;

    public SqlEventStoreTests()
    {
        var options = new DbContextOptionsBuilder<EventStoreContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new EventStoreContext(options);
        _eventStore = new SqlEventStore(_context);
    }

    [Fact]
    public async Task AppendEventsAsync_ShouldPersistSingleEvent()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();
        var aggregateType = "Academic";
        var empNr = EmpNr.Create("AB1234");
        var empName = EmpName.Create("John Doe");
        var rank = Rank.Create("P");

        var domainEvent = new AcademicCreatedEvent(aggregateId, empNr, empName, rank);
        var events = new List<IDomainEvent> { domainEvent };

        // Act
        await _eventStore.AppendEventsAsync(aggregateId, aggregateType, events, 0);

        // Assert
        var storedEvents = await _context.Events.ToListAsync();
        storedEvents.Should().HaveCount(1);

        var storedEvent = storedEvents.First();
        storedEvent.AggregateId.Should().Be(aggregateId);
        storedEvent.AggregateType.Should().Be(aggregateType);
        storedEvent.EventType.Should().Be(nameof(AcademicCreatedEvent));
        storedEvent.Version.Should().Be(1);
        storedEvent.EventId.Should().Be(domainEvent.EventId);
        storedEvent.Timestamp.Should().Be(domainEvent.OccurredAt);
        storedEvent.EventData.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task AppendEventsAsync_ShouldPersistMultipleEvents()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();
        var aggregateType = "Academic";

        var empNr = EmpNr.Create("AB1234");
        var empName = EmpName.Create("John Doe");
        var oldRank = Rank.Create("L");
        var newRank = Rank.Create("SL");

        var events = new List<IDomainEvent>
        {
            new AcademicCreatedEvent(aggregateId, empNr, empName, oldRank),
            new AcademicRankChangedEvent(aggregateId, oldRank, newRank)
        };

        // Act
        await _eventStore.AppendEventsAsync(aggregateId, aggregateType, events, 0);

        // Assert
        var storedEvents = await _context.Events.OrderBy(e => e.Version).ToListAsync();
        storedEvents.Should().HaveCount(2);

        storedEvents[0].Version.Should().Be(1);
        storedEvents[0].EventType.Should().Be(nameof(AcademicCreatedEvent));

        storedEvents[1].Version.Should().Be(2);
        storedEvents[1].EventType.Should().Be(nameof(AcademicRankChangedEvent));
    }

    [Fact]
    public async Task AppendEventsAsync_ShouldHandleConcurrencyConflict()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();
        var aggregateType = "Academic";

        var empNr = EmpNr.Create("AB1234");
        var empName = EmpName.Create("John Doe");
        var rank = Rank.Create("P");

        var firstEvent = new AcademicCreatedEvent(aggregateId, empNr, empName, rank);
        await _eventStore.AppendEventsAsync(aggregateId, aggregateType, new[] { firstEvent }, 0);

        var secondEvent = new AcademicRankChangedEvent(aggregateId, rank, Rank.Create("SL"));

        // Act & Assert - Try to append with wrong expected version
        var act = async () => await _eventStore.AppendEventsAsync(aggregateId, aggregateType, new[] { secondEvent }, 0);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Concurrency conflict*");
    }

    [Fact]
    public async Task GetEventsAsync_ShouldRetrieveAllEventsForAggregate()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();
        var aggregateType = "Academic";

        var empNr = EmpNr.Create("AB1234");
        var empName = EmpName.Create("John Doe");
        var oldRank = Rank.Create("L");
        var newRank = Rank.Create("SL");

        var events = new List<IDomainEvent>
        {
            new AcademicCreatedEvent(aggregateId, empNr, empName, oldRank),
            new AcademicRankChangedEvent(aggregateId, oldRank, newRank)
        };

        await _eventStore.AppendEventsAsync(aggregateId, aggregateType, events, 0);

        // Act
        var retrievedEvents = await _eventStore.GetEventsAsync(aggregateId);

        // Assert
        retrievedEvents.Should().HaveCount(2);

        var eventsList = retrievedEvents.ToList();
        eventsList[0].Should().BeOfType<AcademicCreatedEvent>();
        eventsList[1].Should().BeOfType<AcademicRankChangedEvent>();

        var createdEvent = (AcademicCreatedEvent)eventsList[0];
        createdEvent.AcademicId.Should().Be(aggregateId);
        createdEvent.EmpNr.Value.Should().Be("AB1234");
        createdEvent.EmpName.Value.Should().Be("John Doe");
        createdEvent.Rank.Value.Should().Be("L");

        var rankChangedEvent = (AcademicRankChangedEvent)eventsList[1];
        rankChangedEvent.AcademicId.Should().Be(aggregateId);
        rankChangedEvent.OldRank.Value.Should().Be("L");
        rankChangedEvent.NewRank.Value.Should().Be("SL");
    }

    [Fact]
    public async Task GetEventsAsync_ShouldReturnEmptyForNonExistentAggregate()
    {
        // Arrange
        var nonExistentAggregateId = Guid.NewGuid();

        // Act
        var retrievedEvents = await _eventStore.GetEventsAsync(nonExistentAggregateId);

        // Assert
        retrievedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task GetEventsFromVersionAsync_ShouldRetrieveEventsFromSpecificVersion()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();
        var aggregateType = "Academic";

        var empNr = EmpNr.Create("AB1234");
        var empName = EmpName.Create("John Doe");
        var rank1 = Rank.Create("L");
        var rank2 = Rank.Create("SL");
        var rank3 = Rank.Create("P");

        var events = new List<IDomainEvent>
        {
            new AcademicCreatedEvent(aggregateId, empNr, empName, rank1),
            new AcademicRankChangedEvent(aggregateId, rank1, rank2),
            new AcademicRankChangedEvent(aggregateId, rank2, rank3)
        };

        await _eventStore.AppendEventsAsync(aggregateId, aggregateType, events, 0);

        // Act - Get events from version 1 (should return the last 2 events)
        var retrievedEvents = await _eventStore.GetEventsFromVersionAsync(aggregateId, 1);

        // Assert
        retrievedEvents.Should().HaveCount(2);

        var eventsList = retrievedEvents.ToList();
        eventsList.Should().AllBeOfType<AcademicRankChangedEvent>();

        var firstRankChange = (AcademicRankChangedEvent)eventsList[0];
        firstRankChange.NewRank.Value.Should().Be("SL");

        var secondRankChange = (AcademicRankChangedEvent)eventsList[1];
        secondRankChange.NewRank.Value.Should().Be("P");
    }

    [Fact]
    public async Task GetEventsFromTimestampAsync_ShouldRetrieveEventsFromSpecificTime()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();
        var aggregateType = "Academic";
        var baseTime = DateTime.UtcNow.AddHours(-2);

        var empNr = EmpNr.Create("AB1234");
        var empName = EmpName.Create("John Doe");
        var rank = Rank.Create("P");

        // Create event with specific timestamp
        var oldEvent = new AcademicCreatedEvent(aggregateId, empNr, empName, rank);
        await _eventStore.AppendEventsAsync(aggregateId, aggregateType, new[] { oldEvent }, 0);

        // Wait a bit and create another event
        await Task.Delay(10);
        var cutoffTime = DateTime.UtcNow;
        await Task.Delay(10);

        var newEvent = new AcademicRankChangedEvent(aggregateId, rank, Rank.Create("SL"));
        await _eventStore.AppendEventsAsync(aggregateId, aggregateType, new[] { newEvent }, 1);

        // Act - Get events from cutoff time
        var retrievedEvents = await _eventStore.GetEventsFromTimestampAsync(cutoffTime);

        // Assert
        retrievedEvents.Should().HaveCount(1);
        retrievedEvents.First().Should().BeOfType<AcademicRankChangedEvent>();
    }

    [Fact]
    public async Task EventSerialization_ShouldPreserveValueObjects()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();
        var aggregateType = "Academic";

        var empNr = EmpNr.Create("XY9876");
        var empName = EmpName.Create("Dr. Jane Smith");
        var rank = Rank.Create("P");

        var originalEvent = new AcademicCreatedEvent(aggregateId, empNr, empName, rank);
        await _eventStore.AppendEventsAsync(aggregateId, aggregateType, new[] { originalEvent }, 0);

        // Act
        var retrievedEvents = await _eventStore.GetEventsAsync(aggregateId);

        // Assert
        var retrievedEvent = (AcademicCreatedEvent)retrievedEvents.First();

        // Verify all value objects are properly deserialized
        retrievedEvent.AcademicId.Should().Be(aggregateId);
        retrievedEvent.EmpNr.Value.Should().Be("XY9876");
        retrievedEvent.EmpName.Value.Should().Be("Dr. Jane Smith");
        retrievedEvent.Rank.Value.Should().Be("P");
        retrievedEvent.EventId.Should().Be(originalEvent.EventId);
        retrievedEvent.OccurredAt.Should().BeCloseTo(originalEvent.OccurredAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task MultipleAggregates_ShouldIsolateEventStreams()
    {
        // Arrange
        var aggregateId1 = Guid.NewGuid();
        var aggregateId2 = Guid.NewGuid();
        var aggregateType = "Academic";

        var empNr1 = EmpNr.Create("AB1111");
        var empName1 = EmpName.Create("John Doe");
        var rank1 = Rank.Create("P");

        var empNr2 = EmpNr.Create("CD2222");
        var empName2 = EmpName.Create("Jane Smith");
        var rank2 = Rank.Create("L");

        var event1 = new AcademicCreatedEvent(aggregateId1, empNr1, empName1, rank1);
        var event2 = new AcademicCreatedEvent(aggregateId2, empNr2, empName2, rank2);

        await _eventStore.AppendEventsAsync(aggregateId1, aggregateType, new[] { event1 }, 0);
        await _eventStore.AppendEventsAsync(aggregateId2, aggregateType, new[] { event2 }, 0);

        // Act
        var events1 = await _eventStore.GetEventsAsync(aggregateId1);
        var events2 = await _eventStore.GetEventsAsync(aggregateId2);

        // Assert
        events1.Should().HaveCount(1);
        events2.Should().HaveCount(1);

        var retrievedEvent1 = (AcademicCreatedEvent)events1.First();
        var retrievedEvent2 = (AcademicCreatedEvent)events2.First();

        retrievedEvent1.EmpNr.Value.Should().Be("AB1111");
        retrievedEvent2.EmpNr.Value.Should().Be("CD2222");
    }

    [Fact]
    public async Task EventStore_ShouldHandleDepartmentEvents()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();
        var aggregateType = "Department";

        var departmentEvent = new DepartmentCreatedEvent(aggregateId, "Computer Science");
        await _eventStore.AppendEventsAsync(aggregateId, aggregateType, new[] { departmentEvent }, 0);

        // Act
        var retrievedEvents = await _eventStore.GetEventsAsync(aggregateId);

        // Assert
        retrievedEvents.Should().HaveCount(1);
        var retrievedEvent = (DepartmentCreatedEvent)retrievedEvents.First();
        retrievedEvent.DepartmentId.Should().Be(aggregateId);
        retrievedEvent.Name.Should().Be("Computer Science");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
