using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Zeus.People.Domain.Entities;
using Zeus.People.Domain.ValueObjects;
using Zeus.People.Infrastructure.Persistence;
using Zeus.People.Infrastructure.Persistence.Repositories;

namespace Zeus.People.Infrastructure.Tests.Repositories;

public class DepartmentRepositoryTests : IDisposable
{
    private readonly AcademicContext _context;
    private readonly DepartmentRepository _repository;

    public DepartmentRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AcademicContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AcademicContext(options);
        _repository = new DepartmentRepository(_context);
    }

    [Fact]
    public async Task AddAsync_ShouldAddDepartmentSuccessfully()
    {
        // Arrange
        var department = Department.Create("Computer Science");

        // Act
        var result = await _repository.AddAsync(department);
        await _context.SaveChangesAsync(); // Unit of Work pattern

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(department.Id);

        var savedDepartment = await _context.Departments.FindAsync(department.Id);
        savedDepartment.Should().NotBeNull();
        savedDepartment!.Name.Should().Be("Computer Science");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnDepartmentWhenExists()
    {
        // Arrange
        var department = Department.Create("Mathematics");
        _context.Departments.Add(department);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(department.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(department.Id);
        result.Value.Name.Should().Be("Mathematics");
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
    public async Task GetByNameAsync_ShouldReturnDepartmentWhenExists()
    {
        // Arrange
        var department = Department.Create("Physics");
        _context.Departments.Add(department);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByNameAsync("Physics");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("Physics");
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateDepartmentSuccessfully()
    {
        // Arrange
        var department = Department.Create("Chemistry");
        _context.Departments.Add(department);
        await _context.SaveChangesAsync();

        // Set budgets using the correct method
        var researchBudget = MoneyAmt.Create(100000m);
        var teachingBudget = MoneyAmt.Create(50000m);
        department.SetBudgets(researchBudget, teachingBudget);

        // Act
        var result = await _repository.UpdateAsync(department);
        await _context.SaveChangesAsync(); // Unit of Work pattern

        // Assert
        result.IsSuccess.Should().BeTrue();

        var updatedDepartment = await _context.Departments.FindAsync(department.Id);
        updatedDepartment.Should().NotBeNull();
        updatedDepartment!.ResearchBudget!.Value.Should().Be(100000m);
        updatedDepartment.TeachingBudget!.Value.Should().Be(50000m);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveDepartmentSuccessfully()
    {
        // Arrange
        var department = Department.Create("Biology");
        _context.Departments.Add(department);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.DeleteAsync(department.Id);
        await _context.SaveChangesAsync(); // Unit of Work pattern - save changes through context

        // Assert
        result.IsSuccess.Should().BeTrue();

        var deletedDepartment = await _context.Departments.FindAsync(department.Id);
        deletedDepartment.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllDepartments()
    {
        // Arrange
        var dept1 = Department.Create("Engineering");
        var dept2 = Department.Create("Liberal Arts");

        _context.Departments.AddRange(dept1, dept2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(d => d.Name == "Engineering");
        result.Value.Should().Contain(d => d.Name == "Liberal Arts");
    }

    [Fact]
    public async Task Department_WithBudgets_ShouldPersistCorrectly()
    {
        // Arrange
        var department = Department.Create("Business");
        var researchBudget = MoneyAmt.Create(75000m);
        var teachingBudget = MoneyAmt.Create(125000m);

        department.SetBudgets(researchBudget, teachingBudget);

        // Act
        _context.Departments.Add(department);
        await _context.SaveChangesAsync();

        // Assert
        var savedDepartment = await _context.Departments.FindAsync(department.Id);
        savedDepartment.Should().NotBeNull();
        savedDepartment!.ResearchBudget!.Value.Should().Be(75000m);
        savedDepartment.TeachingBudget!.Value.Should().Be(125000m);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
