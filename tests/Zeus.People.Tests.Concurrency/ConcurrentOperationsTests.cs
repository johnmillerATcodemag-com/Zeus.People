using System.Collections.Concurrent;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Zeus.People.Application.Commands.Academic;
using Zeus.People.Application.Commands.Department;
using Zeus.People.Application.Handlers.Academic;
using Zeus.People.Application.Handlers.Department;
using Zeus.People.Application.Interfaces;
using Zeus.People.Domain.Entities;
using Zeus.People.Domain.Events;
using Zeus.People.Domain.ValueObjects;
using Zeus.People.Infrastructure.EventStore;
using Zeus.People.Infrastructure.Persistence;
using Zeus.People.Infrastructure.Persistence.Repositories;

namespace Zeus.People.Tests.Concurrency;

/// <summary>
/// Comprehensive tests for concurrent operations and thread safety
/// </summary>
public class ConcurrentOperationsTests : IDisposable
{
    private readonly AcademicContext _context;
    private readonly EventStoreContext _eventStoreContext;
    private readonly SqlEventStore _eventStore;
    private readonly IAcademicRepository _academicRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ConcurrentOperationsTests()
    {
        // Setup in-memory databases for concurrent testing
        var academicOptions = new DbContextOptionsBuilder<AcademicContext>()
            .UseInMemoryDatabase(databaseName: $"TestAcademicDb_{Guid.NewGuid()}")
            .Options;

        var eventStoreOptions = new DbContextOptionsBuilder<EventStoreContext>()
            .UseInMemoryDatabase(databaseName: $"TestEventStoreDb_{Guid.NewGuid()}")
            .Options;

        _context = new AcademicContext(academicOptions);
        _eventStoreContext = new EventStoreContext(eventStoreOptions);
        _eventStore = new SqlEventStore(_eventStoreContext);

        _academicRepository = new AcademicRepository(_context);
        _departmentRepository = new DepartmentRepository(_context);
        _unitOfWork = new UnitOfWork(_context);

        // Initialize databases
        _context.Database.EnsureCreated();
        _eventStoreContext.Database.EnsureCreated();
    }

    #region Concurrent Academic Operations

