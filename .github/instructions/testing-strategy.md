# Testing Strategy Instructions

## Overview

Comprehensive testing strategy for the Academic Management System CQRS application ensuring business rule compliance, system reliability, and performance requirements.

## Testing Pyramid

### Unit Tests (70%)

- **Domain Logic**: Business rules and entity validation
- **Value Objects**: Input validation and constraints
- **Command/Query Handlers**: Business logic without infrastructure
- **Event Handlers**: Event processing logic
- **Validation**: FluentValidation rules

### Integration Tests (20%)

- **Database Operations**: Entity Framework operations
- **Message Bus**: Service Bus integration
- **API Endpoints**: Controller integration
- **External Services**: Third-party API integration

### End-to-End Tests (10%)

- **Complete Workflows**: Full business scenarios
- **UI Integration**: Frontend-backend integration
- **Performance Tests**: Load and stress testing
- **Security Tests**: Authentication and authorization

## Testing Framework Setup

### Test Project Structure

```
tests/
├── Zeus.People.Domain.Tests/
│   ├── Entities/
│   ├── ValueObjects/
│   ├── Events/
│   └── TestHelpers/
├── Zeus.People.Application.Tests/
│   ├── Commands/
│   ├── Queries/
│   ├── Handlers/
│   └── Validators/
├── Zeus.People.Infrastructure.Tests/
│   ├── Persistence/
│   ├── EventStore/
│   ├── Messaging/
│   └── Configuration/
└── Zeus.People.API.Tests/
    ├── Controllers/
    ├── Integration/
    └── E2E/
```

### Test Base Classes

#### Domain Test Base

```csharp
public abstract class DomainTestBase
{
    protected readonly ITestOutputHelper Output;

    protected DomainTestBase(ITestOutputHelper output)
    {
        Output = output;
    }

    protected static Academic CreateValidAcademic(int empNr = 715, string empName = "Adams A")
    {
        return Academic.Create(
            EmpNr.Create(empNr),
            EmpName.Create(empName),
            Rank.Create("L"),
            Department.Create("Computer Science"));
    }
}
```

#### Integration Test Base

```csharp
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected readonly ITestOutputHelper Output;
    protected readonly TestApplication App;
    protected readonly HttpClient Client;

    protected IntegrationTestBase(ITestOutputHelper output)
    {
        Output = output;
        App = new TestApplication();
        Client = App.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await App.ResetDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        await App.DisposeAsync();
    }
}
```

## Business Rule Testing

### Academic Entity Tests

```csharp
[Fact]
public void Academic_WithValidData_ShouldCreateSuccessfully()
{
    // Arrange
    var empNr = EmpNr.Create(715);
    var empName = EmpName.Create("Adams A");
    var rank = Rank.Create("L");
    var department = Department.Create("Computer Science");

    // Act
    var academic = Academic.Create(empNr, empName, rank, department);

    // Assert
    academic.Should().NotBeNull();
    academic.EmpNr.Should().Be(empNr);
    academic.EmpName.Should().Be(empName);
    academic.Rank.Should().Be(rank);
}

[Fact]
public void Academic_WithTenureAndContractEndDate_ShouldThrowException()
{
    // Arrange & Act & Assert
    var action = () => Academic.Create(
        EmpNr.Create(139),
        EmpName.Create("Test Academic"),
        Rank.Create("P"),
        Department.Create("Computer Science"),
        contractEnd: DateTime.Parse("01/31/95"),
        isTenured: true);

    action.Should().Throw<DomainException>()
        .WithMessage("Academic who is tenured must not have a Date indicating their contract end");
}
```

### Department Business Rules Tests

```csharp
[Fact]
public void Department_WithProfessorHead_ShouldWorkForDepartment()
{
    // Arrange
    var department = Department.Create("Computer Science");
    var professor = CreateProfessor("Codd EF");

    // Act
    department.SetHead(professor);
    professor.AssignToDepartment(department);

    // Assert
    department.Head.Should().Be(professor);
    professor.Department.Should().Be(department);
}

[Fact]
public void Department_ProfessorCount_ShouldMatchQuantity()
{
    // Arrange
    var department = Department.Create("Computer Science");
    var professors = CreateProfessors(5);

    // Act
    foreach (var professor in professors)
    {
        professor.AssignToDepartment(department);
    }

    // Assert
    department.GetProfessorCount().Should().Be(5);
}
```

