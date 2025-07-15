using Moq;
using Microsoft.Extensions.Logging;
using Zeus.People.Application.Commands.Academic;
using Zeus.People.Application.Commands.Department;
using Zeus.People.Application.Handlers.Academic;
using Zeus.People.Application.Handlers.Department;
using Zeus.People.Application.Interfaces;
using Zeus.People.Application.Common;
using Zeus.People.Domain.Entities;
using Zeus.People.Domain.ValueObjects;

namespace Zeus.People.Application.Tests;

/// <summary>
/// Tests to verify that command handlers enforce business rules
/// </summary>
public class BusinessRulesEnforcementTests
{
    private readonly Mock<IAcademicRepository> _mockAcademicRepository;
    private readonly Mock<IDepartmentRepository> _mockDepartmentRepository;
    private readonly Mock<IRoomRepository> _mockRoomRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<CreateAcademicCommandHandler>> _mockAcademicLogger;
    private readonly Mock<ILogger<CreateDepartmentCommandHandler>> _mockDepartmentLogger;
    private readonly Mock<ILogger<AssignAcademicToRoomCommandHandler>> _mockAssignmentLogger;

    public BusinessRulesEnforcementTests()
    {
        _mockAcademicRepository = new Mock<IAcademicRepository>();
        _mockDepartmentRepository = new Mock<IDepartmentRepository>();
        _mockRoomRepository = new Mock<IRoomRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockAcademicLogger = new Mock<ILogger<CreateAcademicCommandHandler>>();
        _mockDepartmentLogger = new Mock<ILogger<CreateDepartmentCommandHandler>>();
        _mockAssignmentLogger = new Mock<ILogger<AssignAcademicToRoomCommandHandler>>();
    }

