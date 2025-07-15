using FluentAssertions;
using Zeus.People.Domain.Entities;
using Zeus.People.Domain.Events;
using Zeus.People.Domain.Exceptions;
using Zeus.People.Domain.ValueObjects;

namespace Zeus.People.Domain.Tests.Entities;

public class DepartmentTests
{
    private readonly string _validDepartmentName = "Computer Science";
    private readonly MoneyAmt _validBudget = MoneyAmt.Create(100000);
    private readonly PhoneNr _validPhone = PhoneNr.Create("123-456-7890");

    [Fact]
    public void Create_WithValidName_ShouldSucceed()
    {
        // Act
        var department = Department.Create(_validDepartmentName);

        // Assert
        department.Name.Should().Be(_validDepartmentName);
        department.ResearchBudget.Should().BeNull();
        department.TeachingBudget.Should().BeNull();
        department.HeadProfessorId.Should().BeNull();
        department.ChairId.Should().BeNull();
        department.AcademicIds.Should().BeEmpty();

        department.DomainEvents.Should().HaveCount(1);
        department.DomainEvents.First().Should().BeOfType<DepartmentCreatedEvent>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_WithInvalidName_ShouldThrowArgumentException(string invalidName)
    {
        // Act & Assert
        var act = () => Department.Create(invalidName);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNullName_ShouldThrowArgumentException()
    {
        // Act & Assert
        var act = () => Department.Create(null!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetBudgets_WithValidAmounts_ShouldSucceedAndRaiseEvent()
    {
        // Arrange
        var department = Department.Create(_validDepartmentName);
        var researchBudget = MoneyAmt.Create(50000);
        var teachingBudget = MoneyAmt.Create(75000);
        department.ClearDomainEvents();

        // Act
        department.SetBudgets(researchBudget, teachingBudget);

        // Assert
        department.ResearchBudget.Should().Be(researchBudget);
        department.TeachingBudget.Should().Be(teachingBudget);

        department.DomainEvents.Should().HaveCount(1);
        var budgetEvent = department.DomainEvents.First().Should().BeOfType<DepartmentBudgetsSetEvent>().Subject;
        budgetEvent.DepartmentId.Should().Be(department.Id);
        budgetEvent.ResearchBudget.Should().Be(researchBudget);
        budgetEvent.TeachingBudget.Should().Be(teachingBudget);
    }

    [Fact]
    public void SetBudgets_WithNullAmounts_ShouldThrowArgumentNullException()
    {
        // Arrange
        var department = Department.Create(_validDepartmentName);

        // Act & Assert
        var act1 = () => department.SetBudgets(null!, _validBudget);
        act1.Should().Throw<ArgumentNullException>();

        var act2 = () => department.SetBudgets(_validBudget, null!);
        act2.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AssignHead_WithProfessorInDepartment_ShouldSucceedAndRaiseEvent()
    {
        // Arrange
        var department = Department.Create(_validDepartmentName);
        var professorId = Guid.NewGuid();
        department.AddAcademic(professorId); // Add professor to department first
        department.ClearDomainEvents();

        // Act
        department.AssignHead(professorId, _validPhone);

        // Assert
        department.HeadProfessorId.Should().Be(professorId);
        department.HeadHomePhone.Should().Be(_validPhone);

        department.DomainEvents.Should().HaveCount(1);
        var headEvent = department.DomainEvents.First().Should().BeOfType<ProfessorAssignedAsDepartmentHeadEvent>().Subject;
        headEvent.DepartmentId.Should().Be(department.Id);
        headEvent.ProfessorId.Should().Be(professorId);
    }

    [Fact]
    public void AssignHead_WithProfessorNotInDepartment_ShouldThrowBusinessRuleViolationException()
    {
        // Arrange
        var department = Department.Create(_validDepartmentName);
        var professorId = Guid.NewGuid();
        // Don't add professor to department

        // Act & Assert
        var act = () => department.AssignHead(professorId, _validPhone);
        act.Should().Throw<BusinessRuleViolationException>()
           .WithMessage("Professor who heads a Dept must work for that Dept");
    }

    [Fact]
    public void AssignChair_WithValidChairId_ShouldSucceedAndRaiseEvent()
    {
        // Arrange
        var department = Department.Create(_validDepartmentName);
        var chairId = Guid.NewGuid();
        department.ClearDomainEvents();

        // Act
        department.AssignChair(chairId);

        // Assert
        department.ChairId.Should().Be(chairId);

        department.DomainEvents.Should().HaveCount(1);
        var chairEvent = department.DomainEvents.First().Should().BeOfType<ChairAssignedToDepartmentEvent>().Subject;
        chairEvent.DepartmentId.Should().Be(department.Id);
        chairEvent.ChairId.Should().Be(chairId);
    }

    [Fact]
    public void AddAcademic_WithValidAcademicId_ShouldAddToCollection()
    {
        // Arrange
        var department = Department.Create(_validDepartmentName);
        var academicId = Guid.NewGuid();

        // Act
        department.AddAcademic(academicId);

        // Assert
        department.AcademicIds.Should().Contain(academicId);
    }

    [Fact]
    public void AddAcademic_WithDuplicateAcademicId_ShouldNotAddDuplicate()
    {
        // Arrange
        var department = Department.Create(_validDepartmentName);
        var academicId = Guid.NewGuid();
        department.AddAcademic(academicId);

        // Act
        department.AddAcademic(academicId);

        // Assert
        department.AcademicIds.Should().HaveCount(1);
        department.AcademicIds.Should().Contain(academicId);
    }

    [Fact]
    public void RemoveAcademic_WithExistingAcademicId_ShouldRemoveFromCollection()
    {
        // Arrange
        var department = Department.Create(_validDepartmentName);
        var academicId = Guid.NewGuid();
        department.AddAcademic(academicId);

        // Act
        department.RemoveAcademic(academicId);

        // Assert
        department.AcademicIds.Should().NotContain(academicId);
    }

    [Fact]
    public void RemoveAcademic_WhenAcademicIsHead_ShouldClearHeadAssignment()
    {
        // Arrange
        var department = Department.Create(_validDepartmentName);
        var professorId = Guid.NewGuid();
        department.AddAcademic(professorId);
        department.AssignHead(professorId, _validPhone);

        // Act
        department.RemoveAcademic(professorId);

        // Assert
        department.AcademicIds.Should().NotContain(professorId);
        department.HeadProfessorId.Should().BeNull();
        department.HeadHomePhone.Should().BeNull();
    }

    [Fact]
    public void ValidateUniqueEmpNameInDepartment_WithUniqueEmpName_ShouldNotThrow()
    {
        // Arrange
        var department = Department.Create(_validDepartmentName);
        var empName = EmpName.Create("Smith J.");
        var existingEmpNames = new List<EmpName>
        {
            EmpName.Create("Jones A."),
            EmpName.Create("Brown B.")
        };

        // Act & Assert
        var act = () => department.ValidateUniqueEmpNameInDepartment(empName, existingEmpNames);
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateUniqueEmpNameInDepartment_WithDuplicateEmpName_ShouldThrowBusinessRuleViolationException()
    {
        // Arrange
        var department = Department.Create(_validDepartmentName);
        var empName = EmpName.Create("Smith J.");
        var existingEmpNames = new List<EmpName>
        {
            EmpName.Create("Smith J."),
            EmpName.Create("Jones A.")
        };

        // Act & Assert
        var act = () => department.ValidateUniqueEmpNameInDepartment(empName, existingEmpNames);
        act.Should().Throw<BusinessRuleViolationException>()
           .WithMessage("Each Academic that works for a Dept must have a unique EmpName in that Dept*");
    }

    [Fact]
    public void GetProfessorCount_ShouldReturnCorrectCount()
    {
        // Arrange
        var department = Department.Create(_validDepartmentName);
        var professor1Id = Guid.NewGuid();
        var professor2Id = Guid.NewGuid();
        var lecturerId = Guid.NewGuid();

        department.AddAcademic(professor1Id);
        department.AddAcademic(professor2Id);
        department.AddAcademic(lecturerId);

        var academics = new List<Academic>
        {
            CreateMockAcademic(professor1Id, Rank.CreateProfessor()),
            CreateMockAcademic(professor2Id, Rank.CreateProfessor()),
            CreateMockAcademic(lecturerId, Rank.CreateLecturer())
        };

        // Act
        var professorCount = department.GetProfessorCount(academics);

        // Assert
        professorCount.Should().Be(2);
    }

    private static Academic CreateMockAcademic(Guid id, Rank rank)
    {
        // Generate a valid EmpNr in AB1234 format based on ID hash
        var hash = id.GetHashCode();
        var letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var empNr = $"{letters[Math.Abs(hash % 26)]}{letters[Math.Abs((hash / 26) % 26)]}{Math.Abs(hash % 9000) + 1000}";

        // Generate a valid name using only letters
        var nameHash = Math.Abs(hash);
        var firstName = $"{letters[nameHash % 26]}{letters[(nameHash / 26) % 26]}{letters[(nameHash / 676) % 26]}";
        var lastName = $"{letters[(nameHash / 17576) % 26]}.";

        var academic = Academic.Create(
            EmpNr.Create(empNr),
            EmpName.Create($"{firstName} {lastName}"),
            rank);

        // Use reflection to set the Id for testing purposes
        var idProperty = typeof(Academic).BaseType!.GetProperty("Id")!;
        idProperty.SetValue(academic, id);

        return academic;
    }
}
