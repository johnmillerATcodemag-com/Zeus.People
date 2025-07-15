using FluentAssertions;
using Zeus.People.Domain.Entities;
using Zeus.People.Domain.Events;

namespace Zeus.People.Domain.Tests.Entities;

public class AggregateRootTests
{
    private class TestAggregateRoot : AggregateRoot
    {
        public TestAggregateRoot() : base() { }
        public TestAggregateRoot(Guid id) : base(id) { }

        public void AddTestEvent(IDomainEvent domainEvent)
        {
            RaiseDomainEvent(domainEvent);
        }
    }

    private class TestDomainEvent : DomainEvent
    {
        public string TestData { get; }

        public TestDomainEvent(string testData)
        {
            TestData = testData;
        }
    }

    [Fact]
    public void Constructor_ShouldSetIdAndTimestamps()
    {
        // Act
        var aggregate = new TestAggregateRoot();

        // Assert
        aggregate.Id.Should().NotBeEmpty();
        aggregate.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        aggregate.ModifiedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        aggregate.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithId_ShouldSetSpecificId()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var aggregate = new TestAggregateRoot(id);

        // Assert
        aggregate.Id.Should().Be(id);
    }

    [Fact]
    public void RaiseDomainEvent_ShouldAddEventToCollection()
    {
        // Arrange
        var aggregate = new TestAggregateRoot();
        var domainEvent = new TestDomainEvent("test data");

        // Act
        aggregate.AddTestEvent(domainEvent);

        // Assert
        aggregate.DomainEvents.Should().HaveCount(1);
        aggregate.DomainEvents.Should().Contain(domainEvent);
        aggregate.ModifiedAt.Should().BeAfter(aggregate.CreatedAt);
    }

    [Fact]
    public void ClearDomainEvents_ShouldRemoveAllEvents()
    {
        // Arrange
        var aggregate = new TestAggregateRoot();
        var domainEvent1 = new TestDomainEvent("test data 1");
        var domainEvent2 = new TestDomainEvent("test data 2");

        aggregate.AddTestEvent(domainEvent1);
        aggregate.AddTestEvent(domainEvent2);

        // Act
        aggregate.ClearDomainEvents();

        // Assert
        aggregate.DomainEvents.Should().BeEmpty();
    }
}
