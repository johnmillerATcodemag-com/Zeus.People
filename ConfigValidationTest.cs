using System;
using Zeus.People.API.Configuration;

namespace ConfigValidationTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Configuration Validation Test");
            Console.WriteLine("=============================");

            // Test ServiceBus configuration
            try
            {
                var config = new ServiceBusConfiguration
                {
                    ConnectionString = "",
                    TopicName = "domain-events",
                    SubscriptionName = "academic-management",
                    UseManagedIdentity = false
                };

                config.Validate();
                Console.WriteLine("ERROR: ServiceBus validation should have failed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ServiceBus validation failed as expected: {ex.Message}");
            }

            // Test Database configuration  
            try
            {
                var dbConfig = new DatabaseConfiguration
                {
                    WriteConnectionString = "",
                    ReadConnectionString = "",
                    EventStoreConnectionString = "",
                    CommandTimeoutSeconds = 500 // Invalid
                };

                dbConfig.Validate();
                Console.WriteLine("ERROR: Database validation should have failed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database validation failed as expected: {ex.Message}");
            }
        }
    }
}
