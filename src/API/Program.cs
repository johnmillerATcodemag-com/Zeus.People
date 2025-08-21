using Zeus.People.API.Configuration;
using Zeus.People.Application;
using Zeus.People.Infrastructure.Configuration;
using Zeus.People.API.Middleware;
using Serilog;
using Serilog.Events;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog early with Application Insights integration
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithMachineName()
    .Enrich.WithProcessId()
    .Enrich.WithThreadId()
    .WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/zeus-people-api-.log", rollingInterval: RollingInterval.Day, 
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();

builder.Host.UseSerilog();

try
{
    Log.Information("Starting Zeus.People API configuration");

    // Configure comprehensive configuration management with Azure Key Vault
    builder.ConfigureAppConfiguration();

    // Add services to the container
    builder.Services.AddControllers(options =>
    {
        // Add route parameter validation filter
        options.Filters.Add(typeof(Zeus.People.API.Filters.RouteParameterValidationFilter));
    });
    builder.Services.AddEndpointsApiExplorer();

    // Add CORS
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });

    // Add Swagger with authentication
    Zeus.People.API.Configuration.ConfigurationExtensions.AddSwaggerConfiguration(builder.Services);

    // Add application layers
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // Add comprehensive monitoring and observability
    builder.Services.AddComprehensiveMonitoring(builder.Configuration, builder.Environment);

    // Configure JWT authentication with Key Vault support (before building the app)
    var serviceProvider = builder.Services.BuildServiceProvider();
    await Zeus.People.API.Configuration.ConfigurationExtensions.AddJwtAuthenticationAsync(builder.Services, serviceProvider);

    var app = builder.Build();

    // Validate configuration before starting the application
    await app.ValidateConfigurationAsync();

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Zeus.People API v1");
            c.RoutePrefix = "swagger";
        });

        // Add configuration debug endpoint in development
        app.AddConfigurationEndpoint();
    }

    app.UseHttpsRedirection();
    app.UseCors("AllowAll");

    // Add performance monitoring middleware
    app.UseMiddleware<PerformanceMonitoringMiddleware>();
    app.UseMiddleware<BusinessMetricsMiddleware>();

    // Add content-type validation middleware before exception handling
    app.UseMiddleware<Zeus.People.API.Middleware.ContentTypeValidationMiddleware>();

    // Add global exception handling middleware - comment out until middleware is fixed
    // app.UseExceptionHandling();

    app.UseAuthentication();
    app.UseAuthorization();

    // Add health checks endpoint with detailed JSON response
    app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";

            var result = new
            {
                status = report.Status.ToString(),
                totalDuration = report.TotalDuration.ToString(),
                timestamp = DateTime.UtcNow,
                results = report.Entries.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new
                    {
                        status = kvp.Value.Status.ToString(),
                        duration = kvp.Value.Duration.ToString(),
                        description = kvp.Value.Description ?? (kvp.Value.Exception?.Message ?? "No description"),
                        data = kvp.Value.Data,
                        tags = kvp.Value.Tags
                    }
                )
            };

            await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            }));
        }
    });

    app.MapControllers();

    Log.Information("Starting Zeus.People API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Zeus.People API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Make Program class public for testing
public partial class Program { }
