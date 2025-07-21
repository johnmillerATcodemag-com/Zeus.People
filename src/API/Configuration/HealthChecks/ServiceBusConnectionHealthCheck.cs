using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Zeus.People.API.Configuration.HealthChecks;

/// <summary>
/// Health check for Service Bus connection
/// </summary>
public class ServiceBusConnectionHealthCheck : IHealthCheck
{
    private readonly IConfigurationService _configurationService;
    private readonly ILogger<ServiceBusConnectionHealthCheck> _logger;

    public ServiceBusConnectionHealthCheck(
        IConfigurationService configurationService,
        ILogger<ServiceBusConnectionHealthCheck> logger)
    {
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Checking Service Bus connection health");

            var serviceBusConfig = await _configurationService.GetConfigurationAsync<ServiceBusConfiguration>(
                ServiceBusConfiguration.SectionName, cancellationToken);

            var healthData = new Dictionary<string, object>
            {
                ["Namespace"] = serviceBusConfig.Namespace,
                ["TopicName"] = serviceBusConfig.TopicName,
                ["SubscriptionName"] = serviceBusConfig.SubscriptionName,
                ["UseManagedIdentity"] = serviceBusConfig.UseManagedIdentity
            };

            // Validate configuration first
            try
            {
                serviceBusConfig.Validate();
                healthData["ConfigurationValid"] = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Service Bus configuration validation failed");
                healthData["ConfigurationValid"] = false;
                return HealthCheckResult.Unhealthy("Service Bus configuration is invalid", ex, healthData);
            }

            // Test Service Bus connection
            ServiceBusClient? serviceBusClient = null;
            try
            {
                if (serviceBusConfig.UseManagedIdentity && !string.IsNullOrEmpty(serviceBusConfig.Namespace))
                {
                    serviceBusClient = new ServiceBusClient(serviceBusConfig.Namespace);
                }
                else if (!string.IsNullOrEmpty(serviceBusConfig.ConnectionString))
                {
                    serviceBusClient = new ServiceBusClient(serviceBusConfig.ConnectionString);
                }
                else
                {
                    healthData["ConnectionStatus"] = "Configuration Error";
                    return HealthCheckResult.Unhealthy("Service Bus connection string or namespace not configured", data: healthData);
                }

                // Test the connection by trying to create a sender
                await using var sender = serviceBusClient.CreateSender(serviceBusConfig.TopicName);

                // We won't send a message, just verify we can create the sender without exceptions
                healthData["ConnectionStatus"] = "Connected";
                healthData["TopicAccessible"] = true;

                _logger.LogDebug("Service Bus connection health check passed");
                return HealthCheckResult.Healthy("Service Bus connection is working", healthData);
            }
            catch (ServiceBusException sbEx) when (sbEx.Reason == ServiceBusFailureReason.MessagingEntityNotFound)
            {
                _logger.LogWarning("Service Bus topic {TopicName} not found", serviceBusConfig.TopicName);
                healthData["ConnectionStatus"] = "Connected";
                healthData["TopicAccessible"] = false;
                healthData["Error"] = $"Topic '{serviceBusConfig.TopicName}' not found";

                return HealthCheckResult.Degraded("Service Bus connected but topic not found", data: healthData);
            }
            catch (ServiceBusException sbEx) when (sbEx.Reason == ServiceBusFailureReason.ServiceCommunicationProblem)
            {
                _logger.LogError("Service Bus authorization failed");
                healthData["ConnectionStatus"] = "Unauthorized";
                healthData["Error"] = "Authorization failed";

                return HealthCheckResult.Unhealthy("Service Bus authorization failed", sbEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Service Bus connection failed");
                healthData["ConnectionStatus"] = "Failed";
                healthData["Error"] = ex.Message;

                return HealthCheckResult.Unhealthy("Service Bus connection failed", ex, healthData);
            }
            finally
            {
                if (serviceBusClient != null)
                {
                    await serviceBusClient.DisposeAsync();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Service Bus health check failed with unexpected error");
            return HealthCheckResult.Unhealthy("Service Bus health check failed", ex, new Dictionary<string, object>
            {
                ["ConfigurationValid"] = false,
                ["Error"] = ex.Message
            });
        }
    }
}
