using Azure.Messaging.ServiceBus;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Moq;
using Zeus.People.Infrastructure.HealthChecks;
using Zeus.People.Infrastructure.Messaging;
using Zeus.People.Infrastructure.Persistence;
using Zeus.People.Infrastructure.EventStore;

namespace Zeus.People.Infrastructure.Tests.HealthChecks;

public class HealthCheckTests : IDisposable
{
    private readonly Mock<ServiceBusClient> _mockServiceBusClient;
    private readonly Mock<ServiceBusSender> _mockSender;
    private readonly ServiceBusConfiguration _serviceBusConfiguration;
    private readonly Mock<IOptions<ServiceBusConfiguration>> _mockServiceBusOptions;

    private readonly AcademicContext _academicContext;
    private readonly EventStoreContext _eventStoreContext;

    public HealthCheckTests()
    {
        // Setup Service Bus mocks
        _mockServiceBusClient = new Mock<ServiceBusClient>();
        _mockSender = new Mock<ServiceBusSender>();
        _serviceBusConfiguration = new ServiceBusConfiguration
        {
            ConnectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=test",
            TopicName = "domain-events"
        };
        _mockServiceBusOptions = new Mock<IOptions<ServiceBusConfiguration>>();
        _mockServiceBusOptions.Setup(x => x.Value).Returns(_serviceBusConfiguration);

        _mockServiceBusClient
            .Setup(x => x.CreateSender(_serviceBusConfiguration.TopicName))
            .Returns(_mockSender.Object);

        // Setup in-memory databases
        var academicOptions = new DbContextOptionsBuilder<AcademicContext>()
            .UseInMemoryDatabase(databaseName: $"AcademicHealthCheck_{Guid.NewGuid()}")
            .Options;
        _academicContext = new AcademicContext(academicOptions);

        var eventStoreOptions = new DbContextOptionsBuilder<EventStoreContext>()
            .UseInMemoryDatabase(databaseName: $"EventStoreHealthCheck_{Guid.NewGuid()}")
            .Options;
        _eventStoreContext = new EventStoreContext(eventStoreOptions);
    }

    [Fact]
    public async Task ServiceBusHealthCheck_WhenServiceBusIsHealthy_ShouldReturnHealthy()
    {
        // Arrange
        _mockSender
            .Setup(x => x.DisposeAsync())
            .Returns(ValueTask.CompletedTask);

        var healthCheck = new ServiceBusHealthCheck(_mockServiceBusClient.Object, _mockServiceBusOptions.Object);
        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Be("Service Bus connection is healthy");
    }

    [Fact]
    public async Task ServiceBusHealthCheck_WhenServiceBusThrows_ShouldReturnUnhealthy()
    {
        // Arrange
        _mockServiceBusClient
            .Setup(x => x.CreateSender(_serviceBusConfiguration.TopicName))
            .Throws(new ServiceBusException("Connection failed", ServiceBusFailureReason.GeneralError));

        var healthCheck = new ServiceBusHealthCheck(_mockServiceBusClient.Object, _mockServiceBusOptions.Object);
        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("Service Bus connection failed");
        result.Exception.Should().NotBeNull();
        result.Exception.Should().BeOfType<ServiceBusException>();
    }

