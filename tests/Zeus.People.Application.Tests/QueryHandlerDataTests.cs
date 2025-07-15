using Moq;
using Microsoft.Extensions.Logging;
using Zeus.People.Application.Queries.Academic;
using Zeus.People.Application.Queries.Department;
using Zeus.People.Application.Handlers.Academic;
using Zeus.People.Application.Handlers.Department;
using Zeus.People.Application.Interfaces;
using Zeus.People.Application.Common;
using Zeus.People.Application.DTOs;

namespace Zeus.People.Application.Tests;

/// <summary>
/// Tests to verify that query handlers return correct data
/// </summary>
public class QueryHandlerDataTests
{
    private readonly Mock<IAcademicReadRepository> _mockAcademicReadRepository;
    private readonly Mock<IDepartmentReadRepository> _mockDepartmentReadRepository;
    private readonly Mock<ILogger<GetAcademicQueryHandler>> _mockAcademicLogger;
    private readonly Mock<ILogger<GetDepartmentQueryHandler>> _mockDepartmentLogger;

    public QueryHandlerDataTests()
    {
        _mockAcademicReadRepository = new Mock<IAcademicReadRepository>();
        _mockDepartmentReadRepository = new Mock<IDepartmentReadRepository>();
        _mockAcademicLogger = new Mock<ILogger<GetAcademicQueryHandler>>();
        _mockDepartmentLogger = new Mock<ILogger<GetDepartmentQueryHandler>>();
    }

