using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Zeus.People.Infrastructure.Messaging;

namespace Zeus.People.Infrastructure.HealthChecks;

/// <summary>
/// Health check for Azure Service Bus connectivity
/// </summary>
public class ServiceBusHealthCheck : IHealthCheck
{
    private readonly ServiceBusClient _serviceBusClient;
    private readonly ServiceBusConfiguration _configuration;

    public ServiceBusHealthCheck(
        ServiceBusClient serviceBusClient,
        IOptions<ServiceBusConfiguration> configuration)
    {
        _serviceBusClient = serviceBusClient ?? throw new ArgumentNullException(nameof(serviceBusClient));
        _configuration = configuration.Value ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to create a sender to check Service Bus connectivity
            await using var sender = _serviceBusClient.CreateSender(_configuration.TopicName);

            // If we can create the sender without exception, the connection is healthy
            return HealthCheckResult.Healthy("Service Bus connection is healthy");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Service Bus connection failed",
                ex);
        }
    }
}
