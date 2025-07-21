# Zeus.People Azure Infrastructure - Bicep Templates Summary

## Overview

This document summarizes the comprehensive Azure Bicep templates created for the Zeus.People Academic Management System infrastructure provisioning. All templates follow Azure best practices, implement proper security measures, and provide environment-specific configurations.

## Infrastructure Architecture

### Target Scope

- **Deployment Scope**: Subscription-level deployment
- **Resource Organization**: Single resource group per environment
- **Naming Convention**: `{service}-{applicationPrefix}-{environment}-{resourceToken}`

### Environment Support

- **Development (dev)**: Lower SKUs, minimal redundancy
- **Staging (staging)**: Production-like configuration with standard SKUs
- **Production (prod)**: High availability, premium SKUs, multi-region support

## Infrastructure Components

### 1. Main Template (`infra/main.bicep`)

- **Purpose**: Orchestration template for all infrastructure components
- **Features**:
  - Subscription-level targeting
  - Environment-specific configuration objects
  - Proper dependency management
  - Comprehensive parameter validation
  - Output values for azd compatibility

### 2. Infrastructure Modules

#### Core Observability

- **Log Analytics Workspace** (`modules/logAnalytics.bicep`)

  - Centralized logging and monitoring
  - Environment-specific retention policies
  - PerGB2018 pricing tier with daily quotas

- **Application Insights** (`modules/appInsights.bicep`)
  - Application performance monitoring
  - Integration with Log Analytics workspace
  - Web application type configuration

#### Security & Identity

- **Managed Identity** (`modules/managedIdentity.bicep`)

  - User-assigned managed identity for secure resource access
  - Principal and client ID outputs for RBAC assignments

- **Key Vault** (`modules/keyVault.bicep`)

  - Secrets and configuration management
  - RBAC-based authorization model
  - Soft delete enabled with 90-day retention
  - Diagnostic settings for audit logging

- **Key Vault Access** (`modules/keyVaultAccess.bicep`)

  - RBAC role assignments for managed identity
  - Key Vault Secrets User role (4633458b-17de-408a-b874-0445c86b69e6)

- **Key Vault Secrets** (`modules/keyVaultSecrets.bicep`)
  - Secure storage of connection strings
  - Application configuration secrets
  - Key Vault reference format outputs

#### Data Services

- **SQL Database** (`modules/sqlDatabase.bicep`)

  - Azure SQL Server with AAD authentication
  - Two databases: Zeus.People (write operations) and Zeus.People.EventStore
  - Advanced Threat Protection and vulnerability assessment
  - Diagnostic settings and audit logging
  - Environment-specific SKU configurations

- **Cosmos DB** (`modules/cosmosDb.bicep`)
  - NoSQL database for read model operations (CQRS pattern)
  - Four containers: academics, departments, rooms, extensions
  - Continuous backup and point-in-time restore
  - Composite indexes and unique key policies
  - Multi-region configuration for production

#### Messaging & Communication

- **Service Bus** (`modules/serviceBus.bicep`)
  - Premium/Standard namespace based on environment
  - Domain events topic with subscription
  - Dead letter queue handling
  - RBAC assignments for managed identity access

#### Compute & Hosting

- **App Service Plan** (`modules/appServicePlan.bicep`)

  - Linux-based hosting infrastructure
  - Environment-specific SKU configurations
  - Zone redundancy and elastic scaling options
  - Worker count and scaling parameters

- **App Service** (`modules/appService.bicep`)
  - .NET 8.0 Linux web application
  - Managed identity integration
  - Key Vault reference configuration
  - Health check endpoint
  - HTTPS-only with TLS 1.2 minimum
  - Auto-heal rules and diagnostic settings

## Security Implementation

### Authentication & Authorization

- **Managed Identity**: User-assigned identity for all inter-service authentication
- **RBAC**: Role-based access control for all Azure resources
- **Key Vault Integration**: Secure storage and retrieval of secrets
- **AAD Authentication**: Azure Active Directory integration for SQL Server

### Network Security

- **HTTPS Only**: All web traffic enforced over HTTPS
- **TLS 1.2+**: Minimum TLS version requirement
- **FTPS Disabled**: Secure file transfer only
- **Private Endpoints**: Configurable for production environments

### Data Protection

- **Encryption at Rest**: All data services encrypted with Microsoft-managed keys
- **Encryption in Transit**: TLS encryption for all communications
- **Soft Delete**: Key Vault soft delete with 90-day retention
- **Backup & Recovery**: Automated backups for all data services

## Monitoring & Diagnostics

### Comprehensive Logging

- **Diagnostic Settings**: Enabled on all resources
- **Log Analytics Integration**: Centralized log collection
- **Application Insights**: Application performance monitoring
- **Audit Logs**: Security and access audit trails

