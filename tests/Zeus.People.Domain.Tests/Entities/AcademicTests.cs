using FluentAssertions;
using Zeus.People.Domain.Entities;
using Zeus.People.Domain.Events;
using Zeus.People.Domain.Exceptions;
using Zeus.People.Domain.ValueObjects;

namespace Zeus.People.Domain.Tests.Entities;

public class AcademicTests
{
    private readonly EmpNr _validEmpNr = EmpNr.Create("AB1234");
    private readonly EmpName _validEmpName = EmpName.Create("Smith J.");
    private readonly Rank _professorRank = Rank.CreateProfessor();
    private readonly Rank _lecturerRank = Rank.CreateLecturer();

    [Fact]
    public void Create_WithValidParameters_ShouldSucceed()
    {
        // Act
        var academic = Academic.Create(_validEmpNr, _validEmpName, _professorRank);

        // Assert
        academic.EmpNr.Should().Be(_validEmpNr);
        academic.EmpName.Should().Be(_validEmpName);
        academic.Rank.Should().Be(_professorRank);
        academic.IsTenured.Should().BeFalse();
        academic.ContractEndDate.Should().BeNull();
        academic.IsProfessor.Should().BeTrue();
        academic.IsTeacher.Should().BeFalse(); // No subjects assigned yet
        academic.IsTeachingProfessor.Should().BeFalse(); // No subjects assigned yet

        academic.DomainEvents.Should().HaveCount(1);
        academic.DomainEvents.First().Should().BeOfType<AcademicCreatedEvent>();
    }

    [Fact]
    public void Create_WithNullParameters_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act1 = () => Academic.Create(null!, _validEmpName, _professorRank);
        act1.Should().Throw<ArgumentNullException>();

        var act2 = () => Academic.Create(_validEmpNr, null!, _professorRank);
        act2.Should().Throw<ArgumentNullException>();

        var act3 = () => Academic.Create(_validEmpNr, _validEmpName, null!);
        act3.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ChangeRank_WithValidRank_ShouldUpdateRankAndRaiseEvent()
    {
        // Arrange
        var academic = Academic.Create(_validEmpNr, _validEmpName, _lecturerRank);
        academic.ClearDomainEvents();

        // Act
        academic.ChangeRank(_professorRank);

        // Assert
        academic.Rank.Should().Be(_professorRank);
        academic.IsProfessor.Should().BeTrue();

        academic.DomainEvents.Should().HaveCount(1);
        var rankChangedEvent = academic.DomainEvents.First().Should().BeOfType<AcademicRankChangedEvent>().Subject;
        rankChangedEvent.AcademicId.Should().Be(academic.Id);
        rankChangedEvent.OldRank.Should().Be(_lecturerRank);
        rankChangedEvent.NewRank.Should().Be(_professorRank);
    }

    [Fact]
    public void AssignToDepartment_WithValidDepartmentId_ShouldAssignAndRaiseEvent()
    {
        // Arrange
        var academic = Academic.Create(_validEmpNr, _validEmpName, _professorRank);
        var departmentId = Guid.NewGuid();
        academic.ClearDomainEvents();

        // Act
        academic.AssignToDepartment(departmentId);

        // Assert
        academic.DepartmentId.Should().Be(departmentId);

        academic.DomainEvents.Should().HaveCount(1);
        var assignedEvent = academic.DomainEvents.First().Should().BeOfType<AcademicAssignedToDepartmentEvent>().Subject;
        assignedEvent.AcademicId.Should().Be(academic.Id);
        assignedEvent.DepartmentId.Should().Be(departmentId);
    }

    [Fact]
    public void AssignToRoom_WithValidRoomId_ShouldAssignAndRaiseEvent()
    {
        // Arrange
        var academic = Academic.Create(_validEmpNr, _validEmpName, _professorRank);
        var roomId = Guid.NewGuid();
        academic.ClearDomainEvents();

        // Act
        academic.AssignToRoom(roomId);

        // Assert
        academic.RoomId.Should().Be(roomId);

        academic.DomainEvents.Should().HaveCount(1);
        var assignedEvent = academic.DomainEvents.First().Should().BeOfType<AcademicAssignedToRoomEvent>().Subject;
        assignedEvent.AcademicId.Should().Be(academic.Id);
        assignedEvent.RoomId.Should().Be(roomId);
    }

    [Fact]
    public void MakeTenured_WhenNotTenuredAndNoContractEndDate_ShouldSucceedAndRaiseEvent()
    {
        // Arrange
        var academic = Academic.Create(_validEmpNr, _validEmpName, _professorRank);
        academic.ClearDomainEvents();

        // Act
        academic.MakeTenured();

        // Assert
        academic.IsTenured.Should().BeTrue();

        academic.DomainEvents.Should().HaveCount(1);
        var tenuredEvent = academic.DomainEvents.First().Should().BeOfType<AcademicTenuredEvent>().Subject;
        tenuredEvent.AcademicId.Should().Be(academic.Id);
    }

