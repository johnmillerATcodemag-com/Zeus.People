# Staging Deployment Testing Guide

# Zeus.People Academic Management System

# Duration: Complete guide for staging deployment testing

## Overview

This guide provides comprehensive instructions for testing deployments to the staging environment of the Zeus.People Academic Management System. The staging environment serves as a production-like environment for final validation before production deployment.

## Prerequisites

### Required Tools

- **Azure CLI** (latest version)
- **Azure Developer CLI (AZD)** (latest version)
- **.NET 8.0 SDK** or later
- **PowerShell** 7.0 or later
- **Git** for version control

### Azure Requirements

- Valid Azure subscription with appropriate permissions
- Service Principal with necessary role assignments:
  - Contributor access to the subscription or resource group
  - User Access Administrator (for role assignments)
  - Key Vault access for secret management

### Environment Variables

Set the following environment variables or Azure secrets:

```bash
AZURE_CLIENT_ID=<your-service-principal-client-id>
AZURE_CLIENT_SECRET=<your-service-principal-client-secret>
AZURE_TENANT_ID=<your-azure-tenant-id>
AZURE_SUBSCRIPTION_ID=<your-azure-subscription-id>
```

## Testing Methods

### Method 1: Automated GitHub Actions Testing

The most comprehensive testing approach uses the automated GitHub Actions workflow.

#### Triggering the Test

1. Navigate to the GitHub repository
2. Go to **Actions** tab
3. Select **Test Staging Deployment** workflow
4. Click **Run workflow**
5. Configure parameters:
   - **Environment**: `staging` (default)
   - **Skip Infrastructure**: `false` (deploys full infrastructure)
   - **Skip Tests**: `false` (runs all validation tests)
   - **Cleanup After Test**: `false` (keeps resources for inspection)

#### Test Coverage

The automated workflow performs:

- ✅ Infrastructure provisioning with Bicep templates
- ✅ Application build and deployment
- ✅ Health endpoint validation
- ✅ API endpoint functionality testing
- ✅ Authentication and authorization validation
- ✅ Basic performance testing
- ✅ Database connectivity verification
- ✅ Comprehensive reporting and artifact generation

### Method 2: Local PowerShell Testing

For rapid validation and debugging, use the local PowerShell scripts.

#### Full Staging Deployment Test

```powershell
# Run comprehensive staging deployment test
.\test-staging-deployment.ps1 -EnvironmentName "staging" -Location "eastus2"

# Skip infrastructure if already deployed
.\test-staging-deployment.ps1 -EnvironmentName "staging" -SkipInfrastructure

# Run with cleanup after testing
.\test-staging-deployment.ps1 -EnvironmentName "staging" -CleanupAfterTest
```

#### Quick Staging Readiness Test

```powershell
# Test existing staging deployment
.\quick-staging-test.ps1 -BaseUrl "https://your-staging-app.azurewebsites.net"

# Skip health check if endpoint unavailable
.\quick-staging-test.ps1 -BaseUrl "https://your-staging-app.azurewebsites.net" -SkipHealthCheck
```

### Method 3: Manual Azure CLI Testing

For step-by-step control and debugging:

```bash
# 1. Login to Azure
az login

# 2. Set subscription
az account set --subscription "your-subscription-id"

# 3. Initialize AZD environment
azd auth login
azd env set AZURE_ENV_NAME "zeus-people-staging"
azd env set AZURE_LOCATION "eastus2"
azd env set AZURE_SUBSCRIPTION_ID "your-subscription-id"

# 4. Provision infrastructure
azd provision

# 5. Deploy application
azd deploy zeus-people-api

# 6. Test health endpoint
curl -f "$(azd env get-values | grep AZURE_APP_SERVICE_URL | cut -d'=' -f2)/health"
```

## Test Scenarios

### Infrastructure Testing

- **Resource Group Creation**: Validates proper resource group provisioning
- **App Service Deployment**: Tests Azure App Service configuration
- **SQL Database Setup**: Verifies database server and database creation
- **Key Vault Configuration**: Tests secret storage and access
- **Application Insights**: Validates monitoring setup
- **Networking**: Tests security groups and connectivity

### Application Testing

- **Build Process**: Validates .NET application compilation
- **Deployment Package**: Tests application packaging and deployment
- **Configuration**: Verifies app settings and connection strings
- **Dependencies**: Tests NuGet package resolution and loading
- **Startup**: Validates application initialization

### Functional Testing

- **Health Endpoints**: `/health` endpoint availability and response
- **API Endpoints**: Core CRUD operations for:
  - Academics (`/api/academics`)
  - Departments (`/api/departments`)
  - Rooms (`/api/rooms`)
  - Extensions (`/api/extensions`)
- **Authentication**: JWT token validation and authorization
- **Database Operations**: Data persistence and retrieval
- **Error Handling**: Proper error responses and logging

### Performance Testing

- **Response Times**: API endpoint response time validation
- **Concurrent Requests**: Basic load testing scenarios
- **Resource Utilization**: Memory and CPU usage monitoring
- **Database Performance**: Query execution time validation

## Expected Results

