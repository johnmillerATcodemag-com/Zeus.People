using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zeus.People.API.Controllers;
using Zeus.People.Application.DTOs;
using Zeus.People.Application.Queries.Academic;
using Zeus.People.Application.Queries.Department;
using Zeus.People.Application.Queries.Extension;
using Zeus.People.Application.Queries.Room;

namespace Zeus.People.API.Controllers;

/// <summary>
/// Reports controller for aggregated data and analytics
/// </summary>
[Authorize]
public class ReportsController : BaseController
{
    /// <summary>
    /// Get academic statistics
    /// </summary>
    /// <returns>Academic statistics</returns>
    [HttpGet("academics/stats")]
    public async Task<IActionResult> GetAcademicStats()
    {
        var tenuredQuery = new GetTenuredAcademicsQuery();
        var countByDeptQuery = new GetAcademicCountByDepartmentQuery();
        var allAcademicsQuery = new GetAllAcademicsQuery();

        var tenuredResult = await Mediator.Send(tenuredQuery);
        var countByDeptResult = await Mediator.Send(countByDeptQuery);
        var allAcademicsResult = await Mediator.Send(allAcademicsQuery);

        if (!tenuredResult.IsSuccess || !countByDeptResult.IsSuccess || !allAcademicsResult.IsSuccess)
        {
            return BadRequest("Failed to retrieve academic statistics");
        }

        var stats = new
        {
            TotalAcademics = allAcademicsResult.Value?.TotalCount ?? 0,
            TenuredAcademics = tenuredResult.Value?.TotalCount ?? 0,
            NonTenuredAcademics = (allAcademicsResult.Value?.TotalCount ?? 0) - (tenuredResult.Value?.TotalCount ?? 0),
            AcademicsByDepartment = countByDeptResult.Value
        };

        return Ok(stats);
    }

    /// <summary>
    /// Get department utilization report
    /// </summary>
    /// <returns>Department utilization statistics</returns>
    [HttpGet("departments/utilization")]
    public async Task<IActionResult> GetDepartmentUtilization()
    {
        var departmentsQuery = new GetAllDepartmentsQuery();
        var departmentsResult = await Mediator.Send(departmentsQuery);

        if (!departmentsResult.IsSuccess)
        {
            return BadRequest("Failed to retrieve department data");
        }

        var utilizationStats = new List<object>();

        foreach (var dept in departmentsResult.Value?.Items ?? new List<DepartmentSummaryDto>())
        {
            var deptWithAcademicsQuery = new GetDepartmentQuery(dept.Id);
            var deptResult = await Mediator.Send(deptWithAcademicsQuery);

            if (deptResult.IsSuccess)
            {
                var deptData = (dynamic)deptResult.Value!;
                utilizationStats.Add(new
                {
                    DepartmentName = deptData.Name,
                    Building = deptData.Building,
                    AcademicCount = deptData.Academics?.Count ?? 0,
                    HasChair = deptData.ChairId != null
                });
            }
        }

        return Ok(utilizationStats);
    }

    /// <summary>
    /// Get room occupancy report
    /// </summary>
    /// <returns>Room occupancy statistics</returns>
    [HttpGet("rooms/occupancy")]
    public async Task<IActionResult> GetRoomOccupancy()
    {
        var roomsQuery = new GetAllRoomsQuery();
        var availableRoomsQuery = new GetAvailableRoomsQuery();

        var roomsResult = await Mediator.Send(roomsQuery);
        var availableRoomsResult = await Mediator.Send(availableRoomsQuery);

        if (!roomsResult.IsSuccess || !availableRoomsResult.IsSuccess)
        {
            return BadRequest("Failed to retrieve room data");
        }

        var totalRooms = roomsResult.Value?.TotalCount ?? 0;
        var availableRooms = availableRoomsResult.Value?.TotalCount ?? 0;
        var occupiedRooms = totalRooms - availableRooms;

        var occupancyStats = new
        {
            TotalRooms = totalRooms,
            OccupiedRooms = occupiedRooms,
            AvailableRooms = availableRooms,
            OccupancyRate = totalRooms > 0 ? (double)occupiedRooms / totalRooms * 100 : 0
        };

        return Ok(occupancyStats);
    }

    /// <summary>
    /// Get extension usage report
    /// </summary>
    /// <returns>Extension usage statistics</returns>
    [HttpGet("extensions/usage")]
    public async Task<IActionResult> GetExtensionUsage()
    {
        var extensionsQuery = new Zeus.People.Application.Queries.Extension.GetAllExtensionsQuery();
        var availableExtensionsQuery = new Zeus.People.Application.Queries.Extension.GetAvailableExtensionsQuery();

        var extensionsResult = await Mediator.Send(extensionsQuery);
        var availableExtensionsResult = await Mediator.Send(availableExtensionsQuery);

        if (!extensionsResult.IsSuccess || !availableExtensionsResult.IsSuccess)
        {
            return BadRequest("Failed to retrieve extension data");
        }

        var totalExtensions = extensionsResult.Value?.TotalCount ?? 0;
        var availableExtensions = availableExtensionsResult.Value?.TotalCount ?? 0;
        var usedExtensions = totalExtensions - availableExtensions;

        var usageStats = new
        {
            TotalExtensions = totalExtensions,
            UsedExtensions = usedExtensions,
            AvailableExtensions = availableExtensions,
            UsageRate = totalExtensions > 0 ? (double)usedExtensions / totalExtensions * 100 : 0
        };

        return Ok(usageStats);
    }

    /// <summary>
    /// Get comprehensive dashboard data
    /// </summary>
    /// <returns>Dashboard statistics</returns>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        // Get all required data in parallel
        var academicStatsTask = GetAcademicStats();
        var roomOccupancyTask = GetRoomOccupancy();
        var extensionUsageTask = GetExtensionUsage();

        await Task.WhenAll(academicStatsTask, roomOccupancyTask, extensionUsageTask);

        // Extract data from action results
        var academicStats = ExtractDataFromActionResult(academicStatsTask.Result);
        var roomOccupancy = ExtractDataFromActionResult(roomOccupancyTask.Result);
        var extensionUsage = ExtractDataFromActionResult(extensionUsageTask.Result);

        var dashboard = new
        {
            AcademicStats = academicStats,
            RoomOccupancy = roomOccupancy,
            ExtensionUsage = extensionUsage,
            GeneratedAt = DateTime.UtcNow
        };

        return Ok(dashboard);
    }

    private static object? ExtractDataFromActionResult(IActionResult actionResult)
    {
        if (actionResult is OkObjectResult okResult)
        {
            return okResult.Value;
        }
        return null;
    }
}