    [Fact]
    public void MakeTenured_WhenHasContractEndDate_ShouldThrowBusinessRuleViolationException()
    {
        // Arrange
        var academic = Academic.Create(_validEmpNr, _validEmpName, _professorRank);
        academic.SetContractEndDate(DateTime.Today.AddYears(1));

        // Act & Assert
        var act = () => academic.MakeTenured();
        act.Should().Throw<BusinessRuleViolationException>()
           .WithMessage("Academic who is tenured must not have a Date indicating their contract end");
    }

    [Fact]
    public void SetContractEndDate_WhenNotTenured_ShouldSucceedAndRaiseEvent()
    {
        // Arrange
        var academic = Academic.Create(_validEmpNr, _validEmpName, _professorRank);
        var contractEndDate = DateTime.Today.AddYears(1);
        academic.ClearDomainEvents();

        // Act
        academic.SetContractEndDate(contractEndDate);

        // Assert
        academic.ContractEndDate.Should().Be(contractEndDate);

        academic.DomainEvents.Should().HaveCount(1);
        var contractEvent = academic.DomainEvents.First().Should().BeOfType<AcademicContractEndDateSetEvent>().Subject;
        contractEvent.AcademicId.Should().Be(academic.Id);
        contractEvent.ContractEndDate.Should().Be(contractEndDate);
    }

    [Fact]
    public void SetContractEndDate_WhenTenured_ShouldThrowBusinessRuleViolationException()
    {
        // Arrange
        var academic = Academic.Create(_validEmpNr, _validEmpName, _professorRank);
        academic.MakeTenured();

        // Act & Assert
        var act = () => academic.SetContractEndDate(DateTime.Today.AddYears(1));
        act.Should().Throw<BusinessRuleViolationException>()
           .WithMessage("Academic who is tenured must not have a Date indicating their contract end");
    }

    [Fact]
    public void AssignChair_WhenIsProfessor_ShouldSucceed()
    {
        // Arrange
        var academic = Academic.Create(_validEmpNr, _validEmpName, _professorRank);
        var chairId = Guid.NewGuid();

        // Act
        academic.AssignChair(chairId);

        // Assert
        academic.ChairId.Should().Be(chairId);
    }

    [Fact]
    public void AssignChair_WhenNotProfessor_ShouldThrowBusinessRuleViolationException()
    {
        // Arrange
        var academic = Academic.Create(_validEmpNr, _validEmpName, _lecturerRank);
        var chairId = Guid.NewGuid();

        // Act & Assert
        var act = () => academic.AssignChair(chairId);
        act.Should().Throw<BusinessRuleViolationException>()
           .WithMessage("Only professors can hold chairs");
    }

    [Fact]
    public void AddSubject_WithValidSubjectId_ShouldMakeAcademicTeacher()
    {
        // Arrange
        var academic = Academic.Create(_validEmpNr, _validEmpName, _professorRank);
        var subjectId = Guid.NewGuid();

        // Act
        academic.AddSubject(subjectId);

        // Assert
        academic.SubjectIds.Should().Contain(subjectId);
        academic.IsTeacher.Should().BeTrue();
        academic.IsTeachingProfessor.Should().BeTrue();
    }

    [Fact]
    public void AddSubject_WithDuplicateSubjectId_ShouldNotAddDuplicate()
    {
        // Arrange
        var academic = Academic.Create(_validEmpNr, _validEmpName, _professorRank);
        var subjectId = Guid.NewGuid();
        academic.AddSubject(subjectId);

        // Act
        academic.AddSubject(subjectId);

        // Assert
        academic.SubjectIds.Should().HaveCount(1);
        academic.SubjectIds.Should().Contain(subjectId);
    }

    [Fact]
    public void RemoveSubject_WithExistingSubjectId_ShouldRemoveSubject()
    {
        // Arrange
        var academic = Academic.Create(_validEmpNr, _validEmpName, _professorRank);
        var subjectId = Guid.NewGuid();
        academic.AddSubject(subjectId);

        // Act
        academic.RemoveSubject(subjectId);

        // Assert
        academic.SubjectIds.Should().NotContain(subjectId);
        academic.IsTeacher.Should().BeFalse();
        academic.IsTeachingProfessor.Should().BeFalse();
    }

    [Fact]
    public void GetEnsuredAccessLevel_ShouldReturnCorrectAccessLevel()
    {
        // Arrange
        var professorAcademic = Academic.Create(_validEmpNr, _validEmpName, _professorRank);
        var lecturerAcademic = Academic.Create(EmpNr.Create("CD5678"), EmpName.Create("Jones A."), _lecturerRank);

        // Act & Assert
        professorAcademic.GetEnsuredAccessLevel().IsNational.Should().BeTrue();
        lecturerAcademic.GetEnsuredAccessLevel().IsLocal.Should().BeTrue();
    }

    [Fact]
    public void SetHomePhone_WithValidPhoneNumber_ShouldSetHomePhone()
    {
        // Arrange
        var academic = Academic.Create(_validEmpNr, _validEmpName, _professorRank);
        var phoneNr = PhoneNr.Create("123-456-7890");

        // Act
        academic.SetHomePhone(phoneNr);

        // Assert
        academic.HomePhone.Should().Be(phoneNr);
    }
}
