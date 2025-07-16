using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Zeus.People.Domain.Entities;
using Zeus.People.Domain.ValueObjects;
using Zeus.People.Infrastructure.Persistence;
using Zeus.People.Infrastructure.Persistence.Repositories;

namespace Zeus.People.Infrastructure.Tests.Repositories;

public class AcademicRepositoryTests : IDisposable
{
    private readonly AcademicContext _context;
    private readonly AcademicRepository _repository;

    public AcademicRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AcademicContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AcademicContext(options);
        _repository = new AcademicRepository(_context);
    }

    [Fact]
    public async Task AddAsync_ShouldAddAcademicSuccessfully()
    {
        // Arrange
        var empNr = EmpNr.Create("AB1001");
        var empName = EmpName.Create("John Doe");
        var rank = Rank.Create("P"); // Professor
        var academic = Academic.Create(empNr, empName, rank);

        // Act
        var result = await _repository.AddAsync(academic);
        await _context.SaveChangesAsync(); // Unit of Work pattern

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(academic.Id);

        var savedAcademic = await _context.Academics.FindAsync(academic.Id);
        savedAcademic.Should().NotBeNull();
        savedAcademic!.EmpName.Value.Should().Be("John Doe");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnAcademicWhenExists()
    {
        // Arrange
        var empNr = EmpNr.Create("AB1002");
        var empName = EmpName.Create("Jane Smith");
        var rank = Rank.Create("L"); // Lecturer
        var academic = Academic.Create(empNr, empName, rank);

        _context.Academics.Add(academic);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(academic.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(academic.Id);
        result.Value.EmpName.Value.Should().Be("Jane Smith");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNullWhenNotExists()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistentId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task GetByEmpNrAsync_ShouldReturnAcademicWhenExists()
    {
        // Arrange
        var empNr = EmpNr.Create("AB1003");
        var empName = EmpName.Create("Bob Johnson");
        var rank = Rank.Create("SL"); // Senior Lecturer
        var academic = Academic.Create(empNr, empName, rank);

        _context.Academics.Add(academic);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByEmpNrAsync("AB1003");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.EmpNr.Value.Should().Be("AB1003");
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateAcademicSuccessfully()
    {
        // Arrange
        var empNr = EmpNr.Create("AB1004");
        var empName = EmpName.Create("Alice Brown");
        var rank = Rank.Create("L"); // Lecturer
        var academic = Academic.Create(empNr, empName, rank);

        _context.Academics.Add(academic);
        await _context.SaveChangesAsync();

        // Promote rank
        var newRank = Rank.Create("SL"); // Senior Lecturer
        academic.ChangeRank(newRank);

        // Act
        var result = await _repository.UpdateAsync(academic);
        await _context.SaveChangesAsync(); // Unit of Work pattern

        // Assert
        result.IsSuccess.Should().BeTrue();

        var updatedAcademic = await _context.Academics.FindAsync(academic.Id);
        updatedAcademic.Should().NotBeNull();
        updatedAcademic!.Rank.Value.Should().Be("SL");
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveAcademicSuccessfully()
    {
        // Arrange
        var empNr = EmpNr.Create("AB1005");
        var empName = EmpName.Create("Charlie Wilson");
        var rank = Rank.Create("P"); // Professor
        var academic = Academic.Create(empNr, empName, rank);

        _context.Academics.Add(academic);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.DeleteAsync(academic.Id);
        await _context.SaveChangesAsync(); // Unit of Work pattern

        // Assert
        result.IsSuccess.Should().BeTrue();

        var deletedAcademic = await _context.Academics.FindAsync(academic.Id);
        deletedAcademic.Should().BeNull();
    }

    [Fact]
    public async Task GetByRankAsync_ShouldReturnAcademicsWithSpecifiedRank()
    {
        // Arrange
        var academic1 = Academic.Create(EmpNr.Create("AB1006"), EmpName.Create("Prof One"), Rank.Create("P"));
        var academic2 = Academic.Create(EmpNr.Create("AB1007"), EmpName.Create("Prof Two"), Rank.Create("P"));
        var academic3 = Academic.Create(EmpNr.Create("AB1008"), EmpName.Create("Lect One"), Rank.Create("L"));

        _context.Academics.AddRange(academic1, academic2, academic3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByRankAsync("P");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().AllSatisfy(a => a.Rank.Value.Should().Be("P"));
    }

    [Fact]
    public async Task Academic_WithValueObjects_ShouldPersistCorrectly()
    {
        // Arrange
        var empNr = EmpNr.Create("AB1009");
        var empName = EmpName.Create("Dr. David Lee");
        var rank = Rank.Create("P"); // Professor
        var academic = Academic.Create(empNr, empName, rank);

        // Act
        _context.Academics.Add(academic);
        await _context.SaveChangesAsync();

        // Assert
        var savedAcademic = await _context.Academics.FindAsync(academic.Id);
        savedAcademic.Should().NotBeNull();
        savedAcademic!.EmpNr.Value.Should().Be("AB1009");
        savedAcademic.EmpName.Value.Should().Be("Dr. David Lee");
        savedAcademic.Rank.Value.Should().Be("P");
        savedAcademic.IsTenured.Should().BeFalse();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
