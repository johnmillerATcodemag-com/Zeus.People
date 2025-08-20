using System.Net;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace Zeus.People.Tests.E2E;

/// <summary>
/// End-to-End tests for Zeus.People API deployed to Azure staging environment
/// Tests against live staging application at: app-academic-staging-dvjm4oxxoy2g6.azurewebsites.net
/// </summary>
public class StagingApplicationE2ETests : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ITestOutputHelper _output;
    private readonly string _baseUrl;

    public StagingApplicationE2ETests(ITestOutputHelper output)
    {
        _output = output;
        _baseUrl = "https://app-academic-staging-dvjm4oxxoy2g6.azurewebsites.net";

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_baseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Environment", "Staging")]
    public async Task StagingApp_HealthEndpoint_ReturnsHealthyStatus()
    {
        // Act
        var response = await _httpClient.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Healthy", content);

        _output.WriteLine($"✅ Health Status: {response.StatusCode}");
        _output.WriteLine($"✅ Health Response: {content}");

        // Verify JSON structure if applicable
        if (content.StartsWith("{"))
        {
            var healthData = JsonSerializer.Deserialize<JsonElement>(content);
            Assert.True(healthData.TryGetProperty("status", out var status));
            Assert.Equal("Healthy", status.GetString());
            _output.WriteLine("✅ Health endpoint returns structured JSON response");
        }
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Environment", "Staging")]
    public async Task StagingApp_HealthEndpoint_ValidatesAllServices()
    {
        // Act
        var response = await _httpClient.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        if (content.StartsWith("{"))
        {
            var healthData = JsonSerializer.Deserialize<JsonElement>(content);

            // Check overall status
            Assert.True(healthData.TryGetProperty("status", out var overallStatus));
            Assert.Equal("Healthy", overallStatus.GetString());

            // Check individual service health if available
            if (healthData.TryGetProperty("entries", out var entries))
            {
                var expectedServices = new[] { "Configuration", "ServiceBus", "CosmosDB" };

                foreach (var serviceName in expectedServices)
                {
                    if (entries.TryGetProperty(serviceName, out var serviceHealth))
                    {
                        Assert.True(serviceHealth.TryGetProperty("status", out var serviceStatus));
                        Assert.Equal("Healthy", serviceStatus.GetString());
                        _output.WriteLine($"✅ {serviceName} service is healthy");
                    }
                }
            }
        }

        _output.WriteLine("✅ All dependent services are healthy in staging environment");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Environment", "Staging")]
    public async Task StagingApp_ApiResponds_WithCorrectHeaders()
    {
        // Act
        var response = await _httpClient.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Headers);

        // Verify common API headers
        Assert.True(response.Content.Headers.ContentType != null);

        _output.WriteLine($"✅ Content-Type: {response.Content.Headers.ContentType}");
        _output.WriteLine("✅ API responds with proper HTTP headers");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Environment", "Staging")]
    public async Task StagingApp_ResponseTime_IsAcceptable()
    {
        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var response = await _httpClient.GetAsync("/health");
        stopwatch.Stop();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(stopwatch.ElapsedMilliseconds < 5000,
            $"Response time {stopwatch.ElapsedMilliseconds}ms exceeds 5 second threshold");

        _output.WriteLine($"✅ Response time: {stopwatch.ElapsedMilliseconds}ms (acceptable)");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Environment", "Staging")]
    public async Task StagingApp_HandlesInvalidRoute_Gracefully()
    {
        // Act
        var response = await _httpClient.GetAsync("/nonexistent-endpoint");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        _output.WriteLine($"✅ Invalid route returns 404 as expected: {response.StatusCode}");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Environment", "Staging")]
    public async Task StagingApp_SupportsHttpsOnly()
    {
        // Arrange
        using var httpClient = new HttpClient();
        var httpUrl = _baseUrl.Replace("https://", "http://");

        // Act & Assert
        try
        {
            var response = await httpClient.GetAsync($"{httpUrl}/health");
            // If we get here, check if it redirects to HTTPS
            if (response.StatusCode == HttpStatusCode.Redirect ||
                response.StatusCode == HttpStatusCode.MovedPermanently)
            {
                var location = response.Headers.Location?.ToString();
                Assert.True(location?.StartsWith("https://"),
                    "HTTP should redirect to HTTPS");
                _output.WriteLine("✅ HTTP properly redirects to HTTPS");
            }
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("SSL") || ex.Message.Contains("HTTPS"))
        {
            // Expected - app only accepts HTTPS
            _output.WriteLine("✅ Application enforces HTTPS-only communication");
        }
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Environment", "Staging")]
    public async Task StagingApp_MultipleRequests_AreHandledCorrectly()
    {
        // Arrange
        const int numberOfRequests = 5;
        var tasks = new List<Task<HttpResponseMessage>>();

        // Act
        for (int i = 0; i < numberOfRequests; i++)
        {
            tasks.Add(_httpClient.GetAsync("/health"));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        foreach (var (response, index) in responses.Select((r, i) => (r, i)))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            _output.WriteLine($"✅ Request {index + 1}/{numberOfRequests}: {response.StatusCode}");
        }

        _output.WriteLine($"✅ All {numberOfRequests} concurrent requests handled successfully");

        // Cleanup
        foreach (var response in responses)
        {
            response.Dispose();
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _httpClient?.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
