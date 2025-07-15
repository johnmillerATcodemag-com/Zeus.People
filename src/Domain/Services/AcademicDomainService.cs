using Zeus.People.Domain.Entities;
using Zeus.People.Domain.Exceptions;
using Zeus.People.Domain.ValueObjects;

namespace Zeus.People.Domain.Services;

/// <summary>
/// Domain service for managing academic operations and enforcing business rules
/// </summary>
public class AcademicDomainService
{
    /// <summary>
    /// Validates that an academic can be assigned to a department
    /// Business rule: Each Academic that works for a Dept must have a unique EmpName in that Dept
    /// </summary>
    public void ValidateAcademicDepartmentAssignment(Academic academic, Department department, IEnumerable<Academic> existingAcademicsInDept)
    {
        if (academic == null) throw new ArgumentNullException(nameof(academic));
        if (department == null) throw new ArgumentNullException(nameof(department));

        var existingEmpNames = existingAcademicsInDept.Select(a => a.EmpName).ToList();
        department.ValidateUniqueEmpNameInDepartment(academic.EmpName, existingEmpNames);
    }

    /// <summary>
    /// Validates that a professor can head a department
    /// Business rule: Professor who heads a Dept must work for that Dept
    /// </summary>
    public void ValidateProfessorCanHeadDepartment(Academic professor, Department department)
    {
        if (professor == null) throw new ArgumentNullException(nameof(professor));
        if (department == null) throw new ArgumentNullException(nameof(department));

        if (!professor.IsProfessor)
        {
            throw new BusinessRuleViolationException("Only professors can head departments");
        }

        if (professor.DepartmentId != department.Id)
        {
            throw new BusinessRuleViolationException("Professor who heads a Dept must work for that Dept");
        }
    }

    /// <summary>
    /// Validates that a professor can hold a chair
    /// Business rule: Professor holds at most one Chair
    /// </summary>
    public void ValidateProfessorCanHoldChair(Academic professor, Chair chair, IEnumerable<Chair> existingChairs)
    {
        if (professor == null) throw new ArgumentNullException(nameof(professor));
        if (chair == null) throw new ArgumentNullException(nameof(chair));

        if (!professor.IsProfessor)
        {
            throw new BusinessRuleViolationException("Only professors can hold chairs");
        }

        // Check if professor already holds another chair
        var professorCurrentChair = existingChairs.FirstOrDefault(c => c.ProfessorId == professor.Id && c.Id != chair.Id);
        if (professorCurrentChair != null)
        {
            throw new BusinessRuleViolationException("Professor holds at most one Chair");
        }
    }

    /// <summary>
    /// Validates teacher audit relationships
    /// Business rule: A Teacher that audits another Teacher cannot be audited by that Teacher
    /// </summary>
    public void ValidateTeacherAuditRelationship(Academic auditor, Academic auditee, IEnumerable<(Guid AuditorId, Guid AuditeeId)> existingAudits)
    {
        if (auditor == null) throw new ArgumentNullException(nameof(auditor));
        if (auditee == null) throw new ArgumentNullException(nameof(auditee));

        if (!auditor.IsTeacher || !auditee.IsTeacher)
        {
            throw new BusinessRuleViolationException("Only teachers can participate in audit relationships");
        }

        // Check if the reverse relationship already exists
        var reverseAuditExists = existingAudits.Any(audit => audit.AuditorId == auditee.Id && audit.AuditeeId == auditor.Id);
        if (reverseAuditExists)
        {
            throw new BusinessRuleViolationException("A Teacher that audits another Teacher cannot be audited by that Teacher");
        }
    }

    /// <summary>
    /// Validates that a teaching professor can serve on a committee
    /// Business rule: TeachingProfessor serves on Committee
    /// </summary>
    public void ValidateTeachingProfessorCanServeOnCommittee(Academic professor, Committee committee)
    {
        if (professor == null) throw new ArgumentNullException(nameof(professor));
        if (committee == null) throw new ArgumentNullException(nameof(committee));

        if (!professor.IsTeachingProfessor)
        {
            throw new BusinessRuleViolationException("Only teaching professors can serve on committees");
        }
    }

    /// <summary>
    /// Validates room and building uniqueness
    /// Business rule: The combination of a Room has roomNr and Room is in Building is unique
    /// </summary>
    public void ValidateRoomBuildingUniqueness(Room room, IEnumerable<Room> existingRooms)
    {
        if (room == null) throw new ArgumentNullException(nameof(room));

        var duplicateRoom = existingRooms.FirstOrDefault(r =>
            r.Id != room.Id &&
            r.BuildingId == room.BuildingId &&
            r.RoomNr.Value == room.RoomNr.Value);

        if (duplicateRoom != null)
        {
            throw new BusinessRuleViolationException("The combination of a Room has roomNr and Room is in Building is unique");
        }
    }
}
