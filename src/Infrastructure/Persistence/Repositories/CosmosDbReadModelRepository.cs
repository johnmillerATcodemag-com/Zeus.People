using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zeus.People.Application.Common;
using Zeus.People.Application.DTOs;
using Zeus.People.Application.Interfaces;
using Zeus.People.Infrastructure.Configuration;

namespace Zeus.People.Infrastructure.Persistence.Repositories;

/// <summary>
/// Cosmos DB implementation for read model operations
/// </summary>
public class CosmosDbReadModelRepository : IAcademicReadRepository, IDepartmentReadRepository, IRoomReadRepository, IExtensionReadRepository
{
    private readonly CosmosClient _cosmosClient;
    private readonly Database _database;
    private readonly Container _academicsContainer;
    private readonly Container _departmentsContainer;
    private readonly Container _roomsContainer;
    private readonly Container _extensionsContainer;
    private readonly ILogger<CosmosDbReadModelRepository> _logger;
    private readonly CosmosDbConfiguration _configuration;

    public CosmosDbReadModelRepository(
        CosmosClient cosmosClient,
        IOptions<CosmosDbConfiguration> configuration,
        ILogger<CosmosDbReadModelRepository> logger)
    {
        _cosmosClient = cosmosClient ?? throw new ArgumentNullException(nameof(cosmosClient));
        _configuration = configuration.Value ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _database = _cosmosClient.GetDatabase(_configuration.DatabaseName);
        _academicsContainer = _database.GetContainer("academics");
        _departmentsContainer = _database.GetContainer("departments");
        _roomsContainer = _database.GetContainer("rooms");
        _extensionsContainer = _database.GetContainer("extensions");
    }

    #region Academic Read Operations

    public async Task<Result<AcademicDto?>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _academicsContainer.ReadItemAsync<AcademicDto>(
                id.ToString(),
                new PartitionKey(id.ToString()),
                cancellationToken: cancellationToken);

