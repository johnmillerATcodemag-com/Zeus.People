using Zeus.People.Application.DTOs;
using Zeus.People.Application.Common;
using Zeus.People.Application.Interfaces;

namespace Zeus.People.API.Tests;

public class MockReadModelRepository : IAcademicReadRepository, IDepartmentReadRepository, IRoomReadRepository, IExtensionReadRepository
{
    private readonly List<AcademicDto> _academics;
    private readonly List<DepartmentDto> _departments;
    private readonly List<RoomDto> _rooms;
    private readonly List<ExtensionDto> _extensions;

    public MockReadModelRepository()
    {
        // Sample academic data
        _academics = new List<AcademicDto>
        {
            new AcademicDto
            {
                Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440000"),
                EmpNr = "EMP001",
                EmpName = "Dr. John Smith",
                Rank = "Professor",
                IsTenured = true,
                ContractEndDate = null,
                HomePhone = "555-1234",
                DepartmentId = Guid.Parse("550e8400-e29b-41d4-a716-446655440100"),
                DepartmentName = "Computer Science",
                RoomId = Guid.Parse("550e8400-e29b-41d4-a716-446655440200"),
                RoomNumber = "CS101",
                ExtensionId = Guid.Parse("550e8400-e29b-41d4-a716-446655440300"),
                ExtensionNumber = "2001"
            },
            new AcademicDto
            {
                Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440001"),
                EmpNr = "EMP002",
                EmpName = "Dr. Jane Doe",
                Rank = "Associate Professor",
                IsTenured = false,
                ContractEndDate = new DateTime(2025, 12, 31),
                HomePhone = "555-5678",
                DepartmentId = Guid.Parse("550e8400-e29b-41d4-a716-446655440101"),
                DepartmentName = "Mathematics",
                RoomId = Guid.Parse("550e8400-e29b-41d4-a716-446655440201"),
                RoomNumber = "MATH202",
                ExtensionId = Guid.Parse("550e8400-e29b-41d4-a716-446655440301"),
                ExtensionNumber = "2002"
            }
        };

        // Sample department data
        _departments = new List<DepartmentDto>
        {
            new DepartmentDto
            {
                Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440100"),
                Name = "Computer Science",
                ResearchBudget = 500000m,
                TeachingBudget = 300000m,
                HeadProfessorId = Guid.Parse("550e8400-e29b-41d4-a716-446655440000"),
                HeadProfessorName = "Dr. John Smith"
            },
            new DepartmentDto
            {
                Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440101"),
                Name = "Mathematics",
                ResearchBudget = 400000m,
                TeachingBudget = 250000m,
                HeadProfessorId = Guid.Parse("550e8400-e29b-41d4-a716-446655440001"),
                HeadProfessorName = "Dr. Jane Doe"
            }
        };

        // Sample room data
        _rooms = new List<RoomDto>
        {
            new RoomDto
            {
                Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440200"),
                RoomNumber = "CS101",
                Capacity = 30,
                RoomType = "Office",
                BuildingId = Guid.Parse("550e8400-e29b-41d4-a716-446655440400"),
                BuildingName = "Computer Science Building",
                BuildingNumber = "CS",
                OccupiedByAcademicId = Guid.Parse("550e8400-e29b-41d4-a716-446655440000"),
                OccupiedByAcademicName = "Dr. John Smith",
                IsOccupied = true
            },
            new RoomDto
            {
                Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440201"),
                RoomNumber = "MATH202",
                Capacity = 25,
                RoomType = "Office",
                BuildingId = Guid.Parse("550e8400-e29b-41d4-a716-446655440401"),
                BuildingName = "Mathematics Building",
                BuildingNumber = "MATH",
                OccupiedByAcademicId = Guid.Parse("550e8400-e29b-41d4-a716-446655440001"),
                OccupiedByAcademicName = "Dr. Jane Doe",
                IsOccupied = true
            }
        };

        // Sample extension data
        _extensions = new List<ExtensionDto>
        {
            new ExtensionDto
            {
                Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440300"),
                ExtensionNumber = "2001",
                AccessLevel = "Full",
                Location = "CS101",
                IsInUse = true,
                Description = "Dr. Smith's office phone"
            },
            new ExtensionDto
            {
                Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440301"),
                ExtensionNumber = "2002",
                AccessLevel = "Standard",
                Location = "MATH202",
                IsInUse = true,
                Description = "Dr. Doe's office phone"
            }
        };
    }

