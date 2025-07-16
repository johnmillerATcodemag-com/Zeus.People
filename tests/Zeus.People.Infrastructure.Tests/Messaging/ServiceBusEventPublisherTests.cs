using Azure.Messaging.ServiceBus;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Zeus.People.Domain.Events;
using Zeus.People.Domain.ValueObjects;
using Zeus.People.Infrastructure.Messaging;

namespace Zeus.People.Infrastructure.Tests.Messaging;

public class ServiceBusEventPublisherTests : IDisposable
{
    private readonly Mock<ServiceBusClient> _mockServiceBusClient;
    private readonly Mock<ServiceBusSender> _mockSender;
    private readonly Mock<ILogger<ServiceBusEventPublisher>> _mockLogger;
    private readonly ServiceBusConfiguration _configuration;
    private readonly ServiceBusEventPublisher _eventPublisher;

    public ServiceBusEventPublisherTests()
    {
        _mockServiceBusClient = new Mock<ServiceBusClient>();
        _mockSender = new Mock<ServiceBusSender>();
        _mockLogger = new Mock<ILogger<ServiceBusEventPublisher>>();

        _configuration = new ServiceBusConfiguration
        {
            ConnectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=test",
            TopicName = "domain-events"
        };

        var options = Options.Create(_configuration);

        _mockServiceBusClient
            .Setup(x => x.CreateSender(_configuration.TopicName))
            .Returns(_mockSender.Object);

        _eventPublisher = new ServiceBusEventPublisher(_mockServiceBusClient.Object, options, _mockLogger.Object);
    }