    [Fact]
    public async Task CreateAcademicCommand_ShouldEnforceDuplicateEmpNrRule()
    {
        // Arrange
        var handler = new CreateAcademicCommandHandler(
            _mockAcademicRepository.Object,
            _mockUnitOfWork.Object,
            _mockAcademicLogger.Object);

        var command = new CreateAcademicCommand("AB1234", "John Doe", "P");

        // Setup: Academic with same EmpNr already exists
        _mockAcademicRepository.Setup(x => x.ExistsByEmpNrAsync(command.EmpNr, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Academic.EmpNrExists", result.Error.Code);
        Assert.Contains("already exists", result.Error.Message);
    }

    [Fact]
    public async Task CreateAcademicCommand_ShouldEnforceValidRankRule()
    {
        // Arrange
        var handler = new CreateAcademicCommandHandler(
            _mockAcademicRepository.Object,
            _mockUnitOfWork.Object,
            _mockAcademicLogger.Object);

        // Using invalid rank that would fail domain validation
        var command = new CreateAcademicCommand("CD5678", "John Doe", "INVALID_RANK");

        // Setup: No existing academic with same EmpNr
        _mockAcademicRepository.Setup(x => x.ExistsByEmpNrAsync(command.EmpNr, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(false));

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Academic.InvalidRank", result.Error.Code);
    }

    [Fact]
    public async Task AssignAcademicToRoom_ShouldEnforceRoomOccupancyRule()
    {
        // Arrange
        var handler = new AssignAcademicToRoomCommandHandler(
            _mockAcademicRepository.Object,
            _mockRoomRepository.Object,
            _mockUnitOfWork.Object,
            _mockAssignmentLogger.Object);

        var academicId = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        var command = new AssignAcademicToRoomCommand(academicId, roomId);

        // Setup: Academic exists
        var academic = Academic.Create(EmpNr.Create("AB1234"), EmpName.Create("John Doe"), Rank.Create("P"));
        _mockAcademicRepository.Setup(x => x.GetByIdAsync(academicId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<Academic?>(academic));

        // Setup: Room exists
        var room = Room.Create(RoomNr.Create("101"), Guid.NewGuid());
        _mockRoomRepository.Setup(x => x.GetByIdAsync(roomId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<Room?>(room));

        // Setup: Room is already occupied (business rule violation)
        _mockRoomRepository.Setup(x => x.IsRoomOccupiedAsync(roomId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Room.AlreadyOccupied", result.Error.Code);
        Assert.Contains("already occupied", result.Error.Message);
    }

    [Fact]
    public async Task AssignAcademicToRoom_ShouldEnforceAcademicExistsRule()
    {
        // Arrange
        var handler = new AssignAcademicToRoomCommandHandler(
            _mockAcademicRepository.Object,
            _mockRoomRepository.Object,
            _mockUnitOfWork.Object,
            _mockAssignmentLogger.Object);

        var academicId = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        var command = new AssignAcademicToRoomCommand(academicId, roomId);

        // Setup: Academic does not exist (business rule violation)
        _mockAcademicRepository.Setup(x => x.GetByIdAsync(academicId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<Academic?>(null));

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Academic.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task AssignAcademicToRoom_ShouldEnforceRoomExistsRule()
    {
        // Arrange
        var handler = new AssignAcademicToRoomCommandHandler(
            _mockAcademicRepository.Object,
            _mockRoomRepository.Object,
            _mockUnitOfWork.Object,
            _mockAssignmentLogger.Object);

        var academicId = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        var command = new AssignAcademicToRoomCommand(academicId, roomId);

        // Setup: Academic exists
        var academic = Academic.Create(EmpNr.Create("AB1234"), EmpName.Create("John Doe"), Rank.Create("P"));
        _mockAcademicRepository.Setup(x => x.GetByIdAsync(academicId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<Academic?>(academic));

        // Setup: Room does not exist (business rule violation)
        _mockRoomRepository.Setup(x => x.GetByIdAsync(roomId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<Room?>(null));

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Room.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task CreateDepartmentCommand_ShouldEnforceDuplicateNameRule()
    {
        // Arrange
        var handler = new CreateDepartmentCommandHandler(
            _mockDepartmentRepository.Object,
            _mockUnitOfWork.Object,
            _mockDepartmentLogger.Object);

        var command = new CreateDepartmentCommand("Computer Science");

        // Setup: Department with same name already exists
        _mockDepartmentRepository.Setup(x => x.ExistsByNameAsync(command.Name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Department.NameExists", result.Error.Code);
        Assert.Contains("already exists", result.Error.Message);
    }

    [Fact]
    public async Task CreateAcademicCommand_ShouldAllowValidInput()
    {
        // Arrange
        var handler = new CreateAcademicCommandHandler(
            _mockAcademicRepository.Object,
            _mockUnitOfWork.Object,
            _mockAcademicLogger.Object);

        var command = new CreateAcademicCommand("AB1234", "John Doe", "P");

        // Setup: No existing academic with same EmpNr
        _mockAcademicRepository.Setup(x => x.ExistsByEmpNrAsync(command.EmpNr, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(false));

        // Setup: Repository add succeeds
        var expectedId = Guid.NewGuid();
        _mockAcademicRepository.Setup(x => x.AddAsync(It.IsAny<Academic>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expectedId));

        // Setup: Unit of work save succeeds
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expectedId, result.Value);
    }

    [Fact]
    public async Task AssignAcademicToRoom_ShouldAllowValidAssignment()
    {
        // Arrange
        var handler = new AssignAcademicToRoomCommandHandler(
            _mockAcademicRepository.Object,
            _mockRoomRepository.Object,
            _mockUnitOfWork.Object,
            _mockAssignmentLogger.Object);

        var academicId = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        var command = new AssignAcademicToRoomCommand(academicId, roomId);

        // Setup: Academic exists
        var academic = Academic.Create(EmpNr.Create("AB1234"), EmpName.Create("John Doe"), Rank.Create("P"));
        _mockAcademicRepository.Setup(x => x.GetByIdAsync(academicId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<Academic?>(academic));

        // Setup: Room exists
        var room = Room.Create(RoomNr.Create("101"), Guid.NewGuid());
        _mockRoomRepository.Setup(x => x.GetByIdAsync(roomId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<Room?>(room));

        // Setup: Room is not occupied
        _mockRoomRepository.Setup(x => x.IsRoomOccupiedAsync(roomId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(false));

        // Setup: Repository update succeeds
        _mockAcademicRepository.Setup(x => x.UpdateAsync(It.IsAny<Academic>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Setup: Unit of work save succeeds
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
    }
}