## Command Handler Testing

### Create Academic Command Tests

```csharp
[Fact]
public async Task CreateAcademicHandler_WithValidCommand_ShouldCreateAcademic()
{
    // Arrange
    var command = new CreateAcademicCommand
    {
        EmpNr = 715,
        EmpName = "Adams A",
        Rank = "L",
        DepartmentName = "Computer Science",
        RoomNr = "69-301",
        ExtNr = "2345"
    };

    var handler = new CreateAcademicHandler(
        _mockRepository.Object,
        _mockEventBus.Object,
        _mockLogger.Object);

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeTrue();
    result.Value.Should().Be(715);

    _mockRepository.Verify(r => r.AddAsync(
        It.Is<Academic>(a => a.EmpNr.Value == 715)), Times.Once);

    _mockEventBus.Verify(e => e.PublishAsync(
        It.IsAny<AcademicCreatedEvent>()), Times.Once);
}

[Fact]
public async Task CreateAcademicHandler_WithDuplicateEmpNr_ShouldReturnFailure()
{
    // Arrange
    var command = new CreateAcademicCommand { EmpNr = 715, /* ... */ };

    _mockRepository.Setup(r => r.GetByEmpNrAsync(715))
        .ReturnsAsync(CreateValidAcademic(715));

    var handler = new CreateAcademicHandler(
        _mockRepository.Object,
        _mockEventBus.Object,
        _mockLogger.Object);

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsFailure.Should().BeTrue();
    result.Error.Should().Contain("already exists");
}
```

## Query Handler Testing

### Get Academic Query Tests

```csharp
[Fact]
public async Task GetAcademicHandler_WithExistingEmpNr_ShouldReturnAcademic()
{
    // Arrange
    var query = new GetAcademicQuery { EmpNr = 715 };
    var expectedAcademic = new AcademicDto
    {
        EmpNr = 715,
        EmpName = "Adams A",
        Rank = "L",
        DepartmentName = "Computer Science"
    };

    _mockReadRepository.Setup(r => r.GetAcademicAsync(715))
        .ReturnsAsync(expectedAcademic);

    var handler = new GetAcademicHandler(_mockReadRepository.Object);

    // Act
    var result = await handler.Handle(query, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.EmpNr.Should().Be(715);
    result.EmpName.Should().Be("Adams A");
}
```

## Integration Testing

### Database Integration Tests

```csharp
[Fact]
public async Task AcademicRepository_SaveAndRetrieve_ShouldPersistCorrectly()
{
    // Arrange
    using var context = CreateDbContext();
    var repository = new AcademicRepository(context);
    var academic = CreateValidAcademic(715);

    // Act
    await repository.AddAsync(academic);
    await context.SaveChangesAsync();

    var retrieved = await repository.GetByEmpNrAsync(715);

    // Assert
    retrieved.Should().NotBeNull();
    retrieved.EmpNr.Value.Should().Be(715);
    retrieved.EmpName.Value.Should().Be("Adams A");
}
```

### API Integration Tests

```csharp
[Fact]
public async Task CreateAcademic_WithValidData_ShouldReturn201()
{
    // Arrange
    var request = new CreateAcademicRequest
    {
        EmpNr = 715,
        EmpName = "Adams A",
        Rank = "L",
        DepartmentName = "Computer Science"
    };

    // Act
    var response = await Client.PostAsJsonAsync("/api/academics", request);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Created);

    var location = response.Headers.Location;
    location.Should().NotBeNull();
    location.ToString().Should().Contain("/api/academics/715");
}
```

## Event Sourcing Testing

### Event Store Tests

```csharp
[Fact]
public async Task EventStore_SaveAndRetrieveEvents_ShouldMaintainOrder()
{
    // Arrange
    var events = new List<DomainEvent>
    {
        new AcademicCreatedEvent(715, "Adams A"),
        new AcademicDepartmentChangedEvent(715, "Mathematics")
    };

    // Act
    await _eventStore.SaveEventsAsync("Academic-715", events, 0);
    var retrievedEvents = await _eventStore.GetEventsAsync("Academic-715");

    // Assert
    retrievedEvents.Should().HaveCount(2);
    retrievedEvents.First().Should().BeOfType<AcademicCreatedEvent>();
    retrievedEvents.Last().Should().BeOfType<AcademicDepartmentChangedEvent>();
}
```

