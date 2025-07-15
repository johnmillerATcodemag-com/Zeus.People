using FluentAssertions;
using Zeus.People.Domain.Entities;
using Zeus.People.Domain.Exceptions;
using Zeus.People.Domain.Services;
using Zeus.People.Domain.ValueObjects;

namespace Zeus.People.Domain.Tests.Services;

public class AcademicDomainServiceTests
{
    private readonly AcademicDomainService _domainService = new();

    [Fact]
    public void ValidateAcademicDepartmentAssignment_WithUniqueEmpName_ShouldNotThrow()
    {
        // Arrange
        var academic = CreateAcademic(EmpName.Create("Smith J."));
        var department = Department.Create("Computer Science");
        var existingAcademics = new List<Academic>
        {
            CreateAcademic(EmpName.Create("Jones A.")),
            CreateAcademic(EmpName.Create("Brown B."))
        };

        // Act & Assert
        var act = () => _domainService.ValidateAcademicDepartmentAssignment(academic, department, existingAcademics);
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateAcademicDepartmentAssignment_WithDuplicateEmpName_ShouldThrowBusinessRuleViolationException()
    {
        // Arrange
        var academic = CreateAcademic(EmpName.Create("Smith J."));
        var department = Department.Create("Computer Science");
        var existingAcademics = new List<Academic>
        {
            CreateAcademic(EmpName.Create("Smith J.")),
            CreateAcademic(EmpName.Create("Jones A."))
        };

        // Act & Assert
        var act = () => _domainService.ValidateAcademicDepartmentAssignment(academic, department, existingAcademics);
        act.Should().Throw<BusinessRuleViolationException>()
           .WithMessage("Each Academic that works for a Dept must have a unique EmpName in that Dept*");
    }

    [Fact]
    public void ValidateProfessorCanHeadDepartment_WithValidProfessorInDepartment_ShouldNotThrow()
    {
        // Arrange
        var professor = CreateAcademic(EmpName.Create("Smith J."), Rank.CreateProfessor());
        var department = Department.Create("Computer Science");
        professor.AssignToDepartment(department.Id);

        // Act & Assert
        var act = () => _domainService.ValidateProfessorCanHeadDepartment(professor, department);
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateProfessorCanHeadDepartment_WithNonProfessor_ShouldThrowBusinessRuleViolationException()
    {
        // Arrange
        var lecturer = CreateAcademic(EmpName.Create("Smith J."), Rank.CreateLecturer());
        var department = Department.Create("Computer Science");
        lecturer.AssignToDepartment(department.Id);

        // Act & Assert
        var act = () => _domainService.ValidateProfessorCanHeadDepartment(lecturer, department);
        act.Should().Throw<BusinessRuleViolationException>()
           .WithMessage("Only professors can head departments");
    }

    [Fact]
    public void ValidateProfessorCanHeadDepartment_WithProfessorNotInDepartment_ShouldThrowBusinessRuleViolationException()
    {
        // Arrange
        var professor = CreateAcademic(EmpName.Create("Smith J."), Rank.CreateProfessor());
        var department = Department.Create("Computer Science");
        // Don't assign professor to department

        // Act & Assert
        var act = () => _domainService.ValidateProfessorCanHeadDepartment(professor, department);
        act.Should().Throw<BusinessRuleViolationException>()
           .WithMessage("Professor who heads a Dept must work for that Dept");
    }

    [Fact]
    public void ValidateProfessorCanHoldChair_WithValidProfessorAndNoExistingChair_ShouldNotThrow()
    {
        // Arrange
        var professor = CreateAcademic(EmpName.Create("Smith J."), Rank.CreateProfessor());
        var chair = Chair.Create("Databases");
        var existingChairs = new List<Chair>();

        // Act & Assert
        var act = () => _domainService.ValidateProfessorCanHoldChair(professor, chair, existingChairs);
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateProfessorCanHoldChair_WithNonProfessor_ShouldThrowBusinessRuleViolationException()
    {
        // Arrange
        var lecturer = CreateAcademic(EmpName.Create("Smith J."), Rank.CreateLecturer());
        var chair = Chair.Create("Databases");
        var existingChairs = new List<Chair>();

        // Act & Assert
        var act = () => _domainService.ValidateProfessorCanHoldChair(lecturer, chair, existingChairs);
        act.Should().Throw<BusinessRuleViolationException>()
           .WithMessage("Only professors can hold chairs");
    }

    [Fact]
    public void ValidateProfessorCanHoldChair_WithProfessorAlreadyHoldingAnotherChair_ShouldThrowBusinessRuleViolationException()
    {
        // Arrange
        var professor = CreateAcademic(EmpName.Create("Smith J."), Rank.CreateProfessor());
        var chair1 = Chair.Create("Databases");
        var chair2 = Chair.Create("AI");
        chair1.AssignToProfessor(professor.Id);

        var existingChairs = new List<Chair> { chair1 };

        // Act & Assert
        var act = () => _domainService.ValidateProfessorCanHoldChair(professor, chair2, existingChairs);
        act.Should().Throw<BusinessRuleViolationException>()
           .WithMessage("Professor holds at most one Chair");
    }

    [Fact]
    public void ValidateTeacherAuditRelationship_WithValidRelationship_ShouldNotThrow()
    {
        // Arrange
        var teacher1 = CreateTeacher(EmpName.Create("Smith J."));
        var teacher2 = CreateTeacher(EmpName.Create("Jones A."));
        var existingAudits = new List<(Guid AuditorId, Guid AuditeeId)>();

        // Act & Assert
        var act = () => _domainService.ValidateTeacherAuditRelationship(teacher1, teacher2, existingAudits);
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateTeacherAuditRelationship_WithReverseRelationshipExists_ShouldThrowBusinessRuleViolationException()
    {
        // Arrange
        var teacher1 = CreateTeacher(EmpName.Create("Smith J."));
        var teacher2 = CreateTeacher(EmpName.Create("Jones A."));
        var existingAudits = new List<(Guid AuditorId, Guid AuditeeId)>
        {
            (teacher2.Id, teacher1.Id) // Reverse relationship exists
        };

        // Act & Assert
        var act = () => _domainService.ValidateTeacherAuditRelationship(teacher1, teacher2, existingAudits);
        act.Should().Throw<BusinessRuleViolationException>()
           .WithMessage("A Teacher that audits another Teacher cannot be audited by that Teacher");
    }

    [Fact]
    public void ValidateTeacherAuditRelationship_WithNonTeacher_ShouldThrowBusinessRuleViolationException()
    {
        // Arrange
        var nonTeacher = CreateAcademic(EmpName.Create("Smith J.")); // No subjects assigned
        var teacher = CreateTeacher(EmpName.Create("Jones A."));
        var existingAudits = new List<(Guid AuditorId, Guid AuditeeId)>();

        // Act & Assert
        var act = () => _domainService.ValidateTeacherAuditRelationship(nonTeacher, teacher, existingAudits);
        act.Should().Throw<BusinessRuleViolationException>()
           .WithMessage("Only teachers can participate in audit relationships");
    }

    [Fact]
    public void ValidateTeachingProfessorCanServeOnCommittee_WithValidTeachingProfessor_ShouldNotThrow()
    {
        // Arrange
        var teachingProfessor = CreateTeacher(EmpName.Create("Smith J."), Rank.CreateProfessor());
        var committee = Committee.Create("Academic Standards");

        // Act & Assert
        var act = () => _domainService.ValidateTeachingProfessorCanServeOnCommittee(teachingProfessor, committee);
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateTeachingProfessorCanServeOnCommittee_WithNonTeachingProfessor_ShouldThrowBusinessRuleViolationException()
    {
        // Arrange
        var nonTeachingProfessor = CreateAcademic(EmpName.Create("Smith J."), Rank.CreateProfessor()); // No subjects
        var committee = Committee.Create("Academic Standards");

        // Act & Assert
        var act = () => _domainService.ValidateTeachingProfessorCanServeOnCommittee(nonTeachingProfessor, committee);
        act.Should().Throw<BusinessRuleViolationException>()
           .WithMessage("Only teaching professors can serve on committees");
    }

    [Fact]
    public void ValidateRoomBuildingUniqueness_WithUniqueRoomNumber_ShouldNotThrow()
    {
        // Arrange
        var buildingId = Guid.NewGuid();
        var room = Room.Create(RoomNr.Create("101"), buildingId);
        var existingRooms = new List<Room>
        {
            Room.Create(RoomNr.Create("102"), buildingId),
            Room.Create(RoomNr.Create("101"), Guid.NewGuid()) // Different building
        };

        // Act & Assert
        var act = () => _domainService.ValidateRoomBuildingUniqueness(room, existingRooms);
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateRoomBuildingUniqueness_WithDuplicateRoomInSameBuilding_ShouldThrowBusinessRuleViolationException()
    {
        // Arrange
        var buildingId = Guid.NewGuid();
        var room = Room.Create(RoomNr.Create("101"), buildingId);
        var existingRooms = new List<Room>
        {
            Room.Create(RoomNr.Create("101"), buildingId) // Same room number in same building
        };

        // Act & Assert
        var act = () => _domainService.ValidateRoomBuildingUniqueness(room, existingRooms);
        act.Should().Throw<BusinessRuleViolationException>()
           .WithMessage("The combination of a Room has roomNr and Room is in Building is unique");
    }

    private static Academic CreateAcademic(EmpName empName, Rank? rank = null)
    {
        // Generate a valid EmpNr in AB1234 format
        var letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var empNr = $"{letters[Random.Shared.Next(26)]}{letters[Random.Shared.Next(26)]}{Random.Shared.Next(1000, 9999)}";

        return Academic.Create(
            EmpNr.Create(empNr),
            empName,
            rank ?? Rank.CreateLecturer());
    }

    private static Academic CreateTeacher(EmpName empName, Rank? rank = null)
    {
        var academic = CreateAcademic(empName, rank);
        academic.AddSubject(Guid.NewGuid()); // Make them a teacher
        return academic;
    }
}
