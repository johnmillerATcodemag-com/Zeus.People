using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zeus.People.Infrastructure.Persistence;
using Zeus.People.Infrastructure.EventStore;

namespace Zeus.People.API.Controllers;

/// <summary>
/// Controller for database migration and setup operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MigrationController : ControllerBase
{
    private readonly AcademicContext _academicContext;
    private readonly EventStoreContext _eventStoreContext;
    private readonly ILogger<MigrationController> _logger;

    public MigrationController(
        AcademicContext academicContext,
        EventStoreContext eventStoreContext,
        ILogger<MigrationController> logger)
    {
        _academicContext = academicContext;
        _eventStoreContext = eventStoreContext;
        _logger = logger;
    }

    /// <summary>
    /// Runs pending database migrations
    /// </summary>
    /// <returns>Migration result</returns>
    [HttpPost("run")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RunMigrations()
    {
        try
        {
            _logger.LogInformation("Starting database migrations...");

            // Run Academic Context migrations
            _logger.LogInformation("Running Academic Context migrations...");
            var academicPendingMigrations = await _academicContext.Database.GetPendingMigrationsAsync();

            if (academicPendingMigrations.Any())
            {
                _logger.LogInformation("Found {Count} pending migrations for Academic Context", academicPendingMigrations.Count());
                await _academicContext.Database.MigrateAsync();
                _logger.LogInformation("Academic Context migrations completed successfully");
            }
            else
            {
                _logger.LogInformation("No pending migrations for Academic Context");
            }

            // Run Event Store Context migrations
            _logger.LogInformation("Running Event Store Context migrations...");
            var eventStorePendingMigrations = await _eventStoreContext.Database.GetPendingMigrationsAsync();

            if (eventStorePendingMigrations.Any())
            {
                _logger.LogInformation("Found {Count} pending migrations for Event Store Context", eventStorePendingMigrations.Count());
                await _eventStoreContext.Database.MigrateAsync();
                _logger.LogInformation("Event Store Context migrations completed successfully");
            }
            else
            {
                _logger.LogInformation("No pending migrations for Event Store Context");
            }

            _logger.LogInformation("All database migrations completed successfully");

            var result = new
            {
                success = true,
                message = "Database migrations completed successfully",
                academicMigrations = academicPendingMigrations.ToList(),
                eventStoreMigrations = eventStorePendingMigrations.ToList(),
                timestamp = DateTime.UtcNow
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running database migrations");

            return StatusCode(500, new
            {
                success = false,
                message = "Error running database migrations",
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Gets migration status information
    /// </summary>
    /// <returns>Migration status</returns>
    [HttpGet("status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetMigrationStatus()
    {
        try
        {
            _logger.LogInformation("Checking migration status...");

            // Check Academic Context
            var academicAppliedMigrations = await _academicContext.Database.GetAppliedMigrationsAsync();
            var academicPendingMigrations = await _academicContext.Database.GetPendingMigrationsAsync();

            // Check Event Store Context
            var eventStoreAppliedMigrations = await _eventStoreContext.Database.GetAppliedMigrationsAsync();
            var eventStorePendingMigrations = await _eventStoreContext.Database.GetPendingMigrationsAsync();

            // Test database connectivity
            var academicCanConnect = await _academicContext.Database.CanConnectAsync();
            var eventStoreCanConnect = await _eventStoreContext.Database.CanConnectAsync();

            var result = new
            {
                success = true,
                timestamp = DateTime.UtcNow,
                connectivity = new
                {
                    academicDatabase = academicCanConnect,
                    eventStoreDatabase = eventStoreCanConnect
                },
                academicContext = new
                {
                    appliedMigrations = academicAppliedMigrations.ToList(),
                    pendingMigrations = academicPendingMigrations.ToList(),
                    migrationCount = academicAppliedMigrations.Count(),
                    pendingCount = academicPendingMigrations.Count()
                },
                eventStoreContext = new
                {
                    appliedMigrations = eventStoreAppliedMigrations.ToList(),
                    pendingMigrations = eventStorePendingMigrations.ToList(),
                    migrationCount = eventStoreAppliedMigrations.Count(),
                    pendingCount = eventStorePendingMigrations.Count()
                }
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking migration status");

            return StatusCode(500, new
            {
                success = false,
                message = "Error checking migration status",
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Creates the database if it doesn't exist
    /// </summary>
    /// <returns>Database creation result</returns>
    [HttpPost("create-database")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateDatabase()
    {
        try
        {
            _logger.LogInformation("Creating databases if they don't exist...");

            // Create Academic Database
            var academicCreated = await _academicContext.Database.EnsureCreatedAsync();
            _logger.LogInformation("Academic database created: {Created}", academicCreated);

            // Create Event Store Database
            var eventStoreCreated = await _eventStoreContext.Database.EnsureCreatedAsync();
            _logger.LogInformation("Event Store database created: {Created}", eventStoreCreated);

            var result = new
            {
                success = true,
                message = "Database creation completed",
                academicDatabaseCreated = academicCreated,
                eventStoreDatabaseCreated = eventStoreCreated,
                timestamp = DateTime.UtcNow
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating databases");

            return StatusCode(500, new
            {
                success = false,
                message = "Error creating databases",
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Tests database connectivity
    /// </summary>
    /// <returns>Connectivity test result</returns>
    [HttpGet("test-connection")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> TestConnection()
    {
        try
        {
            _logger.LogInformation("Testing database connections...");

            var academicConnectionString = _academicContext.Database.GetConnectionString();
            var eventStoreConnectionString = _eventStoreContext.Database.GetConnectionString();

            var academicCanConnect = await _academicContext.Database.CanConnectAsync();
            var eventStoreCanConnect = await _eventStoreContext.Database.CanConnectAsync();

            var result = new
            {
                success = true,
                timestamp = DateTime.UtcNow,
                connections = new
                {
                    academicDatabase = new
                    {
                        canConnect = academicCanConnect,
                        connectionString = MaskConnectionString(academicConnectionString)
                    },
                    eventStoreDatabase = new
                    {
                        canConnect = eventStoreCanConnect,
                        connectionString = MaskConnectionString(eventStoreConnectionString)
                    }
                }
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing database connections");

            return StatusCode(500, new
            {
                success = false,
                message = "Error testing database connections",
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }

    private static string? MaskConnectionString(string? connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            return null;

        // Mask sensitive parts of the connection string
        var masked = connectionString;

        // Mask password
        if (masked.Contains("Password=", StringComparison.OrdinalIgnoreCase))
        {
            var passwordStart = masked.IndexOf("Password=", StringComparison.OrdinalIgnoreCase);
            var passwordEnd = masked.IndexOf(';', passwordStart);
            if (passwordEnd == -1) passwordEnd = masked.Length;

            var before = masked.Substring(0, passwordStart);
            var after = masked.Substring(passwordEnd);
            masked = before + "Password=***" + after;
        }

        return masked;
    }

    /// <summary>
    /// Seeds basic test data for development purposes
    /// </summary>
    /// <returns>Seeding result</returns>
    [HttpPost("seed-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SeedTestData()
    {
        try
        {
            _logger.LogInformation("Starting test data seeding...");

            // Check if data already exists
            var academicsCount = await _academicContext.Academics.CountAsync();
            var departmentsCount = await _academicContext.Departments.CountAsync();

            if (academicsCount > 0 || departmentsCount > 0)
            {
                return Ok(new
                {
                    success = true,
                    message = "Test data already exists",
                    existingAcademics = academicsCount,
                    existingDepartments = departmentsCount,
                    timestamp = DateTime.UtcNow
                });
            }

            // Create test departments using the factory method
            var departments = new[]
            {
                Zeus.People.Domain.Entities.Department.Create("Computer Science"),
                Zeus.People.Domain.Entities.Department.Create("Mathematics"),
                Zeus.People.Domain.Entities.Department.Create("Engineering")
            };

            await _academicContext.Departments.AddRangeAsync(departments);

            // Create test academics using the factory method
            var academics = new[]
            {
                Zeus.People.Domain.Entities.Academic.Create(
                    Zeus.People.Domain.ValueObjects.EmpNr.Create("AB1234"),
                    Zeus.People.Domain.ValueObjects.EmpName.Create("Dr. John Smith"),
                    Zeus.People.Domain.ValueObjects.Rank.Create("P")
                ),
                Zeus.People.Domain.Entities.Academic.Create(
                    Zeus.People.Domain.ValueObjects.EmpNr.Create("CD5678"),
                    Zeus.People.Domain.ValueObjects.EmpName.Create("Dr. Jane Doe"),
                    Zeus.People.Domain.ValueObjects.Rank.Create("SL")
                ),
                Zeus.People.Domain.Entities.Academic.Create(
                    Zeus.People.Domain.ValueObjects.EmpNr.Create("EF9012"),
                    Zeus.People.Domain.ValueObjects.EmpName.Create("Dr. Bob Johnson"),
                    Zeus.People.Domain.ValueObjects.Rank.Create("L")
                )
            };

            await _academicContext.Academics.AddRangeAsync(academics);

            // Save changes
            await _academicContext.SaveChangesAsync();

            _logger.LogInformation("Test data seeding completed successfully");

            return Ok(new
            {
                success = true,
                message = "Test data seeded successfully",
                departmentsCreated = departments.Length,
                academicsCreated = academics.Length,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed test data");
            return StatusCode(500, new
            {
                success = false,
                message = "Failed to seed test data",
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }
}