### Alerting & Metrics

- **Auto-heal Rules**: Automatic recovery for web applications
- **Health Checks**: Application health monitoring endpoints
- **Performance Metrics**: Comprehensive metrics collection
- **Custom Dashboards**: Log Analytics workspace queries

## Environment Configuration

### Development Environment

- **App Service Plan**: Basic B1 SKU
- **SQL Database**: Basic tier
- **Cosmos DB**: 400 RU/s throughput
- **Service Bus**: Standard namespace
- **High Availability**: Disabled
- **Log Retention**: 30 days

### Staging Environment

- **App Service Plan**: Standard S1 SKU
- **SQL Database**: Standard S0 tier
- **Cosmos DB**: 800 RU/s throughput
- **Service Bus**: Standard namespace
- **High Availability**: Disabled
- **Log Retention**: 60 days

### Production Environment

- **App Service Plan**: Premium P1V3 SKU with zone redundancy
- **SQL Database**: Premium P1 tier
- **Cosmos DB**: 1600 RU/s with multi-region replication
- **Service Bus**: Premium namespace
- **High Availability**: Enabled with secondary region
- **Log Retention**: 90 days

## Parameter Files

### Environment-Specific Parameters

- `main.parameters.dev.json`: Development environment configuration
- `main.parameters.staging.json`: Staging environment configuration
- `main.parameters.prod.json`: Production environment configuration

### Parameter Categories

- **Environment Settings**: Environment name, regions, prefixes
- **Security Credentials**: SQL admin credentials (Key Vault references)
- **Access Control**: Principal IDs for Key Vault access
- **Resource Tokens**: Unique naming for resource disambiguation

## Deployment Outputs

### AZD Compatibility

All outputs follow Azure Developer CLI (azd) naming conventions:

- `AZURE_RESOURCE_GROUP_NAME`: Resource group name
- `AZURE_LOCATION`: Primary deployment region
- `SERVICE_API_NAME`: App Service name
- `SERVICE_API_URI`: Application URL
- `AZURE_KEY_VAULT_NAME`: Key Vault name
- `AZURE_KEY_VAULT_ENDPOINT`: Key Vault endpoint URL

### Resource Information

- Connection strings and endpoints for all services
- Managed identity information for authentication
- Diagnostic and monitoring resource identifiers

## Best Practices Implemented

### Infrastructure as Code

- **Modular Design**: Separate modules for each service type
- **Parameter Validation**: Strict parameter types and validation rules
- **Output Consistency**: Standardized output naming and format
- **Documentation**: Comprehensive parameter and output descriptions

### Security Best Practices

- **Least Privilege**: Minimal required permissions for all identities
- **Secure Defaults**: Security-first configuration for all resources
- **Secret Management**: Centralized secret storage and access
- **Audit Trails**: Comprehensive logging and monitoring

### Operational Excellence

- **Environment Parity**: Consistent configuration across environments
- **Automated Recovery**: Auto-heal and backup configurations
- **Monitoring**: Comprehensive observability and alerting
- **Documentation**: Clear documentation and naming conventions

## Next Steps

### Deployment Instructions

1. Configure Azure subscription and permissions
2. Update parameter files with environment-specific values
3. Set up Key Vault with SQL admin credentials
4. Deploy using Azure CLI or Azure DevOps pipelines
5. Verify resource creation and connectivity

### Post-Deployment Configuration

1. Configure application settings in App Service
2. Set up monitoring alerts and dashboards
3. Configure backup and disaster recovery procedures
4. Implement CI/CD pipelines for application deployment

## File Structure

```
infra/
├── main.bicep                          # Main orchestration template
├── main.parameters.dev.json            # Development parameters
├── main.parameters.staging.json        # Staging parameters
├── main.parameters.prod.json          # Production parameters
└── modules/
    ├── logAnalytics.bicep              # Log Analytics workspace
    ├── appInsights.bicep               # Application Insights
    ├── managedIdentity.bicep           # User-assigned managed identity
    ├── keyVault.bicep                  # Key Vault with RBAC
    ├── keyVaultAccess.bicep            # Key Vault RBAC assignments
    ├── keyVaultSecrets.bicep           # Key Vault secrets management
    ├── sqlDatabase.bicep               # SQL Server and databases
    ├── cosmosDb.bicep                  # Cosmos DB account and containers
    ├── serviceBus.bicep                # Service Bus namespace and topics
    ├── appServicePlan.bicep            # App Service hosting plan
    └── appService.bicep                # Web application service
```

This comprehensive infrastructure template provides a production-ready, secure, and scalable foundation for the Zeus.People Academic Management System.