    [Fact]
    public async Task ConcurrentAcademicCreation_ShouldHandleMultipleCreationsCorrectly()
    {
        // Arrange
        const int concurrentOperations = 10;
        var tasks = new Task<bool>[concurrentOperations];
        var results = new ConcurrentBag<Guid>();
        var errors = new ConcurrentBag<Exception>();

        // Act - Create multiple academics concurrently
        for (int i = 0; i < concurrentOperations; i++)
        {
            var index = i;
            tasks[i] = Task.Run(async () =>
            {
                try
                {
                    var empNr = EmpNr.Create($"AC{index:D4}");
                    var empName = EmpName.Create($"Academic {index}");
                    var rank = Rank.Create("L");
                    var academic = Academic.Create(empNr, empName, rank);

                    var result = await _academicRepository.AddAsync(academic);
                    await _unitOfWork.SaveChangesAsync();

                    if (result.IsSuccess)
                    {
                        results.Add(result.Value);
                        return true;
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    errors.Add(ex);
                    return false;
                }
            });
        }

        var completedTasks = await Task.WhenAll(tasks);

        // Assert
        var successCount = completedTasks.Count(t => t);
        successCount.Should().Be(concurrentOperations, "All concurrent academic creations should succeed");
        errors.Should().BeEmpty("No errors should occur during concurrent operations");
        results.Should().HaveCount(concurrentOperations, "All academics should be created with unique IDs");
        results.Distinct().Should().HaveCount(concurrentOperations, "All academic IDs should be unique");

        // Verify data integrity
        var academicsInDb = await _context.Academics.CountAsync();
        academicsInDb.Should().Be(concurrentOperations);
    }

    [Fact]
    public async Task ConcurrentAcademicUpdates_ShouldMaintainDataConsistency()
    {
        // Arrange - Create a base academic
        var empNr = EmpNr.Create("AC0001");
        var empName = EmpName.Create("Test Academic");
        var rank = Rank.Create("L");
        var academic = Academic.Create(empNr, empName, rank);

        await _academicRepository.AddAsync(academic);
        await _unitOfWork.SaveChangesAsync();

        const int concurrentUpdates = 5;
        var updateTasks = new Task<bool>[concurrentUpdates];
        var successCount = 0;

        // Act - Attempt concurrent rank changes
        for (int i = 0; i < concurrentUpdates; i++)
        {
            var updateIndex = i;
            updateTasks[i] = Task.Run(async () =>
            {
                try
                {
                    // Simulate different operations
                    var academicResult = await _academicRepository.GetByIdAsync(academic.Id);
                    if (academicResult.IsSuccess && academicResult.Value != null)
                    {
                        var retrievedAcademic = academicResult.Value;

                        // Different updates based on index
                        switch (updateIndex % 3)
                        {
                            case 0:
                                retrievedAcademic.SetHomePhone(PhoneNr.Create($"555-000{updateIndex}"));
                                break;
                            case 1:
                                retrievedAcademic.AssignToDepartment(Guid.NewGuid());
                                break;
                            case 2:
                                retrievedAcademic.AssignToRoom(Guid.NewGuid());
                                break;
                        }

                        await _academicRepository.UpdateAsync(retrievedAcademic);
                        await _unitOfWork.SaveChangesAsync();

                        Interlocked.Increment(ref successCount);
                        return true;
                    }
                    return false;
                }
                catch
                {
                    return false;
                }
            });
        }

        await Task.WhenAll(updateTasks);

        // Assert
        successCount.Should().BeGreaterThan(0, "At least some concurrent updates should succeed");

        // Verify final state consistency
        var finalAcademicResult = await _academicRepository.GetByIdAsync(academic.Id);
        finalAcademicResult.IsSuccess.Should().BeTrue();
        finalAcademicResult.Value.Should().NotBeNull();
    }

    #endregion

    #region Concurrent Department Operations

    [Fact]
    public async Task ConcurrentDepartmentCreation_ShouldPreventDuplicateNames()
    {
        // Arrange
        const string departmentName = "Computer Science";
        const int concurrentAttempts = 5;
        var tasks = new Task<bool>[concurrentAttempts];
        var successResults = new ConcurrentBag<Guid>();
        var failures = new ConcurrentBag<string>();

        // Act - Attempt to create departments with same name concurrently
        for (int i = 0; i < concurrentAttempts; i++)
        {
            tasks[i] = Task.Run(async () =>
            {
                try
                {
                    // Check if department exists
                    var existsResult = await _departmentRepository.ExistsByNameAsync(departmentName);
                    if (existsResult.IsSuccess && !existsResult.Value)
                    {
                        var department = Department.Create(departmentName);
                        var result = await _departmentRepository.AddAsync(department);

                        if (result.IsSuccess)
                        {
                            await _unitOfWork.SaveChangesAsync();
                            successResults.Add(result.Value);
                            return true;
                        }
                    }

                    failures.Add("Department already exists or creation failed");
                    return false;
                }
                catch (Exception ex)
                {
                    failures.Add(ex.Message);
                    return false;
                }
            });
        }

        await Task.WhenAll(tasks);

        // Assert
        // Only one department should be created successfully due to name uniqueness
        successResults.Should().HaveCount(1, "Only one department with the same name should be created");

        var departmentsInDb = await _context.Departments
            .Where(d => d.Name == departmentName)
            .CountAsync();
        departmentsInDb.Should().Be(1, "Database should contain only one department with the given name");
    }

    #endregion

    #region Event Store Concurrency Tests

    [Fact]
    public async Task ConcurrentEventStoreOperations_ShouldMaintainConsistency()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();
        const string aggregateType = "Academic";
        const int concurrentOperations = 10;

        var empNr = EmpNr.Create("AB1234");
        var empName = EmpName.Create("John Doe");
        var initialRank = Rank.Create("L");

        // Create initial event
        var initialEvent = new AcademicCreatedEvent(aggregateId, empNr, empName, initialRank);
        await _eventStore.AppendEventsAsync(aggregateId, aggregateType, new[] { initialEvent }, 0);

        var tasks = new Task<bool>[concurrentOperations];
        var successCount = 0;
        var conflictCount = 0;

        // Act - Attempt concurrent event appends
        for (int i = 0; i < concurrentOperations; i++)
        {
            var operationIndex = i;
            tasks[i] = Task.Run(async () =>
            {
                try
                {
                    var currentEvents = await _eventStore.GetEventsAsync(aggregateId);
                    var currentVersion = currentEvents.Count();

                    var newRank = Rank.Create(operationIndex % 2 == 0 ? "SL" : "P");
                    var rankChangeEvent = new AcademicRankChangedEvent(aggregateId, initialRank, newRank);

                    await _eventStore.AppendEventsAsync(aggregateId, aggregateType, new[] { rankChangeEvent }, currentVersion);

                    Interlocked.Increment(ref successCount);
                    return true;
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("Concurrency conflict"))
                {
                    Interlocked.Increment(ref conflictCount);
                    return false;
                }
                catch
                {
                    return false;
                }
            });
        }

        await Task.WhenAll(tasks);

        // Assert
        successCount.Should().BeGreaterThan(0, "At least some operations should succeed");
        conflictCount.Should().BeGreaterThan(0, "Concurrency conflicts should be detected");
        (successCount + conflictCount).Should().Be(concurrentOperations, "All operations should either succeed or fail with concurrency conflict");

        // Verify event store consistency
        var finalEvents = await _eventStore.GetEventsAsync(aggregateId);
        var finalVersion = finalEvents.Count();
        finalVersion.Should().Be(successCount + 1, "Final version should match successful operations plus initial event");
    }

