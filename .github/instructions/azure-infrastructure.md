# Azure Infrastructure Instructions for Academic Management System

## Overview

Provision and configure Azure resources for a production-ready CQRS Academic Management System with proper security, scalability, and monitoring.

## Required Azure Resources

### Core Infrastructure

- **Resource Group**: `rg-academic-mgmt-{env}`
- **Azure Key Vault**: For secrets and configuration management
- **Azure App Service Plan**: For hosting the API
- **Azure App Service**: For the Web API application
- **Application Insights**: For monitoring and logging

### Data Storage

- **Azure SQL Database**:
  - Write database for commands and event store
  - Elastic pool for cost optimization
  - Backup retention: 35 days
  - Point-in-time restore enabled
- **Azure Cosmos DB**:
  - Read database for query models
  - SQL API with multi-region replication
  - Consistent indexing policy

### Messaging & Integration

- **Azure Service Bus**:
  - Premium tier for VNet integration
  - Topics for domain events
  - Dead letter queues for failed messages
  - Auto-scaling enabled

### Security & Identity

- **Azure AD B2C Tenant**: For authentication
- **Managed Identity**: For secure resource access
- **Azure Private Endpoints**: For database security
- **Azure Firewall**: For network security

### Monitoring & DevOps

- **Azure Monitor**: For comprehensive monitoring
- **Log Analytics Workspace**: For centralized logging
- **Azure DevOps**: For CI/CD pipelines
- **Azure Container Registry**: For container images (if using containers)

## Environment Strategy

### Development Environment

```
Resource Naming: {service}-academic-dev-{region}
Location: East US 2
Pricing Tier: Basic/Standard (cost-optimized)
```

### Staging Environment

```
Resource Naming: {service}-academic-staging-{region}
Location: East US 2
Pricing Tier: Standard (production-like)
```

### Production Environment

```
Resource Naming: {service}-academic-prod-{region}
Location: East US 2 (primary), West US 2 (secondary)
Pricing Tier: Premium/Standard (high availability)
```

## Infrastructure as Code (IaC)

### Use Azure Bicep Templates

- Parameterized templates for different environments
- Modular approach with linked templates
- Resource dependencies properly defined
- Output values for application configuration

### Key Configuration Parameters

```bicep
@description('Environment name (dev, staging, prod)')
param environmentName string

@description('Application name prefix')
param applicationPrefix string = 'academic'

@description('SQL Database administrator login')
@secure()
param sqlAdminLogin string

@description('SQL Database administrator password')
@secure()
param sqlAdminPassword string

@description('Cosmos DB account name')
param cosmosDbAccountName string

@description('Service Bus namespace name')
param serviceBusNamespace string
```

## Security Configuration

### Key Vault Secrets

Store sensitive configuration:

- Database connection strings
- Service Bus connection strings
- Azure AD B2C configuration
- API keys and certificates
- Storage account keys

### Managed Identity Configuration

- System-assigned identity for App Service
- Grant access to Key Vault, SQL Database, Cosmos DB
- Configure Service Bus access
- Application Insights access

### Network Security

- Configure VNet integration for App Service
- Private endpoints for databases
- Network Security Groups (NSGs)
- Azure Firewall rules

## Configuration Management

### App Service Configuration

```json
{
  "ConnectionStrings": {
    "WriteDatabase": "@Microsoft.KeyVault(SecretUri=https://{vault}.vault.azure.net/secrets/write-db-connection/)",
    "ReadDatabase": "@Microsoft.KeyVault(SecretUri=https://{vault}.vault.azure.net/secrets/read-db-connection/)",
    "ServiceBus": "@Microsoft.KeyVault(SecretUri=https://{vault}.vault.azure.net/secrets/servicebus-connection/)"
  },
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "{tenant-id}",
    "ClientId": "{client-id}"
  },
  "ApplicationInsights": {
    "InstrumentationKey": "@Microsoft.KeyVault(SecretUri=https://{vault}.vault.azure.net/secrets/appinsights-key/)"
  }
}
```

### Database Configuration

- SQL Database: Enable Advanced Threat Protection
- Cosmos DB: Configure consistent indexing
- Backup policies and retention
- Performance monitoring alerts

## Monitoring & Alerting

### Application Insights Configuration

- Custom telemetry for business events
- Performance counters
- Dependency tracking
- Failed request alerts

### Key Metrics to Monitor

- API response times
- Database performance
- Service Bus message processing
- Authentication failures
- Business rule violations

### Alert Rules

- High error rates (> 5%)
- Slow response times (> 2 seconds)
- Database connection failures
- Service Bus message backlog
- Resource utilization (> 80%)

## Deployment Strategy

### Blue-Green Deployment

- Use deployment slots in App Service
- Automated testing in staging slot
- Traffic switching with validation
- Rollback capabilities

### Database Migrations

- Automated schema migrations
- Data migration scripts
- Backup before migrations
- Rollback procedures

## Cost Optimization

### Resource Optimization

- Auto-scaling for App Service
- Reserved instances for predictable workloads
- Cosmos DB auto-scale
- SQL Database elastic pools

### Monitoring Costs

- Azure Cost Management alerts
- Resource tagging for cost allocation
- Regular cost reviews
- Unused resource identification

## Disaster Recovery

### Backup Strategy

- SQL Database automated backups
- Cosmos DB continuous backup
- Key Vault backup
- Application configuration backup

### Recovery Procedures

- RTO: 4 hours
- RPO: 1 hour
- Cross-region failover procedures
- Data recovery testing

## Compliance & Governance

### Governance Policies

- Resource naming conventions
- Required tags for all resources
- Approved VM sizes and regions
- Security baselines

### Compliance Requirements

- Data residency requirements
- Encryption at rest and in transit
- Access control and audit logging
- Regular security assessments