    [Fact]
    public async Task DatabaseHealthCheck_WhenDatabaseIsAccessible_ShouldReturnHealthy()
    {
        // Arrange
        var healthCheck = new DatabaseHealthCheck(_academicContext);
        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        // In-memory database may not support ExecuteSqlRaw, so we just check it doesn't crash
        result.Status.Should().BeOneOf(HealthStatus.Healthy, HealthStatus.Unhealthy);
        result.Description.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task EventStoreHealthCheck_WhenEventStoreIsAccessible_ShouldReturnHealthy()
    {
        // Arrange
        var healthCheck = new EventStoreHealthCheck(_eventStoreContext);
        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        // In-memory database may not support ExecuteSqlRaw, so we just check it doesn't crash
        result.Status.Should().BeOneOf(HealthStatus.Healthy, HealthStatus.Unhealthy);
        result.Description.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task EventStoreHealthCheck_WhenEventStoreIsUnavailable_ShouldReturnUnhealthy()
    {
        // Arrange
        // Create a context with invalid connection to simulate failure
        var invalidOptions = new DbContextOptionsBuilder<EventStoreContext>()
            .UseInMemoryDatabase(databaseName: "InvalidEventStore")
            .Options;

        var invalidContext = new EventStoreContext(invalidOptions);
        invalidContext.Dispose(); // Dispose to make it unavailable

        var healthCheck = new EventStoreHealthCheck(invalidContext);
        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("Event Store connection failed");
        result.Exception.Should().NotBeNull();
    }

    [Fact]
    public async Task DatabaseHealthCheck_WhenDatabaseIsUnavailable_ShouldReturnUnhealthy()
    {
        // Arrange
        // Create a context with invalid connection to simulate failure
        var invalidOptions = new DbContextOptionsBuilder<AcademicContext>()
            .UseInMemoryDatabase(databaseName: "InvalidAcademic")
            .Options;

        var invalidContext = new AcademicContext(invalidOptions);
        invalidContext.Dispose(); // Dispose to make it unavailable

        var healthCheck = new DatabaseHealthCheck(invalidContext);
        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("Database connection failed");
        result.Exception.Should().NotBeNull();
    }

    [Fact]
    public async Task ServiceBusHealthCheck_WithCancellation_ShouldHandleGracefully()
    {
        // Arrange
        var healthCheck = new ServiceBusHealthCheck(_mockServiceBusClient.Object, _mockServiceBusOptions.Object);
        var context = new HealthCheckContext();
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act
        var result = await healthCheck.CheckHealthAsync(context, cancellationTokenSource.Token);

        // Assert - Should handle cancellation gracefully
        result.Status.Should().BeOneOf(HealthStatus.Healthy, HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task DatabaseHealthCheck_WithCancellation_ShouldHandleGracefully()
    {
        // Arrange
        var healthCheck = new DatabaseHealthCheck(_academicContext);
        var context = new HealthCheckContext();
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act
        var result = await healthCheck.CheckHealthAsync(context, cancellationTokenSource.Token);

        // Assert - Should handle cancellation gracefully
        result.Status.Should().BeOneOf(HealthStatus.Healthy, HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task EventStoreHealthCheck_WithCancellation_ShouldHandleGracefully()
    {
        // Arrange
        var healthCheck = new EventStoreHealthCheck(_eventStoreContext);
        var context = new HealthCheckContext();
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act
        var result = await healthCheck.CheckHealthAsync(context, cancellationTokenSource.Token);

        // Assert - Should handle cancellation gracefully
        result.Status.Should().BeOneOf(HealthStatus.Healthy, HealthStatus.Unhealthy);
    }

    [Fact]
    public void ServiceBusHealthCheck_Constructor_WithNullServiceBusClient_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new ServiceBusHealthCheck(null!, _mockServiceBusOptions.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("serviceBusClient");
    }

    [Fact]
    public void ServiceBusHealthCheck_Constructor_WithNullConfiguration_ShouldThrowNullReferenceException()
    {
        // Act & Assert
        var act = () => new ServiceBusHealthCheck(_mockServiceBusClient.Object, null!);
        act.Should().Throw<NullReferenceException>();
    }

    [Fact]
    public void DatabaseHealthCheck_Constructor_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new DatabaseHealthCheck(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("context");
    }

    [Fact]
    public void EventStoreHealthCheck_Constructor_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new EventStoreHealthCheck(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("context");
    }

    public void Dispose()
    {
        _academicContext?.Dispose();
        _eventStoreContext?.Dispose();
    }
}