    [Fact]
    public async Task PublishAsync_SingleEvent_ShouldSendMessageToServiceBus()
    {
        // Arrange
        var academicId = Guid.NewGuid();
        var empNr = EmpNr.Create("AB1234");
        var empName = EmpName.Create("John Doe");
        var rank = Rank.Create("P");

        var domainEvent = new AcademicCreatedEvent(academicId, empNr, empName, rank);

        _mockSender
            .Setup(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _eventPublisher.PublishAsync(domainEvent);

        // Assert
        _mockSender.Verify(
            x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishAsync_SingleEvent_ShouldCreateMessageWithCorrectProperties()
    {
        // Arrange
        var academicId = Guid.NewGuid();
        var empNr = EmpNr.Create("AB1234");
        var empName = EmpName.Create("John Doe");
        var rank = Rank.Create("P");

        var domainEvent = new AcademicCreatedEvent(academicId, empNr, empName, rank);
        ServiceBusMessage? capturedMessage = null;

        _mockSender
            .Setup(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Callback<ServiceBusMessage, CancellationToken>((message, ct) => capturedMessage = message)
            .Returns(Task.CompletedTask);

        // Act
        await _eventPublisher.PublishAsync(domainEvent);

        // Assert
        capturedMessage.Should().NotBeNull();
        capturedMessage!.MessageId.Should().Be(domainEvent.EventId.ToString());
        capturedMessage.Subject.Should().Be(nameof(AcademicCreatedEvent));
        capturedMessage.ApplicationProperties["EventType"].Should().Be(nameof(AcademicCreatedEvent));
        capturedMessage.ApplicationProperties["EventId"].Should().Be(domainEvent.EventId.ToString());
        capturedMessage.ApplicationProperties["Version"].Should().Be(domainEvent.Version);
        capturedMessage.ApplicationProperties["OccurredAt"].Should().Be(domainEvent.OccurredAt.ToString("O"));
        capturedMessage.Body.ToString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task PublishAsync_MultipleEvents_ShouldSendBatchToServiceBus()
    {
        // Arrange
        var academicId = Guid.NewGuid();
        var empNr = EmpNr.Create("AB1234");
        var empName = EmpName.Create("John Doe");
        var oldRank = Rank.Create("L");
        var newRank = Rank.Create("SL");

        var events = new List<IDomainEvent>
        {
            new AcademicCreatedEvent(academicId, empNr, empName, oldRank),
            new AcademicRankChangedEvent(academicId, oldRank, newRank)
        };

        _mockSender
            .Setup(x => x.SendMessagesAsync(It.IsAny<IEnumerable<ServiceBusMessage>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _eventPublisher.PublishAsync(events);

        // Assert
        _mockSender.Verify(
            x => x.SendMessagesAsync(It.Is<IEnumerable<ServiceBusMessage>>(msgs => msgs.Count() == 2), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishAsync_WithCancellationToken_ShouldPassTokenToSender()
    {
        // Arrange
        var academicId = Guid.NewGuid();
        var empNr = EmpNr.Create("AB1234");
        var empName = EmpName.Create("John Doe");
        var rank = Rank.Create("P");

        var domainEvent = new AcademicCreatedEvent(academicId, empNr, empName, rank);
        var cancellationToken = new CancellationToken();

        _mockSender
            .Setup(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), cancellationToken))
            .Returns(Task.CompletedTask);

        // Act
        await _eventPublisher.PublishAsync(domainEvent, cancellationToken);

        // Assert
        _mockSender.Verify(
            x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task PublishAsync_WhenServiceBusThrows_ShouldLogErrorAndRethrow()
    {
        // Arrange
        var academicId = Guid.NewGuid();
        var empNr = EmpNr.Create("AB1234");
        var empName = EmpName.Create("John Doe");
        var rank = Rank.Create("P");

        var domainEvent = new AcademicCreatedEvent(academicId, empNr, empName, rank);
        var expectedException = new ServiceBusException("Service Bus error", ServiceBusFailureReason.GeneralError);

        _mockSender
            .Setup(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var act = async () => await _eventPublisher.PublishAsync(domainEvent);

        await act.Should().ThrowAsync<ServiceBusException>()
            .WithMessage("*Service Bus error*");

        // Verify error was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to publish event")),
                expectedException,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishAsync_DepartmentEvent_ShouldSendCorrectMessage()
    {
        // Arrange
        var departmentId = Guid.NewGuid();
        var domainEvent = new DepartmentCreatedEvent(departmentId, "Computer Science");
        ServiceBusMessage? capturedMessage = null;

        _mockSender
            .Setup(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Callback<ServiceBusMessage, CancellationToken>((message, ct) => capturedMessage = message)
            .Returns(Task.CompletedTask);

        // Act
        await _eventPublisher.PublishAsync(domainEvent);

        // Assert
        capturedMessage.Should().NotBeNull();
        capturedMessage!.Subject.Should().Be(nameof(DepartmentCreatedEvent));
        capturedMessage.ApplicationProperties["EventType"].Should().Be(nameof(DepartmentCreatedEvent));
        capturedMessage.ApplicationProperties["EventId"].Should().Be(domainEvent.EventId.ToString());
    }

    [Fact]
    public async Task PublishAsync_EmptyEventCollection_ShouldNotCallServiceBus()
    {
        // Arrange
        var emptyEvents = new List<IDomainEvent>();

        // Act
        await _eventPublisher.PublishAsync(emptyEvents);

        // Assert
        _mockSender.Verify(
            x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _mockSender.Verify(
            x => x.SendMessagesAsync(It.IsAny<IEnumerable<ServiceBusMessage>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PublishAsync_ShouldLogSuccessfulPublication()
    {
        // Arrange
        var academicId = Guid.NewGuid();
        var empNr = EmpNr.Create("AB1234");
        var empName = EmpName.Create("John Doe");
        var rank = Rank.Create("P");

        var domainEvent = new AcademicCreatedEvent(academicId, empNr, empName, rank);

        _mockSender
            .Setup(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _eventPublisher.PublishAsync(domainEvent);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Published event") &&
                                             v.ToString()!.Contains(nameof(AcademicCreatedEvent)) &&
                                             v.ToString()!.Contains(domainEvent.EventId.ToString())),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_WithNullServiceBusClient_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        var act = () => new ServiceBusEventPublisher(
            null!,
            Options.Create(_configuration),
            _mockLogger.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("serviceBusClient");
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        var act = () => new ServiceBusEventPublisher(
            _mockServiceBusClient.Object,
            Options.Create<ServiceBusConfiguration>(null!),
            _mockLogger.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("configuration");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        var act = () => new ServiceBusEventPublisher(
            _mockServiceBusClient.Object,
            Options.Create(_configuration),
            null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    public void Dispose()
    {
        _eventPublisher?.DisposeAsync().AsTask().Wait();
    }
}