            return Result.Success<AcademicDto?>(response.Resource);
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return Result.Success<AcademicDto?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get academic by ID {AcademicId}", id);
            return Result.Failure<AcademicDto?>(new Error("AcademicReadModel.RetrievalError", $"Failed to get academic: {ex.Message}"));
        }
    }

    public async Task<Result<AcademicDto?>> GetByEmpNrAsync(string empNr, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.empNr = @empNr")
                .WithParameter("@empNr", empNr);

            var response = await _academicsContainer.GetItemQueryIterator<AcademicDto>(query)
                .ReadNextAsync(cancellationToken);

            return Result.Success<AcademicDto?>(response.FirstOrDefault());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get academic by employee number {EmpNr}", empNr);
            return Result.Failure<AcademicDto?>(new Error("AcademicReadModel.EmpNrRetrievalError", $"Failed to get academic: {ex.Message}"));
        }
    }

    public async Task<Result<PagedResult<AcademicSummaryDto>>> GetAllAsync(
        int pageNumber,
        int pageSize,
        string? nameFilter = null,
        string? rankFilter = null,
        bool? isTenuredFilter = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var whereClause = BuildAcademicWhereClause(nameFilter, rankFilter, isTenuredFilter);

            // Get total count
            var countQuery = new QueryDefinition($"SELECT VALUE COUNT(1) FROM c {whereClause}");
            var countResponse = await _academicsContainer.GetItemQueryIterator<int>(countQuery)
                .ReadNextAsync(cancellationToken);
            var totalCount = countResponse.FirstOrDefault();

            // Get paginated data
            var dataQuery = new QueryDefinition($"SELECT * FROM c {whereClause} ORDER BY c.lastName, c.firstName OFFSET @offset LIMIT @limit")
                .WithParameter("@offset", (pageNumber - 1) * pageSize)
                .WithParameter("@limit", pageSize);

            var dataResponse = await _academicsContainer.GetItemQueryIterator<AcademicSummaryDto>(dataQuery)
                .ReadNextAsync(cancellationToken);

            var result = new PagedResult<AcademicSummaryDto>
            {
                Items = dataResponse.ToList(),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return Result<PagedResult<AcademicSummaryDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all academics");
            return Result.Failure<PagedResult<AcademicSummaryDto>>(new Error("AcademicReadModel.GetAllError", $"Failed to get academics: {ex.Message}"));
        }
    }

    public async Task<Result<PagedResult<AcademicSummaryDto>>> GetByDepartmentAsync(
        Guid departmentId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get total count
            var countQuery = new QueryDefinition("SELECT VALUE COUNT(1) FROM c WHERE c.departmentId = @departmentId")
                .WithParameter("@departmentId", departmentId);
            var countResponse = await _academicsContainer.GetItemQueryIterator<int>(countQuery)
                .ReadNextAsync(cancellationToken);
            var totalCount = countResponse.FirstOrDefault();

            // Get paginated data
            var dataQuery = new QueryDefinition("SELECT * FROM c WHERE c.departmentId = @departmentId ORDER BY c.lastName, c.firstName OFFSET @offset LIMIT @limit")
                .WithParameter("@departmentId", departmentId)
                .WithParameter("@offset", (pageNumber - 1) * pageSize)
                .WithParameter("@limit", pageSize);

            var dataResponse = await _academicsContainer.GetItemQueryIterator<AcademicSummaryDto>(dataQuery)
                .ReadNextAsync(cancellationToken);

            var result = new PagedResult<AcademicSummaryDto>
            {
                Items = dataResponse.ToList(),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return Result<PagedResult<AcademicSummaryDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get academics by department {DepartmentId}", departmentId);
            return Result.Failure<PagedResult<AcademicSummaryDto>>(new Error("Cosmos.AcademicRetrievalError", $"Failed to get academics: {ex.Message}"));
        }
    }

    public async Task<Result<PagedResult<AcademicSummaryDto>>> GetByRankAsync(
        string rank,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get total count
            var countQuery = new QueryDefinition("SELECT VALUE COUNT(1) FROM c WHERE c.rank = @rank")
                .WithParameter("@rank", rank);
            var countResponse = await _academicsContainer.GetItemQueryIterator<int>(countQuery)
                .ReadNextAsync(cancellationToken);
            var totalCount = countResponse.FirstOrDefault();

            // Get paginated data
            var dataQuery = new QueryDefinition("SELECT * FROM c WHERE c.rank = @rank ORDER BY c.lastName, c.firstName OFFSET @offset LIMIT @limit")
                .WithParameter("@rank", rank)
                .WithParameter("@offset", (pageNumber - 1) * pageSize)
                .WithParameter("@limit", pageSize);

            var dataResponse = await _academicsContainer.GetItemQueryIterator<AcademicSummaryDto>(dataQuery)
                .ReadNextAsync(cancellationToken);

            var result = new PagedResult<AcademicSummaryDto>
            {
                Items = dataResponse.ToList(),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return Result<PagedResult<AcademicSummaryDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get academics by rank {Rank}", rank);
            return Result.Failure<PagedResult<AcademicSummaryDto>>(new Error("Cosmos.AcademicRetrievalError", $"Failed to get academics: {ex.Message}"));
        }
    }

    public async Task<Result<PagedResult<AcademicSummaryDto>>> GetTenuredAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get total count
            var countQuery = new QueryDefinition("SELECT VALUE COUNT(1) FROM c WHERE c.isTenured = true");
            var countResponse = await _academicsContainer.GetItemQueryIterator<int>(countQuery)
                .ReadNextAsync(cancellationToken);
            var totalCount = countResponse.FirstOrDefault();

            // Get paginated data
            var dataQuery = new QueryDefinition("SELECT * FROM c WHERE c.isTenured = true ORDER BY c.lastName, c.firstName OFFSET @offset LIMIT @limit")
                .WithParameter("@offset", (pageNumber - 1) * pageSize)
                .WithParameter("@limit", pageSize);

            var dataResponse = await _academicsContainer.GetItemQueryIterator<AcademicSummaryDto>(dataQuery)
                .ReadNextAsync(cancellationToken);

            var result = new PagedResult<AcademicSummaryDto>
            {
                Items = dataResponse.ToList(),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return Result<PagedResult<AcademicSummaryDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get tenured academics");
            return Result.Failure<PagedResult<AcademicSummaryDto>>(new Error("Cosmos.AcademicRetrievalError", $"Failed to get academics: {ex.Message}"));
        }
    }

    public async Task<Result<PagedResult<AcademicSummaryDto>>> GetWithExpiringContractsAsync(
        DateTime? beforeDate,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var expiryDate = beforeDate ?? DateTime.UtcNow.AddMonths(6);

            // Get total count
            var countQuery = new QueryDefinition("SELECT VALUE COUNT(1) FROM c WHERE c.contractEndDate <= @expiryDate")
                .WithParameter("@expiryDate", expiryDate);
            var countResponse = await _academicsContainer.GetItemQueryIterator<int>(countQuery)
                .ReadNextAsync(cancellationToken);
            var totalCount = countResponse.FirstOrDefault();

            // Get paginated data
            var dataQuery = new QueryDefinition("SELECT * FROM c WHERE c.contractEndDate <= @expiryDate ORDER BY c.contractEndDate OFFSET @offset LIMIT @limit")
                .WithParameter("@expiryDate", expiryDate)
                .WithParameter("@offset", (pageNumber - 1) * pageSize)
                .WithParameter("@limit", pageSize);

            var dataResponse = await _academicsContainer.GetItemQueryIterator<AcademicSummaryDto>(dataQuery)
                .ReadNextAsync(cancellationToken);

            var result = new PagedResult<AcademicSummaryDto>
            {
                Items = dataResponse.ToList(),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return Result<PagedResult<AcademicSummaryDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get academics with expiring contracts");
            return Result.Failure<PagedResult<AcademicSummaryDto>>(new Error("Cosmos.AcademicRetrievalError", $"Failed to get academics: {ex.Message}"));
        }
    }

    public async Task<Result<List<AcademicCountByDepartmentDto>>> GetCountByDepartmentAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryDefinition(@"
                SELECT c.departmentId, c.departmentName, COUNT(1) as count 
                FROM c 
                GROUP BY c.departmentId, c.departmentName");

            var response = await _academicsContainer.GetItemQueryIterator<AcademicCountByDepartmentDto>(query)
                .ReadNextAsync(cancellationToken);

            return Result<List<AcademicCountByDepartmentDto>>.Success(response.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get academic count by department");
            return Result.Failure<List<AcademicCountByDepartmentDto>>(new Error("Cosmos.AcademicCountError", $"Failed to get count: {ex.Message}"));
        }
    }

    #endregion

    #region Department Read Operations

    async Task<Result<DepartmentDto?>> IDepartmentReadRepository.GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _departmentsContainer.ReadItemAsync<DepartmentDto>(
                id.ToString(),
                new PartitionKey(id.ToString()),
                cancellationToken: cancellationToken);

            return Result.Success<DepartmentDto?>(response.Resource);
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return Result.Success<DepartmentDto?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get department by ID {DepartmentId}", id);
            return Result.Failure<DepartmentDto?>(new Error("ReadModel.Error", $"Failed to get department: {ex.Message}"));
        }
    }

    public async Task<Result<DepartmentDto?>> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.name = @name")
                .WithParameter("@name", name);

            var response = await _departmentsContainer.GetItemQueryIterator<DepartmentDto>(query)
                .ReadNextAsync(cancellationToken);

            return Result.Success(response.FirstOrDefault());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get department by name {Name}", name);
            return Result.Failure<DepartmentDto?>(new Error("Department.GetByNameFailed", $"Failed to get department: {ex.Message}"));
        }
    }

    public async Task<Result<PagedResult<DepartmentSummaryDto>>> GetAllAsync(
        int pageNumber,
        int pageSize,
        string? nameFilter = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var whereClause = string.IsNullOrEmpty(nameFilter) ? "" : "WHERE CONTAINS(LOWER(c.name), LOWER(@nameFilter))";

            // Get total count
            var countQuery = new QueryDefinition($"SELECT VALUE COUNT(1) FROM c {whereClause}");
            if (!string.IsNullOrEmpty(nameFilter))
                countQuery = countQuery.WithParameter("@nameFilter", nameFilter);

            var countResponse = await _departmentsContainer.GetItemQueryIterator<int>(countQuery)
                .ReadNextAsync(cancellationToken);
            var totalCount = countResponse.FirstOrDefault();

            // Get paginated data
            var dataQuery = new QueryDefinition($"SELECT * FROM c {whereClause} ORDER BY c.name OFFSET @offset LIMIT @limit")
                .WithParameter("@offset", (pageNumber - 1) * pageSize)
                .WithParameter("@limit", pageSize);
            if (!string.IsNullOrEmpty(nameFilter))
                dataQuery = dataQuery.WithParameter("@nameFilter", nameFilter);

            var dataResponse = await _departmentsContainer.GetItemQueryIterator<DepartmentSummaryDto>(dataQuery)
                .ReadNextAsync(cancellationToken);

            var result = new PagedResult<DepartmentSummaryDto>
            {
                Items = dataResponse.ToList(),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return Result<PagedResult<DepartmentSummaryDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all departments");
            return Result.Failure<PagedResult<DepartmentSummaryDto>>(new Error("Department.GetAllFailed", $"Failed to get departments: {ex.Message}"));
        }
    }

    public async Task<Result<DepartmentStaffCountDto?>> GetStaffCountAsync(Guid departmentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryDefinition("SELECT c.id, c.name, c.staffCount FROM c WHERE c.id = @departmentId")
                .WithParameter("@departmentId", departmentId);

            var response = await _departmentsContainer.GetItemQueryIterator<DepartmentStaffCountDto>(query)
                .ReadNextAsync(cancellationToken);

            var result = response.FirstOrDefault();
            return Result.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get staff count for department {DepartmentId}", departmentId);
            return Result.Failure<DepartmentStaffCountDto?>(new Error("Department.StaffCountFailed", $"Failed to get staff count: {ex.Message}"));
        }
    }

    public async Task<Result<List<DepartmentStaffCountDto>>> GetAllStaffCountsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryDefinition("SELECT c.id, c.name, c.staffCount FROM c");

            var response = await _departmentsContainer.GetItemQueryIterator<DepartmentStaffCountDto>(query)
                .ReadNextAsync(cancellationToken);

            return Result<List<DepartmentStaffCountDto>>.Success(response.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all staff counts");
            return Result.Failure<List<DepartmentStaffCountDto>>(new Error("Department.GetAllStaffCountsFailed", $"Failed to get staff counts: {ex.Message}"));
        }
    }

    public async Task<Result<List<DepartmentSummaryDto>>> GetWithBudgetAsync(
        decimal? minBudget,
        decimal? maxBudget,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var conditions = new List<string>();
            var queryDef = new QueryDefinition("SELECT * FROM c");

            if (minBudget.HasValue)
            {
                conditions.Add("c.budget >= @minBudget");
                queryDef = queryDef.WithParameter("@minBudget", minBudget.Value);
            }

            if (maxBudget.HasValue)
            {
                conditions.Add("c.budget <= @maxBudget");
                queryDef = queryDef.WithParameter("@maxBudget", maxBudget.Value);
            }

            if (conditions.Any())
            {
                queryDef = new QueryDefinition($"SELECT * FROM c WHERE {string.Join(" AND ", conditions)}");
                if (minBudget.HasValue)
                    queryDef = queryDef.WithParameter("@minBudget", minBudget.Value);
                if (maxBudget.HasValue)
                    queryDef = queryDef.WithParameter("@maxBudget", maxBudget.Value);
            }

            var response = await _departmentsContainer.GetItemQueryIterator<DepartmentSummaryDto>(queryDef)
                .ReadNextAsync(cancellationToken);

            return Result<List<DepartmentSummaryDto>>.Success(response.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get departments with budget filter");
            return Result.Failure<List<DepartmentSummaryDto>>(new Error("Department.GetWithBudgetFailed", $"Failed to get departments: {ex.Message}"));
        }
    }

    public async Task<Result<List<DepartmentSummaryDto>>> GetWithoutHeadsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.headId = null OR NOT IS_DEFINED(c.headId)");

            var response = await _departmentsContainer.GetItemQueryIterator<DepartmentSummaryDto>(query)
                .ReadNextAsync(cancellationToken);

            return Result<List<DepartmentSummaryDto>>.Success(response.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get departments without heads");
            return Result.Failure<List<DepartmentSummaryDto>>(new Error("Department.GetWithoutHeadsFailed", $"Failed to get departments: {ex.Message}"));
        }
    }

    #endregion

    #region Room Read Operations

    Task<Result<RoomDto?>> IRoomReadRepository.GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return GetRoomByIdAsync(id, cancellationToken);
    }

    public async Task<Result<RoomDto?>> GetRoomByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _roomsContainer.ReadItemAsync<RoomDto>(
                id.ToString(),
                new PartitionKey(id.ToString()),
                cancellationToken: cancellationToken);

            return Result.Success<RoomDto?>(response.Resource);
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return Result.Success<RoomDto?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get room by ID {RoomId}", id);
            return Result.Failure<RoomDto?>(new Error("Room.GetByIdFailed", $"Failed to get room: {ex.Message}"));
        }
    }

    public async Task<Result<RoomDto?>> GetByRoomNumberAsync(string roomNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.roomNumber = @roomNumber")
                .WithParameter("@roomNumber", roomNumber);

            var response = await _roomsContainer.GetItemQueryIterator<RoomDto>(query)
                .ReadNextAsync(cancellationToken);

            return Result.Success<RoomDto?>(response.FirstOrDefault());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get room by number {RoomNumber}", roomNumber);
            return Result.Failure<RoomDto?>(new Error("ReadModel.Error", $"Failed to get room: {ex.Message}"));
        }
    }

    public async Task<Result<PagedResult<RoomDto>>> GetAllAsync(
        int pageNumber,
        int pageSize,
        string? roomNumberFilter = null,
        bool? isOccupiedFilter = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var conditions = new List<string>();
            var queryDef = new QueryDefinition("SELECT * FROM c");

            if (!string.IsNullOrEmpty(roomNumberFilter))
            {
                conditions.Add("CONTAINS(LOWER(c.roomNumber), LOWER(@roomNumberFilter))");
            }

            if (isOccupiedFilter.HasValue)
            {
                conditions.Add("c.isOccupied = @isOccupiedFilter");
            }

            var whereClause = conditions.Any() ? $"WHERE {string.Join(" AND ", conditions)}" : "";

            // Get total count
            var countQuery = new QueryDefinition($"SELECT VALUE COUNT(1) FROM c {whereClause}");
            if (!string.IsNullOrEmpty(roomNumberFilter))
                countQuery = countQuery.WithParameter("@roomNumberFilter", roomNumberFilter);
            if (isOccupiedFilter.HasValue)
                countQuery = countQuery.WithParameter("@isOccupiedFilter", isOccupiedFilter.Value);

            var countResponse = await _roomsContainer.GetItemQueryIterator<int>(countQuery)
                .ReadNextAsync(cancellationToken);
            var totalCount = countResponse.FirstOrDefault();

            // Get paginated data
            var dataQuery = new QueryDefinition($"SELECT * FROM c {whereClause} ORDER BY c.roomNumber OFFSET @offset LIMIT @limit")
                .WithParameter("@offset", (pageNumber - 1) * pageSize)
                .WithParameter("@limit", pageSize);
            if (!string.IsNullOrEmpty(roomNumberFilter))
                dataQuery = dataQuery.WithParameter("@roomNumberFilter", roomNumberFilter);
            if (isOccupiedFilter.HasValue)
                dataQuery = dataQuery.WithParameter("@isOccupiedFilter", isOccupiedFilter.Value);

            var dataResponse = await _roomsContainer.GetItemQueryIterator<RoomDto>(dataQuery)
                .ReadNextAsync(cancellationToken);

            var result = new PagedResult<RoomDto>
            {
                Items = dataResponse.ToList(),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return Result<PagedResult<RoomDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all rooms");
            return Result.Failure<PagedResult<RoomDto>>(new Error("Cosmos.RoomRetrievalError", $"Failed to get rooms: {ex.Message}"));
        }
    }

    public async Task<Result<List<RoomOccupancyDto>>> GetOccupancyAsync(Guid? roomId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var queryText = roomId.HasValue
                ? "SELECT c.id as roomId, c.roomNumber, c.buildingName, c.isOccupied, c.occupiedByAcademicId, c.occupiedByAcademicName, c.occupiedByEmpNr, c.occupiedSince FROM c WHERE c.id = @roomId"
                : "SELECT c.id as roomId, c.roomNumber, c.buildingName, c.isOccupied, c.occupiedByAcademicId, c.occupiedByAcademicName, c.occupiedByEmpNr, c.occupiedSince FROM c";

            var query = new QueryDefinition(queryText);
            if (roomId.HasValue)
                query = query.WithParameter("@roomId", roomId.Value);

            var response = await _roomsContainer.GetItemQueryIterator<RoomOccupancyDto>(query)
                .ReadNextAsync(cancellationToken);

            return Result<List<RoomOccupancyDto>>.Success(response.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get room occupancy");
            return Result.Failure<List<RoomOccupancyDto>>(new Error("Cosmos.RoomOccupancyError", $"Failed to get occupancy: {ex.Message}"));
        }
    }

    public async Task<Result<PagedResult<RoomDto>>> GetAvailableAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get total count
            var countQuery = new QueryDefinition("SELECT VALUE COUNT(1) FROM c WHERE c.isOccupied = false");
            var countResponse = await _roomsContainer.GetItemQueryIterator<int>(countQuery)
                .ReadNextAsync(cancellationToken);
            var totalCount = countResponse.FirstOrDefault();

            // Get paginated data
            var dataQuery = new QueryDefinition("SELECT * FROM c WHERE c.isOccupied = false ORDER BY c.roomNumber OFFSET @offset LIMIT @limit")
                .WithParameter("@offset", (pageNumber - 1) * pageSize)
                .WithParameter("@limit", pageSize);

            var dataResponse = await _roomsContainer.GetItemQueryIterator<RoomDto>(dataQuery)
                .ReadNextAsync(cancellationToken);

            var result = new PagedResult<RoomDto>
            {
                Items = dataResponse.ToList(),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return Result<PagedResult<RoomDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get available rooms");
            return Result.Failure<PagedResult<RoomDto>>(new Error("Cosmos.RoomRetrievalError", $"Failed to get rooms: {ex.Message}"));
        }
    }

    public async Task<Result<PagedResult<RoomDto>>> GetByBuildingAsync(
        Guid buildingId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get total count
            var countQuery = new QueryDefinition("SELECT VALUE COUNT(1) FROM c WHERE c.buildingId = @buildingId")
                .WithParameter("@buildingId", buildingId);
            var countResponse = await _roomsContainer.GetItemQueryIterator<int>(countQuery)
                .ReadNextAsync(cancellationToken);
            var totalCount = countResponse.FirstOrDefault();

            // Get paginated data
            var dataQuery = new QueryDefinition("SELECT * FROM c WHERE c.buildingId = @buildingId ORDER BY c.roomNumber OFFSET @offset LIMIT @limit")
                .WithParameter("@buildingId", buildingId)
                .WithParameter("@offset", (pageNumber - 1) * pageSize)
                .WithParameter("@limit", pageSize);

            var dataResponse = await _roomsContainer.GetItemQueryIterator<RoomDto>(dataQuery)
                .ReadNextAsync(cancellationToken);

            var result = new PagedResult<RoomDto>
            {
                Items = dataResponse.ToList(),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return Result<PagedResult<RoomDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get rooms by building {BuildingId}", buildingId);
            return Result.Failure<PagedResult<RoomDto>>(new Error("Cosmos.RoomRetrievalError", $"Failed to get rooms: {ex.Message}"));
        }
    }

    #endregion

    #region Extension Read Operations

    Task<Result<ExtensionDto?>> IExtensionReadRepository.GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return GetExtensionByIdAsync(id, cancellationToken);
    }

    public async Task<Result<ExtensionDto?>> GetExtensionByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _extensionsContainer.ReadItemAsync<ExtensionDto>(
                id.ToString(),
                new PartitionKey(id.ToString()),
                cancellationToken: cancellationToken);

            return Result.Success<ExtensionDto?>(response.Resource);
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return Result.Success<ExtensionDto?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get extension by ID {ExtensionId}", id);
            return Result.Failure<ExtensionDto?>(new Error("ReadModel.Error", $"Failed to get extension: {ex.Message}"));
        }
    }

    public async Task<Result<ExtensionDto?>> GetByExtensionNumberAsync(string extensionNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.extensionNumber = @extensionNumber")
                .WithParameter("@extensionNumber", extensionNumber);

            var response = await _extensionsContainer.GetItemQueryIterator<ExtensionDto>(query)
                .ReadNextAsync(cancellationToken);

            return Result.Success<ExtensionDto?>(response.FirstOrDefault());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get extension by number {ExtensionNumber}", extensionNumber);
            return Result.Failure<ExtensionDto?>(new Error("ReadModel.Error", $"Failed to get extension: {ex.Message}"));
        }
    }

    async Task<Result<PagedResult<ExtensionDto>>> IExtensionReadRepository.GetAllAsync(
        int pageNumber,
        int pageSize,
        string? extensionNumberFilter,
        string? accessLevelFilter,
        bool? isInUseFilter,
        CancellationToken cancellationToken)
    {
        try
        {
            var conditions = new List<string>();

            if (!string.IsNullOrEmpty(extensionNumberFilter))
            {
                conditions.Add("CONTAINS(LOWER(c.extensionNumber), LOWER(@extensionNumberFilter))");
            }

            if (!string.IsNullOrEmpty(accessLevelFilter))
            {
                conditions.Add("c.accessLevel = @accessLevelFilter");
            }

            if (isInUseFilter.HasValue)
            {
                conditions.Add("c.isInUse = @isInUseFilter");
            }

            var whereClause = conditions.Any() ? $"WHERE {string.Join(" AND ", conditions)}" : "";

            // Get total count
            var countQuery = new QueryDefinition($"SELECT VALUE COUNT(1) FROM c {whereClause}");
            if (!string.IsNullOrEmpty(extensionNumberFilter))
                countQuery = countQuery.WithParameter("@extensionNumberFilter", extensionNumberFilter);
            if (!string.IsNullOrEmpty(accessLevelFilter))
                countQuery = countQuery.WithParameter("@accessLevelFilter", accessLevelFilter);
            if (isInUseFilter.HasValue)
                countQuery = countQuery.WithParameter("@isInUseFilter", isInUseFilter.Value);

            var countResponse = await _extensionsContainer.GetItemQueryIterator<int>(countQuery)
                .ReadNextAsync(cancellationToken);
            var totalCount = countResponse.FirstOrDefault();

            // Get paginated data
            var dataQuery = new QueryDefinition($"SELECT * FROM c {whereClause} ORDER BY c.extensionNumber OFFSET @offset LIMIT @limit")
                .WithParameter("@offset", (pageNumber - 1) * pageSize)
                .WithParameter("@limit", pageSize);
            if (!string.IsNullOrEmpty(extensionNumberFilter))
                dataQuery = dataQuery.WithParameter("@extensionNumberFilter", extensionNumberFilter);
            if (!string.IsNullOrEmpty(accessLevelFilter))
                dataQuery = dataQuery.WithParameter("@accessLevelFilter", accessLevelFilter);
            if (isInUseFilter.HasValue)
                dataQuery = dataQuery.WithParameter("@isInUseFilter", isInUseFilter.Value);

            var dataResponse = await _extensionsContainer.GetItemQueryIterator<ExtensionDto>(dataQuery)
                .ReadNextAsync(cancellationToken);

            var result = new PagedResult<ExtensionDto>
            {
                Items = dataResponse.ToList(),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return Result<PagedResult<ExtensionDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all extensions");
            return Result.Failure<PagedResult<ExtensionDto>>(new Error("Cosmos.ExtensionRetrievalError", $"Failed to get extensions: {ex.Message}"));
        }
    }

    public async Task<Result<List<ExtensionAccessLevelDto>>> GetAccessLevelAsync(Guid? extensionId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var queryText = extensionId.HasValue
                ? "SELECT c.id as extensionId, c.extensionNumber, c.accessLevel, c.accessLevelDescription, c.isInUse, c.usedByAcademicName FROM c WHERE c.id = @extensionId"
                : "SELECT c.id as extensionId, c.extensionNumber, c.accessLevel, c.accessLevelDescription, c.isInUse, c.usedByAcademicName FROM c";

            var query = new QueryDefinition(queryText);
            if (extensionId.HasValue)
                query = query.WithParameter("@extensionId", extensionId.Value);

            var response = await _extensionsContainer.GetItemQueryIterator<ExtensionAccessLevelDto>(query)
                .ReadNextAsync(cancellationToken);

            return Result<List<ExtensionAccessLevelDto>>.Success(response.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get extension access levels");
            return Result.Failure<List<ExtensionAccessLevelDto>>(new Error("Cosmos.ExtensionAccessLevelError", $"Failed to get access levels: {ex.Message}"));
        }
    }

    public async Task<Result<PagedResult<ExtensionDto>>> GetAvailableAsync(
        string? accessLevelFilter = null,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var whereClause = "WHERE c.isInUse = false";
            if (!string.IsNullOrEmpty(accessLevelFilter))
            {
                whereClause += " AND c.accessLevel = @accessLevelFilter";
            }

            // Get total count
            var countQuery = new QueryDefinition($"SELECT VALUE COUNT(1) FROM c {whereClause}");
            if (!string.IsNullOrEmpty(accessLevelFilter))
                countQuery = countQuery.WithParameter("@accessLevelFilter", accessLevelFilter);

            var countResponse = await _extensionsContainer.GetItemQueryIterator<int>(countQuery)
                .ReadNextAsync(cancellationToken);
            var totalCount = countResponse.FirstOrDefault();

            // Get paginated data
            var dataQuery = new QueryDefinition($"SELECT * FROM c {whereClause} ORDER BY c.extensionNumber OFFSET @offset LIMIT @limit")
                .WithParameter("@offset", (pageNumber - 1) * pageSize)
                .WithParameter("@limit", pageSize);
            if (!string.IsNullOrEmpty(accessLevelFilter))
                dataQuery = dataQuery.WithParameter("@accessLevelFilter", accessLevelFilter);

            var dataResponse = await _extensionsContainer.GetItemQueryIterator<ExtensionDto>(dataQuery)
                .ReadNextAsync(cancellationToken);

            var result = new PagedResult<ExtensionDto>
            {
                Items = dataResponse.ToList(),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return Result<PagedResult<ExtensionDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get available extensions");
            return Result.Failure<PagedResult<ExtensionDto>>(new Error("Cosmos.ExtensionRetrievalError", $"Failed to get extensions: {ex.Message}"));
        }
    }

    public async Task<Result<PagedResult<ExtensionDto>>> GetByAccessLevelAsync(
        string accessLevelCode,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get total count
            var countQuery = new QueryDefinition("SELECT VALUE COUNT(1) FROM c WHERE c.accessLevel = @accessLevelCode")
                .WithParameter("@accessLevelCode", accessLevelCode);
            var countResponse = await _extensionsContainer.GetItemQueryIterator<int>(countQuery)
                .ReadNextAsync(cancellationToken);
            var totalCount = countResponse.FirstOrDefault();

            // Get paginated data
            var dataQuery = new QueryDefinition("SELECT * FROM c WHERE c.accessLevel = @accessLevelCode ORDER BY c.extensionNumber OFFSET @offset LIMIT @limit")
                .WithParameter("@accessLevelCode", accessLevelCode)
                .WithParameter("@offset", (pageNumber - 1) * pageSize)
                .WithParameter("@limit", pageSize);

            var dataResponse = await _extensionsContainer.GetItemQueryIterator<ExtensionDto>(dataQuery)
                .ReadNextAsync(cancellationToken);

            var result = new PagedResult<ExtensionDto>
            {
                Items = dataResponse.ToList(),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return Result<PagedResult<ExtensionDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get extensions by access level {AccessLevel}", accessLevelCode);
            return Result.Failure<PagedResult<ExtensionDto>>(new Error("Cosmos.ExtensionRetrievalError", $"Failed to get extensions: {ex.Message}"));
        }
    }

    #endregion

    #region Private Helper Methods

    private static string BuildAcademicWhereClause(string? nameFilter, string? rankFilter, bool? isTenuredFilter)
    {
        var conditions = new List<string>();

        if (!string.IsNullOrEmpty(nameFilter))
        {
            conditions.Add("(CONTAINS(LOWER(c.firstName), LOWER(@nameFilter)) OR CONTAINS(LOWER(c.lastName), LOWER(@nameFilter)))");
        }

        if (!string.IsNullOrEmpty(rankFilter))
        {
            conditions.Add("c.rank = @rankFilter");
        }

        if (isTenuredFilter.HasValue)
        {
            conditions.Add("c.isTenured = @isTenuredFilter");
        }

        return conditions.Any() ? $"WHERE {string.Join(" AND ", conditions)}" : "";
    }

    #endregion
}