    // IAcademicReadRepository implementation
    public async Task<Result<AcademicDto?>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken);
        var academic = _academics.FirstOrDefault(a => a.Id == id);
        return Result<AcademicDto?>.Success(academic);
    }

    public async Task<Result<AcademicDto?>> GetByEmpNrAsync(string empNr, CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken);
        var academic = _academics.FirstOrDefault(a => a.EmpNr == empNr);
        return Result<AcademicDto?>.Success(academic);
    }

    public async Task<Result<PagedResult<AcademicSummaryDto>>> GetAllAsync(
        int pageNumber,
        int pageSize,
        string? nameFilter = null,
        string? rankFilter = null,
        bool? isTenuredFilter = null,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken);

        var query = _academics.AsQueryable();

        if (!string.IsNullOrEmpty(nameFilter))
            query = query.Where(a => a.EmpName.Contains(nameFilter, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrEmpty(rankFilter))
            query = query.Where(a => a.Rank.Contains(rankFilter, StringComparison.OrdinalIgnoreCase));

        if (isTenuredFilter.HasValue)
            query = query.Where(a => a.IsTenured == isTenuredFilter.Value);

        var totalCount = query.Count();
        var items = query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AcademicSummaryDto
            {
                Id = a.Id,
                EmpNr = a.EmpNr,
                EmpName = a.EmpName,
                Rank = a.Rank,
                IsTenured = a.IsTenured,
                DepartmentName = a.DepartmentName,
                RoomNumber = a.RoomNumber
            })
            .ToList();

        var result = new PagedResult<AcademicSummaryDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        return Result<PagedResult<AcademicSummaryDto>>.Success(result);
    }

    public async Task<Result<PagedResult<AcademicSummaryDto>>> GetByDepartmentAsync(
        Guid departmentId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken);

        var query = _academics.Where(a => a.DepartmentId == departmentId);
        var totalCount = query.Count();
        var items = query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AcademicSummaryDto
            {
                Id = a.Id,
                EmpNr = a.EmpNr,
                EmpName = a.EmpName,
                Rank = a.Rank,
                IsTenured = a.IsTenured,
                DepartmentName = a.DepartmentName,
                RoomNumber = a.RoomNumber
            })
            .ToList();

        var result = new PagedResult<AcademicSummaryDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        return Result<PagedResult<AcademicSummaryDto>>.Success(result);
    }

    public async Task<Result<PagedResult<AcademicSummaryDto>>> GetByRankAsync(
        string rank,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken);

        var query = _academics.Where(a => a.Rank.Equals(rank, StringComparison.OrdinalIgnoreCase));
        var totalCount = query.Count();
        var items = query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AcademicSummaryDto
            {
                Id = a.Id,
                EmpNr = a.EmpNr,
                EmpName = a.EmpName,
                Rank = a.Rank,
                IsTenured = a.IsTenured,
                DepartmentName = a.DepartmentName,
                RoomNumber = a.RoomNumber
            })
            .ToList();

        var result = new PagedResult<AcademicSummaryDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        return Result<PagedResult<AcademicSummaryDto>>.Success(result);
    }

    public async Task<Result<PagedResult<AcademicSummaryDto>>> GetTenuredAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken);

        var query = _academics.Where(a => a.IsTenured);
        var totalCount = query.Count();
        var items = query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AcademicSummaryDto
            {
                Id = a.Id,
                EmpNr = a.EmpNr,
                EmpName = a.EmpName,
                Rank = a.Rank,
                IsTenured = a.IsTenured,
                DepartmentName = a.DepartmentName,
                RoomNumber = a.RoomNumber
            })
            .ToList();

        var result = new PagedResult<AcademicSummaryDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        return Result<PagedResult<AcademicSummaryDto>>.Success(result);
    }

    public async Task<Result<PagedResult<AcademicSummaryDto>>> GetWithExpiringContractsAsync(
        DateTime? beforeDate,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken);

        var cutoffDate = beforeDate ?? DateTime.Now.AddMonths(6);
        var query = _academics.Where(a => a.ContractEndDate.HasValue && a.ContractEndDate <= cutoffDate);
        var totalCount = query.Count();
        var items = query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AcademicSummaryDto
            {
                Id = a.Id,
                EmpNr = a.EmpNr,
                EmpName = a.EmpName,
                Rank = a.Rank,
                IsTenured = a.IsTenured,
                DepartmentName = a.DepartmentName,
                RoomNumber = a.RoomNumber
            })
            .ToList();

        var result = new PagedResult<AcademicSummaryDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        return Result<PagedResult<AcademicSummaryDto>>.Success(result);
    }

    public async Task<Result<List<AcademicCountByDepartmentDto>>> GetCountByDepartmentAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken);

        var countByDept = _academics.GroupBy(a => a.DepartmentId)
            .Select(g => new AcademicCountByDepartmentDto
            {
                DepartmentId = g.Key ?? Guid.Empty,
                DepartmentName = g.First().DepartmentName ?? "Unknown",
                ProfessorCount = g.Count(a => a.Rank == "Professor"),
                SeniorLecturerCount = g.Count(a => a.Rank == "Senior Lecturer"),
                LecturerCount = g.Count(a => a.Rank == "Lecturer"),
                TotalCount = g.Count()
            })
            .ToList();

        return Result<List<AcademicCountByDepartmentDto>>.Success(countByDept);
    }

    // IDepartmentReadRepository implementation - explicit interface implementation
    async Task<Result<DepartmentDto?>> IDepartmentReadRepository.GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        await Task.Delay(1, cancellationToken);
        var department = _departments.FirstOrDefault(d => d.Id == id);
        return Result<DepartmentDto?>.Success(department);
    }

    async Task<Result<DepartmentDto?>> IDepartmentReadRepository.GetByNameAsync(string name, CancellationToken cancellationToken)
    {
        await Task.Delay(1, cancellationToken);
        var department = _departments.FirstOrDefault(d => d.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        return Result<DepartmentDto?>.Success(department);
    }

    async Task<Result<PagedResult<DepartmentSummaryDto>>> IDepartmentReadRepository.GetAllAsync(int pageNumber, int pageSize, string? nameFilter, CancellationToken cancellationToken)
    {
        await Task.Delay(1, cancellationToken);

        var query = _departments.AsQueryable();
        if (!string.IsNullOrEmpty(nameFilter))
            query = query.Where(d => d.Name.Contains(nameFilter, StringComparison.OrdinalIgnoreCase));

        var totalCount = query.Count();
        var items = query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new DepartmentSummaryDto
            {
                Id = d.Id,
                Name = d.Name,
                HeadProfessorName = d.HeadProfessorName,
                TotalAcademics = _academics.Count(a => a.DepartmentId == d.Id),
                ResearchBudget = d.ResearchBudget,
                TeachingBudget = d.TeachingBudget
            })
            .ToList();

        var result = new PagedResult<DepartmentSummaryDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        return Result<PagedResult<DepartmentSummaryDto>>.Success(result);
    }

    async Task<Result<DepartmentStaffCountDto?>> IDepartmentReadRepository.GetStaffCountAsync(Guid departmentId, CancellationToken cancellationToken)
    {
        await Task.Delay(1, cancellationToken);
        var department = _departments.FirstOrDefault(d => d.Id == departmentId);
        if (department == null)
            return Result<DepartmentStaffCountDto?>.Success((DepartmentStaffCountDto?)null);

        var staffCount = new DepartmentStaffCountDto
        {
            DepartmentId = department.Id,
            DepartmentName = department.Name,
            ProfessorCount = _academics.Count(a => a.DepartmentId == department.Id && a.Rank == "Professor"),
            SeniorLecturerCount = _academics.Count(a => a.DepartmentId == department.Id && a.Rank == "Senior Lecturer"),
            LecturerCount = _academics.Count(a => a.DepartmentId == department.Id && a.Rank == "Lecturer"),
            TotalStaffCount = _academics.Count(a => a.DepartmentId == department.Id)
        };
        return Result<DepartmentStaffCountDto?>.Success((DepartmentStaffCountDto?)staffCount);
    }

    async Task<Result<List<DepartmentStaffCountDto>>> IDepartmentReadRepository.GetAllStaffCountsAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(1, cancellationToken);
        var staffCounts = _departments.Select(d => new DepartmentStaffCountDto
        {
            DepartmentId = d.Id,
            DepartmentName = d.Name,
            ProfessorCount = _academics.Count(a => a.DepartmentId == d.Id && a.Rank == "Professor"),
            SeniorLecturerCount = _academics.Count(a => a.DepartmentId == d.Id && a.Rank == "Senior Lecturer"),
            LecturerCount = _academics.Count(a => a.DepartmentId == d.Id && a.Rank == "Lecturer"),
            TotalStaffCount = _academics.Count(a => a.DepartmentId == d.Id)
        }).ToList();
        return Result<List<DepartmentStaffCountDto>>.Success(staffCounts);
    }

    async Task<Result<List<DepartmentSummaryDto>>> IDepartmentReadRepository.GetWithBudgetAsync(decimal? minBudget, decimal? maxBudget, CancellationToken cancellationToken)
    {
        await Task.Delay(1, cancellationToken);
        var query = _departments.AsQueryable();

        if (minBudget.HasValue)
            query = query.Where(d => (d.ResearchBudget + d.TeachingBudget) >= minBudget.Value);
        if (maxBudget.HasValue)
            query = query.Where(d => (d.ResearchBudget + d.TeachingBudget) <= maxBudget.Value);

        var result = query.Select(d => new DepartmentSummaryDto
        {
            Id = d.Id,
            Name = d.Name,
            HeadProfessorName = d.HeadProfessorName,
            TotalAcademics = _academics.Count(a => a.DepartmentId == d.Id),
            ResearchBudget = d.ResearchBudget,
            TeachingBudget = d.TeachingBudget
        }).ToList();

        return Result<List<DepartmentSummaryDto>>.Success(result);
    }

    async Task<Result<List<DepartmentSummaryDto>>> IDepartmentReadRepository.GetWithoutHeadsAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(1, cancellationToken);
        var result = _departments.Where(d => d.HeadProfessorId == null)
            .Select(d => new DepartmentSummaryDto
            {
                Id = d.Id,
                Name = d.Name,
                HeadProfessorName = d.HeadProfessorName,
                TotalAcademics = _academics.Count(a => a.DepartmentId == d.Id),
                ResearchBudget = d.ResearchBudget,
                TeachingBudget = d.TeachingBudget
            }).ToList();

        return Result<List<DepartmentSummaryDto>>.Success(result);
    }

    // IRoomReadRepository implementation - explicit interface implementation
    async Task<Result<RoomDto?>> IRoomReadRepository.GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        await Task.Delay(1, cancellationToken);
        var room = _rooms.FirstOrDefault(r => r.Id == id);
        return Result<RoomDto?>.Success(room);
    }

    async Task<Result<RoomDto?>> IRoomReadRepository.GetByRoomNumberAsync(string roomNumber, CancellationToken cancellationToken)
    {
        await Task.Delay(1, cancellationToken);
        var room = _rooms.FirstOrDefault(r => r.RoomNumber.Equals(roomNumber, StringComparison.OrdinalIgnoreCase));
        return Result<RoomDto?>.Success(room);
    }

    async Task<Result<PagedResult<RoomDto>>> IRoomReadRepository.GetAllAsync(int pageNumber, int pageSize, string? roomNumberFilter, bool? isOccupiedFilter, CancellationToken cancellationToken)
    {
        await Task.Delay(1, cancellationToken);
        var query = _rooms.AsQueryable();

        if (!string.IsNullOrEmpty(roomNumberFilter))
            query = query.Where(r => r.RoomNumber.Contains(roomNumberFilter, StringComparison.OrdinalIgnoreCase));
        if (isOccupiedFilter.HasValue)
            query = query.Where(r => r.IsOccupied == isOccupiedFilter.Value);

        var totalCount = query.Count();
        var items = query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

        var result = new PagedResult<RoomDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        return Result<PagedResult<RoomDto>>.Success(result);
    }

    async Task<Result<List<RoomOccupancyDto>>> IRoomReadRepository.GetOccupancyAsync(Guid? buildingId, CancellationToken cancellationToken)
    {
        await Task.Delay(1, cancellationToken);
        var query = _rooms.AsQueryable();
        if (buildingId.HasValue)
            query = query.Where(r => r.BuildingId == buildingId.Value);

        var roomList = query.ToList();
        var occupancy = roomList.Select(r => new RoomOccupancyDto
        {
            RoomId = r.Id,
            RoomNumber = r.RoomNumber,
            BuildingName = r.BuildingName,
            IsOccupied = r.IsOccupied,
            OccupiedByAcademicId = r.OccupiedByAcademicId,
            OccupiedByAcademicName = r.OccupiedByAcademicName,
            OccupiedByEmpNr = r.IsOccupied ? _academics.FirstOrDefault(a => a.Id == r.OccupiedByAcademicId)?.EmpNr : null,
            OccupiedSince = r.IsOccupied ? DateTime.Now.AddDays(-30) : null
        }).ToList();

        return Result<List<RoomOccupancyDto>>.Success(occupancy);
    }

    async Task<Result<PagedResult<RoomDto>>> IRoomReadRepository.GetAvailableAsync(int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        await Task.Delay(1, cancellationToken);
        var query = _rooms.Where(r => !r.IsOccupied);
        var totalCount = query.Count();
        var items = query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

        var result = new PagedResult<RoomDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        return Result<PagedResult<RoomDto>>.Success(result);
    }

    async Task<Result<PagedResult<RoomDto>>> IRoomReadRepository.GetByBuildingAsync(Guid buildingId, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        await Task.Delay(1, cancellationToken);
        var query = _rooms.Where(r => r.BuildingId == buildingId);
        var totalCount = query.Count();
        var items = query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

        var result = new PagedResult<RoomDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        return Result<PagedResult<RoomDto>>.Success(result);
    }

    // IExtensionReadRepository implementation - explicit interface implementation
    async Task<Result<ExtensionDto?>> IExtensionReadRepository.GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        await Task.Delay(1, cancellationToken);
        var extension = _extensions.FirstOrDefault(e => e.Id == id);
        return Result<ExtensionDto?>.Success(extension);
    }

    async Task<Result<ExtensionDto?>> IExtensionReadRepository.GetByExtensionNumberAsync(string extensionNumber, CancellationToken cancellationToken)
    {
        await Task.Delay(1, cancellationToken);
        var extension = _extensions.FirstOrDefault(e => e.ExtensionNumber.Equals(extensionNumber, StringComparison.OrdinalIgnoreCase));
        return Result<ExtensionDto?>.Success(extension);
    }

    async Task<Result<PagedResult<ExtensionDto>>> IExtensionReadRepository.GetAllAsync(int pageNumber, int pageSize, string? extensionFilter, string? locationFilter, bool? isInUseFilter, CancellationToken cancellationToken)
    {
        await Task.Delay(1, cancellationToken);
        var query = _extensions.AsQueryable();

        if (!string.IsNullOrEmpty(extensionFilter))
            query = query.Where(e => e.ExtensionNumber.Contains(extensionFilter, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(locationFilter))
            query = query.Where(e => e.Location.Contains(locationFilter, StringComparison.OrdinalIgnoreCase));
        if (isInUseFilter.HasValue)
            query = query.Where(e => e.IsInUse == isInUseFilter.Value);

        var totalCount = query.Count();
        var items = query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

        var result = new PagedResult<ExtensionDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        return Result<PagedResult<ExtensionDto>>.Success(result);
    }

    async Task<Result<List<ExtensionAccessLevelDto>>> IExtensionReadRepository.GetAccessLevelAsync(Guid? extensionId, CancellationToken cancellationToken)
    {
        await Task.Delay(1, cancellationToken);
        var query = _extensions.AsQueryable();
        if (extensionId.HasValue)
            query = query.Where(e => e.Id == extensionId.Value);

        var extensionList = query.ToList();
        var accessLevels = extensionList.Select(e => new ExtensionAccessLevelDto
        {
            ExtensionId = e.Id,
            ExtensionNumber = e.ExtensionNumber,
            AccessLevel = e.AccessLevel,
            AccessLevelDescription = GetAccessLevelDescription(e.AccessLevel),
            AssignedDate = DateTime.Now.AddDays(-60),
            AssignedBy = "System Administrator"
        }).ToList();

        return Result<List<ExtensionAccessLevelDto>>.Success(accessLevels);
    }

    private static string GetAccessLevelDescription(string accessLevel)
    {
        return accessLevel switch
        {
            "Full" => "Full access with all features",
            "Standard" => "Standard access with basic features",
            "Limited" => "Limited access with restricted features",
            _ => "Unknown access level"
        };
    }

    async Task<Result<PagedResult<ExtensionDto>>> IExtensionReadRepository.GetAvailableAsync(string? accessLevelFilter, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        await Task.Delay(1, cancellationToken);
        var query = _extensions.Where(e => !e.IsInUse);

        if (!string.IsNullOrEmpty(accessLevelFilter))
            query = query.Where(e => e.AccessLevel.Equals(accessLevelFilter, StringComparison.OrdinalIgnoreCase));

        var totalCount = query.Count();
        var items = query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

        var result = new PagedResult<ExtensionDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        return Result<PagedResult<ExtensionDto>>.Success(result);
    }

    async Task<Result<PagedResult<ExtensionDto>>> IExtensionReadRepository.GetByAccessLevelAsync(string accessLevel, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        await Task.Delay(1, cancellationToken);
        var query = _extensions.Where(e => e.AccessLevel.Equals(accessLevel, StringComparison.OrdinalIgnoreCase));
        var totalCount = query.Count();
        var items = query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

        var result = new PagedResult<ExtensionDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        return Result<PagedResult<ExtensionDto>>.Success(result);
    }
}