## Performance Testing

### Load Testing Configuration

```csharp
[Fact]
public async Task CreateAcademic_Under1000ConcurrentRequests_ShouldMaintainPerformance()
{
    // Arrange
    var tasks = new List<Task<HttpResponseMessage>>();
    var stopwatch = Stopwatch.StartNew();

    // Act
    for (int i = 0; i < 1000; i++)
    {
        var request = new CreateAcademicRequest
        {
            EmpNr = 1000 + i,
            EmpName = $"Academic {i}",
            Rank = "L",
            DepartmentName = "Computer Science"
        };

        tasks.Add(Client.PostAsJsonAsync("/api/academics", request));
    }

    var responses = await Task.WhenAll(tasks);
    stopwatch.Stop();

    // Assert
    responses.Should().OnlyContain(r =>
        r.StatusCode == HttpStatusCode.Created ||
        r.StatusCode == HttpStatusCode.Conflict);

    stopwatch.ElapsedMilliseconds.Should().BeLessThan(30000); // 30 seconds
}
```

## Test Data Management

### Test Data Builders

```csharp
public class AcademicTestDataBuilder
{
    private int _empNr = 715;
    private string _empName = "Adams A";
    private string _rank = "L";
    private string _department = "Computer Science";

    public AcademicTestDataBuilder WithEmpNr(int empNr)
    {
        _empNr = empNr;
        return this;
    }

    public AcademicTestDataBuilder WithRank(string rank)
    {
        _rank = rank;
        return this;
    }

    public Academic Build()
    {
        return Academic.Create(
            EmpNr.Create(_empNr),
            EmpName.Create(_empName),
            Rank.Create(_rank),
            Department.Create(_department));
    }
}
```

### Test Database Management

```csharp
public class TestDatabaseFixture : IAsyncLifetime
{
    public string ConnectionString { get; private set; }

    public async Task InitializeAsync()
    {
        // Create test database
        var databaseName = $"TestDb_{Guid.NewGuid():N}";
        ConnectionString = $"Server=(localdb)\\MSSQLLocalDB;Database={databaseName};Trusted_Connection=true";

        using var context = new AcademicContext(
            new DbContextOptionsBuilder<AcademicContext>()
                .UseSqlServer(ConnectionString)
                .Options);

        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        // Clean up test database
        using var context = new AcademicContext(
            new DbContextOptionsBuilder<AcademicContext>()
                .UseSqlServer(ConnectionString)
                .Options);

        await context.Database.EnsureDeletedAsync();
    }
}
```

## Test Execution and Reporting

### Test Execution Commands

```powershell
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test category
dotnet test --filter Category=Unit

# Run tests in parallel
dotnet test --parallel
```

### Continuous Integration Testing

```yaml
# Azure DevOps Pipeline
- task: DotNetCoreCLI@2
  displayName: "Run Unit Tests"
  inputs:
    command: "test"
    projects: "**/*Tests.csproj"
    arguments: '--configuration Release --collect:"XPlat Code Coverage" --logger trx --results-directory $(Agent.TempDirectory)'

- task: PublishTestResults@2
  displayName: "Publish Test Results"
  inputs:
    testResultsFormat: "VSTest"
    testResultsFiles: "**/*.trx"
    searchFolder: "$(Agent.TempDirectory)"
```

## Test Quality Metrics

### Coverage Requirements

- **Unit Tests**: Minimum 80% code coverage
- **Integration Tests**: Critical path coverage
- **Business Rules**: 100% coverage for all rules

### Performance Benchmarks

- **API Response Time**: < 200ms for 95th percentile
- **Database Queries**: < 100ms for simple operations
- **Event Processing**: < 50ms per event

### Test Maintenance

- Regular test review and cleanup
- Test data management and cleanup
- Performance test baseline maintenance
- Flaky test identification and resolution