    [Fact]
    public async Task ConcurrentEventStoreReads_ShouldReturnConsistentData()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();
        const string aggregateType = "Academic";
        const int concurrentReads = 20;

        // Create some events first
        var empNr = EmpNr.Create("AB5678");
        var empName = EmpName.Create("Jane Doe");
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

        var readTasks = new Task<int>[concurrentReads];
        var eventCounts = new ConcurrentBag<int>();

        // Act - Concurrent reads
        for (int i = 0; i < concurrentReads; i++)
        {
            readTasks[i] = Task.Run(async () =>
            {
                var retrievedEvents = await _eventStore.GetEventsAsync(aggregateId);
                var count = retrievedEvents.Count();
                eventCounts.Add(count);
                return count;
            });
        }

        var results = await Task.WhenAll(readTasks);

        // Assert
        results.Should().AllSatisfy(count => count.Should().Be(3, "All concurrent reads should return the same number of events"));
        eventCounts.Should().AllSatisfy(c => c.Should().Be(3, "All event counts should be consistent"));
    }

    #endregion

    #region Command Handler Concurrency

    [Fact]
    public async Task ConcurrentCommandHandlers_ShouldMaintainBusinessRules()
    {
        // Arrange
        const int concurrentCommands = 5;
        var departmentName = "Engineering";

        // Create a department first
        var department = Department.Create(departmentName);
        await _departmentRepository.AddAsync(department);
        await _unitOfWork.SaveChangesAsync();

        var createAcademicHandler = new CreateAcademicCommandHandler(
            _academicRepository,
            _unitOfWork,
            NullLogger<CreateAcademicCommandHandler>.Instance);

        var tasks = new Task[concurrentCommands];
        var successCount = 0;
        var errorCount = 0;

        // Act - Execute concurrent create academic commands
        for (int i = 0; i < concurrentCommands; i++)
        {
            var index = i;
            tasks[i] = Task.Run(async () =>
            {
                try
                {
                    var command = new CreateAcademicCommand($"EMP{index:D3}", $"Academic {index}", "L");
                    var result = await createAcademicHandler.Handle(command, CancellationToken.None);

                    if (result.IsSuccess)
                    {
                        Interlocked.Increment(ref successCount);
                    }
                    else
                    {
                        Interlocked.Increment(ref errorCount);
                    }
                }
                catch
                {
                    Interlocked.Increment(ref errorCount);
                }
            });
        }

        await Task.WhenAll(tasks);

        // Assert
        successCount.Should().Be(concurrentCommands, "All valid commands should succeed");
        errorCount.Should().Be(0, "No errors should occur for valid concurrent commands");

        // Verify data consistency
        var academicsInDb = await _context.Academics.CountAsync();
        academicsInDb.Should().Be(concurrentCommands);
    }

    #endregion

    #region Transaction Concurrency

    [Fact]
    public async Task ConcurrentTransactions_ShouldMaintainACIDProperties()
    {
        // Arrange
        const int concurrentTransactions = 3;
        var tasks = new Task<bool>[concurrentTransactions];
        var successfulTransactions = 0;

        // Act - Execute concurrent transactions
        for (int i = 0; i < concurrentTransactions; i++)
        {
            var transactionIndex = i;
            tasks[i] = Task.Run(async () =>
            {
                try
                {
                    await _unitOfWork.BeginTransactionAsync();

                    // Create department
                    var department = Department.Create($"Department {transactionIndex}");
                    await _departmentRepository.AddAsync(department);

                    // Create academic
                    var empNr = EmpNr.Create($"TX{transactionIndex:D3}");
                    var empName = EmpName.Create($"Academic TX {transactionIndex}");
                    var rank = Rank.Create("L");
                    var academic = Academic.Create(empNr, empName, rank);
                    academic.AssignToDepartment(department.Id);

                    await _academicRepository.AddAsync(academic);
                    await _unitOfWork.SaveChangesAsync();
                    await _unitOfWork.CommitTransactionAsync();

                    Interlocked.Increment(ref successfulTransactions);
                    return true;
                }
                catch
                {
                    try
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                    }
                    catch
                    {
                        // Ignore rollback errors for this test
                    }
                    return false;
                }
            });
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        successfulTransactions.Should().Be(concurrentTransactions, "All transactions should complete successfully");

        // Verify consistency
        var departmentCount = await _context.Departments.CountAsync();
        var academicCount = await _context.Academics.CountAsync();

        departmentCount.Should().Be(concurrentTransactions);
        academicCount.Should().Be(concurrentTransactions);
    }

    #endregion

    #region Race Condition Tests

    [Fact]
    public async Task RaceCondition_AcademicDepartmentAssignment_ShouldMaintainConsistency()
    {
        // Arrange - Create academic and multiple departments
        var empNr = EmpNr.Create("RC001");
        var empName = EmpName.Create("Race Condition Academic");
        var rank = Rank.Create("P");
        var academic = Academic.Create(empNr, empName, rank);

        await _academicRepository.AddAsync(academic);
        await _unitOfWork.SaveChangesAsync();

        var departments = new List<Department>();
        for (int i = 0; i < 3; i++)
        {
            var dept = Department.Create($"Race Department {i}");
            departments.Add(dept);
            await _departmentRepository.AddAsync(dept);
        }
        await _unitOfWork.SaveChangesAsync();

        const int concurrentAssignments = 3;
        var assignmentTasks = new Task<bool>[concurrentAssignments];

        // Act - Concurrent department assignments
        for (int i = 0; i < concurrentAssignments; i++)
        {
            var deptIndex = i;
            assignmentTasks[i] = Task.Run(async () =>
            {
                try
                {
                    var academicResult = await _academicRepository.GetByIdAsync(academic.Id);
                    if (academicResult.IsSuccess && academicResult.Value != null)
                    {
                        var retrievedAcademic = academicResult.Value;
                        retrievedAcademic.AssignToDepartment(departments[deptIndex].Id);

                        await _academicRepository.UpdateAsync(retrievedAcademic);
                        await _unitOfWork.SaveChangesAsync();
                        return true;
                    }
                    return false;
                }
                catch
                {
                    return false;
                }
            });
        }

        var results = await Task.WhenAll(assignmentTasks);

        // Assert
        var successCount = results.Count(r => r);
        successCount.Should().BeGreaterThan(0, "At least one assignment should succeed");

        // Verify final state consistency
        var finalAcademicResult = await _academicRepository.GetByIdAsync(academic.Id);
        finalAcademicResult.IsSuccess.Should().BeTrue();
        finalAcademicResult.Value!.DepartmentId.Should().NotBeNull("Academic should be assigned to a department");

        // Verify the assigned department exists
        var assignedDeptId = finalAcademicResult.Value!.DepartmentId!.Value;
        departments.Select(d => d.Id).Should().Contain(assignedDeptId, "Academic should be assigned to one of the created departments");
    }

    #endregion

    #region Performance Under Concurrency

    [Fact]
    public async Task HighConcurrencyLoad_ShouldMaintainPerformanceAndConsistency()
    {
        // Arrange
        const int highConcurrencyLevel = 50;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var tasks = new Task<bool>[highConcurrencyLevel];
        var successCount = 0;

        // Act - High concurrency load test
        for (int i = 0; i < highConcurrencyLevel; i++)
        {
            var operationIndex = i;
            tasks[i] = Task.Run(async () =>
            {
                try
                {
                    // Mix different operations
                    switch (operationIndex % 4)
                    {
                        case 0: // Create Academic
                            var empNr = EmpNr.Create($"HC{operationIndex:D3}");
                            var empName = EmpName.Create($"High Concurrency Academic {operationIndex}");
                            var rank = Rank.Create("L");
                            var academic = Academic.Create(empNr, empName, rank);
                            await _academicRepository.AddAsync(academic);
                            break;

                        case 1: // Create Department
                            var department = Department.Create($"HC Department {operationIndex}");
                            await _departmentRepository.AddAsync(department);
                            break;

                        case 2: // Read Operations
                            await _academicRepository.GetByRankAsync("L");
                            await _departmentRepository.GetAllAsync();
                            break;

                        case 3: // Event Store Operations
                            var aggregateId = Guid.NewGuid();
                            var createEvent = new AcademicCreatedEvent(
                                aggregateId,
                                EmpNr.Create($"ES{operationIndex:D3}"),
                                EmpName.Create($"Event Store Academic {operationIndex}"),
                                Rank.Create("L"));
                            await _eventStore.AppendEventsAsync(aggregateId, "Academic", new[] { createEvent }, 0);
                            break;
                    }

                    await _unitOfWork.SaveChangesAsync();
                    Interlocked.Increment(ref successCount);
                    return true;
                }
                catch
                {
                    return false;
                }
            });
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        successCount.Should().BeGreaterThan((int)(highConcurrencyLevel * 0.8),
            "At least 80% of high concurrency operations should succeed");

        stopwatch.ElapsedMilliseconds.Should().BeLessThan(30000,
            "High concurrency operations should complete within 30 seconds");

        // Verify no data corruption
        var totalAcademics = await _context.Academics.CountAsync();
        var totalDepartments = await _context.Departments.CountAsync();
        var totalEvents = await _eventStoreContext.Events.CountAsync();

        (totalAcademics + totalDepartments + totalEvents).Should().BeGreaterThan(0,
            "Data should be persisted correctly under high concurrency");
    }

    #endregion

    public void Dispose()
    {
        _context.Dispose();
        _eventStoreContext.Dispose();
    }
}
