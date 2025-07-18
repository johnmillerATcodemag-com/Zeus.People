using Zeus.People.API.Configuration;
using Zeus.People.Application;
using Zeus.People.Infrastructure.Configuration;
using Serilog;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

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

// Add JWT Authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

// Add Swagger with authentication
builder.Services.AddSwaggerConfiguration();

// Add application layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Zeus.People API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

// Add content-type validation middleware before exception handling
app.UseMiddleware<Zeus.People.API.Middleware.ContentTypeValidationMiddleware>();

// Add global exception handling middleware - comment out until middleware is fixed
// app.UseExceptionHandling();

app.UseAuthentication();
app.UseAuthorization();

// Add health checks endpoint with JSON response
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";

        var result = new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration.ToString(),
            results = report.Entries.ToDictionary(
                kvp => kvp.Key,
                kvp => new
                {
                    status = kvp.Value.Status.ToString(),
                    duration = kvp.Value.Duration.ToString(),
                    description = kvp.Value.Description,
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

try
{
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