### Success Criteria

- ✅ All infrastructure resources deployed successfully
- ✅ Application accessible via HTTPS
- ✅ Health endpoint returns `Healthy` status
- ✅ API endpoints return expected responses
- ✅ Authentication properly blocks unauthorized access
- ✅ Database connectivity established
- ✅ Performance within acceptable thresholds

### Failure Scenarios

Common failure scenarios and troubleshooting:

#### Infrastructure Failures

- **Permission Issues**: Verify service principal permissions
- **Resource Conflicts**: Check for naming conflicts or quota limits
- **Region Availability**: Ensure selected region supports required services

#### Application Deployment Failures

- **Build Errors**: Check .NET SDK version and dependencies
- **Configuration Issues**: Verify app settings and connection strings
- **Startup Failures**: Review application logs in Azure portal

#### Runtime Issues

- **503 Service Unavailable**: Application may be starting up (wait 2-3 minutes)
- **500 Internal Server Error**: Check application logs and database connectivity
- **Authentication Failures**: Verify JWT configuration and key vault access

## Artifacts and Reporting

### Generated Reports

The testing process generates several artifacts:

1. **Deployment Report** (`staging-deployment-report-*.json`)

   - Overall test results
   - Timing information
   - Resource details
   - Success/failure status

2. **API Test Results** (`staging-api-test-results-*.json`)

   - Individual endpoint test results
   - Response times
   - Status codes
   - Error details

3. **Performance Results** (`staging-performance-results-*.json`)

   - Response time metrics
   - Throughput measurements
   - Resource utilization data

4. **Test Logs** (`staging-deployment-test-*.log`)
   - Detailed execution logs
   - Error messages
   - Debugging information

### Monitoring and Alerting

#### Azure Portal Monitoring

- Navigate to the deployed App Service
- Check **Metrics** for performance data
- Review **Logs** for application errors
- Monitor **Alerts** for any triggered conditions

#### Application Insights

- View real-time telemetry data
- Analyze request/response patterns
- Monitor dependencies and failures
- Set up custom alerts for critical issues

## Cleanup and Maintenance

### Resource Cleanup

```powershell
# Clean up staging resources
azd down --force --purge

# Or use the test script with cleanup flag
.\test-staging-deployment.ps1 -EnvironmentName "staging" -CleanupAfterTest
```

### Scheduled Testing

The GitHub Actions workflow is configured for:

- **Daily Automated Tests**: 2 AM UTC daily staging validation
- **On-Demand Testing**: Manual workflow dispatch
- **Pull Request Validation**: Automated testing on code changes

## Best Practices

### Testing Strategy

1. **Run Daily**: Schedule daily staging tests to catch environment drift
2. **Test Before Production**: Always validate staging before production deployment
3. **Monitor Trends**: Track performance metrics over time
4. **Document Issues**: Create GitHub issues for persistent failures
5. **Version Testing**: Test with specific application versions

### Security Considerations

1. **Secret Management**: Use Azure Key Vault for sensitive configuration
2. **Network Security**: Implement proper network security groups
3. **Access Control**: Use role-based access control (RBAC)
4. **Audit Logging**: Enable comprehensive audit logging
5. **SSL/TLS**: Ensure HTTPS enforcement for all endpoints

### Cost Management

1. **Resource Sizing**: Use appropriate SKUs for staging (smaller than production)
2. **Auto-Shutdown**: Implement dev/test pricing and auto-shutdown policies
3. **Cleanup Automation**: Regular cleanup of unused resources
4. **Monitoring Costs**: Set up cost alerts and budgets

## Troubleshooting

### Common Issues

#### Authentication Problems

```bash
# Re-authenticate with Azure
az login --tenant "your-tenant-id"
azd auth login --tenant-id "your-tenant-id"
```

#### Permission Errors

```bash
# Check current account and permissions
az account show
az role assignment list --assignee "your-user-or-service-principal"
```

#### Resource Deployment Failures

```bash
# Check deployment status
az deployment group list --resource-group "your-resource-group"
az deployment group show --resource-group "your-resource-group" --name "deployment-name"
```

#### Application Issues

```bash
# View application logs
az webapp log tail --name "your-app-name" --resource-group "your-resource-group"

# Check application settings
az webapp config appsettings list --name "your-app-name" --resource-group "your-resource-group"
```

### Support Resources

- **Azure Documentation**: https://docs.microsoft.com/azure/
- **Azure Developer CLI**: https://learn.microsoft.com/azure/developer/azure-developer-cli/
- **GitHub Issues**: Create issues in the repository for team support
- **Azure Support**: For Azure-specific infrastructure issues

## Integration with CI/CD

The staging deployment testing integrates with the main CI/CD pipeline:

1. **Code Changes**: Trigger automated builds and tests
2. **Staging Deployment**: Automatic deployment to staging environment
3. **Validation Testing**: Comprehensive staging validation tests
4. **Production Gate**: Staging success required for production deployment
5. **Rollback Support**: Automated rollback procedures if needed

This comprehensive testing approach ensures reliable, secure, and performant deployments to the production environment.