    [Fact]
    public async Task GetAcademicQuery_ShouldReturnCorrectAcademicData()
    {
        // Arrange
        var handler = new GetAcademicQueryHandler(_mockAcademicReadRepository.Object, _mockAcademicLogger.Object);
        var academicId = Guid.NewGuid();
        var query = new GetAcademicQuery(academicId);

        var expectedAcademic = new AcademicDto
        {
            Id = academicId,
            EmpNr = "AB1234",
            EmpName = "Dr. John Smith",
            Rank = "P",
            IsTenured = true,
            HomePhone = "555-0123",
            DepartmentId = Guid.NewGuid(),
            DepartmentName = "Computer Science",
            RoomId = Guid.NewGuid(),
            RoomNumber = "101",
            ExtensionId = Guid.NewGuid(),
            ExtensionNumber = "1234",
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        _mockAcademicReadRepository.Setup(x => x.GetByIdAsync(academicId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<AcademicDto?>(expectedAcademic));

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(expectedAcademic.Id, result.Value.Id);
        Assert.Equal(expectedAcademic.EmpNr, result.Value.EmpNr);
        Assert.Equal(expectedAcademic.EmpName, result.Value.EmpName);
        Assert.Equal(expectedAcademic.Rank, result.Value.Rank);
        Assert.Equal(expectedAcademic.IsTenured, result.Value.IsTenured);
        Assert.Equal(expectedAcademic.HomePhone, result.Value.HomePhone);
        Assert.Equal(expectedAcademic.DepartmentId, result.Value.DepartmentId);
        Assert.Equal(expectedAcademic.DepartmentName, result.Value.DepartmentName);
        Assert.Equal(expectedAcademic.RoomId, result.Value.RoomId);
        Assert.Equal(expectedAcademic.RoomNumber, result.Value.RoomNumber);
    }

    [Fact]
    public async Task GetAcademicQuery_ShouldReturnNotFoundWhenAcademicDoesNotExist()
    {
        // Arrange
        var handler = new GetAcademicQueryHandler(_mockAcademicReadRepository.Object, _mockAcademicLogger.Object);
        var academicId = Guid.NewGuid();
        var query = new GetAcademicQuery(academicId);

        _mockAcademicReadRepository.Setup(x => x.GetByIdAsync(academicId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<AcademicDto?>(null));

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Academic.NotFound", result.Error.Code);
        Assert.Contains(academicId.ToString(), result.Error.Message);
    }

    [Fact]
    public async Task GetDepartmentQuery_ShouldReturnCorrectDepartmentData()
    {
        // Arrange
        var handler = new GetDepartmentQueryHandler(_mockDepartmentReadRepository.Object, _mockDepartmentLogger.Object);
        var departmentId = Guid.NewGuid();
        var query = new GetDepartmentQuery(departmentId);

        var expectedDepartment = new DepartmentDto
        {
            Id = departmentId,
            Name = "Computer Science",
            ResearchBudget = 100000m,
            TeachingBudget = 75000m,
            HeadProfessorId = Guid.NewGuid(),
            HeadProfessorName = "Prof. Jane Doe",
            TotalAcademics = 25,
            ProfessorCount = 5,
            SeniorLecturerCount = 10,
            LecturerCount = 10,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
            Academics = new List<AcademicSummaryDto>
            {
                new AcademicSummaryDto { Id = Guid.NewGuid(), EmpNr = "AB1234", EmpName = "Dr. Smith", Rank = "P" }
            }
        };

        _mockDepartmentReadRepository.Setup(x => x.GetByIdAsync(departmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<DepartmentDto?>(expectedDepartment));

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(expectedDepartment.Id, result.Value.Id);
        Assert.Equal(expectedDepartment.Name, result.Value.Name);
        Assert.Equal(expectedDepartment.ResearchBudget, result.Value.ResearchBudget);
        Assert.Equal(expectedDepartment.TeachingBudget, result.Value.TeachingBudget);
        Assert.Equal(expectedDepartment.HeadProfessorId, result.Value.HeadProfessorId);
        Assert.Equal(expectedDepartment.HeadProfessorName, result.Value.HeadProfessorName);
        Assert.Equal(expectedDepartment.TotalAcademics, result.Value.TotalAcademics);
        Assert.Equal(expectedDepartment.Academics.Count, result.Value.Academics.Count);
    }

    [Fact]
    public async Task GetDepartmentQuery_ShouldReturnNotFoundWhenDepartmentDoesNotExist()
    {
        // Arrange
        var handler = new GetDepartmentQueryHandler(_mockDepartmentReadRepository.Object, _mockDepartmentLogger.Object);
        var departmentId = Guid.NewGuid();
        var query = new GetDepartmentQuery(departmentId);

        _mockDepartmentReadRepository.Setup(x => x.GetByIdAsync(departmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<DepartmentDto?>(null));

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Department.NotFound", result.Error.Code);
        Assert.Contains(departmentId.ToString(), result.Error.Message);
    }

    [Fact]
    public async Task GetAcademicByEmpNrQuery_ShouldReturnCorrectAcademicData()
    {
        // Arrange
        var handler = new GetAcademicByEmpNrQueryHandler(_mockAcademicReadRepository.Object,
            Mock.Of<ILogger<GetAcademicByEmpNrQueryHandler>>());
        var empNr = "AB1234";
        var query = new GetAcademicByEmpNrQuery(empNr);

        var expectedAcademic = new AcademicDto
        {
            Id = Guid.NewGuid(),
            EmpNr = empNr,
            EmpName = "Dr. Sarah Wilson",
            Rank = "SL",
            IsTenured = false,
            ContractEndDate = DateTime.UtcNow.AddYears(2),
            HomePhone = "555-0456",
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        _mockAcademicReadRepository.Setup(x => x.GetByEmpNrAsync(empNr, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<AcademicDto?>(expectedAcademic));

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(expectedAcademic.EmpNr, result.Value.EmpNr);
        Assert.Equal(expectedAcademic.EmpName, result.Value.EmpName);
        Assert.Equal(expectedAcademic.Rank, result.Value.Rank);
        Assert.Equal(expectedAcademic.IsTenured, result.Value.IsTenured);
        Assert.Equal(expectedAcademic.ContractEndDate, result.Value.ContractEndDate);
    }

    [Fact]
    public async Task GetAllAcademicsQuery_ShouldReturnPagedResults()
    {
        // Arrange
        var handler = new GetAllAcademicsQueryHandler(_mockAcademicReadRepository.Object,
            Mock.Of<ILogger<GetAllAcademicsQueryHandler>>());
        var query = new GetAllAcademicsQuery(PageNumber: 1, PageSize: 2);

        var expectedAcademics = new List<AcademicSummaryDto>
        {
            new AcademicSummaryDto
            {
                Id = Guid.NewGuid(),
                EmpNr = "AB1234",
                EmpName = "Dr. John Smith",
                Rank = "P",
                DepartmentName = "Computer Science"
            },
            new AcademicSummaryDto
            {
                Id = Guid.NewGuid(),
                EmpNr = "CD5678",
                EmpName = "Dr. Jane Doe",
                Rank = "SL",
                DepartmentName = "Mathematics"
            }
        };

        var expectedPagedResult = new PagedResult<AcademicSummaryDto>
        {
            Items = expectedAcademics,
            PageNumber = 1,
            PageSize = 2,
            TotalCount = 10
        };

        _mockAcademicReadRepository.Setup(x => x.GetAllAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<bool?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expectedPagedResult));

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(expectedPagedResult.PageNumber, result.Value.PageNumber);
        Assert.Equal(expectedPagedResult.PageSize, result.Value.PageSize);
        Assert.Equal(expectedPagedResult.TotalCount, result.Value.TotalCount);
        Assert.Equal(5, result.Value.TotalPages); // TotalPages is calculated: 10 total / 2 per page = 5
        Assert.Equal(2, result.Value.Items.Count());

        var firstAcademic = result.Value.Items.First();
        Assert.Equal(expectedAcademics[0].EmpNr, firstAcademic.EmpNr);
        Assert.Equal(expectedAcademics[0].EmpName, firstAcademic.EmpName);
        Assert.Equal(expectedAcademics[0].Rank, firstAcademic.Rank);
    }

    [Fact]
    public async Task GetAcademicsByDepartmentQuery_ShouldReturnCorrectFilteredData()
    {
        // Arrange
        var handler = new GetAcademicsByDepartmentQueryHandler(_mockAcademicReadRepository.Object,
            Mock.Of<ILogger<GetAcademicsByDepartmentQueryHandler>>());
        var departmentId = Guid.NewGuid();
        var query = new GetAcademicsByDepartmentQuery(departmentId, PageNumber: 1, PageSize: 10);

        var expectedAcademics = new List<AcademicSummaryDto>
        {
            new AcademicSummaryDto
            {
                Id = Guid.NewGuid(),
                EmpNr = "AB1234",
                EmpName = "Dr. Computer Scientist",
                Rank = "P",
                DepartmentName = "Computer Science"
            }
        };

        var expectedPagedResult = new PagedResult<AcademicSummaryDto>
        {
            Items = expectedAcademics,
            PageNumber = 1,
            PageSize = 10,
            TotalCount = 1
        };

        _mockAcademicReadRepository.Setup(x => x.GetByDepartmentAsync(
                departmentId, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expectedPagedResult));

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value.Items);
        Assert.Equal("Computer Science", result.Value.Items.First().DepartmentName);
        Assert.Equal("Dr. Computer Scientist", result.Value.Items.First().EmpName);
    }

    [Fact]
    public async Task GetDepartmentByNameQuery_ShouldReturnCorrectDepartmentData()
    {
        // Arrange
        var handler = new GetDepartmentByNameQueryHandler(_mockDepartmentReadRepository.Object,
            Mock.Of<ILogger<GetDepartmentByNameQueryHandler>>());
        var departmentName = "Computer Science";
        var query = new GetDepartmentByNameQuery(departmentName);

        var expectedDepartment = new DepartmentDto
        {
            Id = Guid.NewGuid(),
            Name = departmentName,
            ResearchBudget = 50000m,
            TeachingBudget = 30000m,
            TotalAcademics = 15,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        _mockDepartmentReadRepository.Setup(x => x.GetByNameAsync(departmentName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<DepartmentDto?>(expectedDepartment));

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(departmentName, result.Value.Name);
        Assert.Equal(expectedDepartment.ResearchBudget, result.Value.ResearchBudget);
        Assert.Equal(expectedDepartment.TotalAcademics, result.Value.TotalAcademics);
    }

    [Fact]
    public async Task QueryHandler_ShouldHandleRepositoryFailuresGracefully()
    {
        // Arrange
        var handler = new GetAcademicQueryHandler(_mockAcademicReadRepository.Object, _mockAcademicLogger.Object);
        var academicId = Guid.NewGuid();
        var query = new GetAcademicQuery(academicId);

        var repositoryError = new Error("Repository.ConnectionFailed", "Database connection failed");
        _mockAcademicReadRepository.Setup(x => x.GetByIdAsync(academicId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<AcademicDto?>(repositoryError));

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Repository.ConnectionFailed", result.Error.Code);
        Assert.Equal("Database connection failed", result.Error.Message);
    }

    [Fact]
    public async Task QueryHandler_ShouldValidateDataIntegrity()
    {
        // Arrange
        var handler = new GetAcademicQueryHandler(_mockAcademicReadRepository.Object, _mockAcademicLogger.Object);
        var academicId = Guid.NewGuid();
        var query = new GetAcademicQuery(academicId);

        var academicWithIncompleteData = new AcademicDto
        {
            Id = academicId,
            EmpNr = "AB1234",
            EmpName = "Dr. Test",
            Rank = "P",
            IsTenured = true,
            // Missing optional fields should be handled gracefully
            DepartmentId = null,
            DepartmentName = null,
            RoomId = null,
            RoomNumber = null
        };

        _mockAcademicReadRepository.Setup(x => x.GetByIdAsync(academicId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<AcademicDto?>(academicWithIncompleteData));

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("AB1234", result.Value.EmpNr);
        Assert.Equal("Dr. Test", result.Value.EmpName);
        Assert.Null(result.Value.DepartmentId);
        Assert.Null(result.Value.DepartmentName);
        // Verify that the query handler doesn't fail when optional navigation properties are null
    }
}
