# Azure Bicep Templates - Fix Summary

## Issues Fixed

### ✅ **Circular Dependency Resolution**

**Problem**: The main.bicep template had circular dependencies where the SQL Database, Cosmos DB, and Service Bus modules were trying to reference `appService.outputs.managedIdentityPrincipalId`, but the App Service module depended on these services being deployed first.

**Solution**:

- Created a separate `managedIdentity.bicep` module that deploys first
- Updated all module references to use `managedIdentity.outputs.principalId` instead of app service outputs
- Restructured deployment order to eliminate circular dependencies

### ✅ **Module Parameter Interface Alignment**

**Problem**: The main.bicep template was calling modules with incorrect parameter names and structures.

**Solution**:

- Fixed App Service module parameters to match the actual interface:
  - Added required `appServicePlanName`, `envConfig`, `managedIdentityName`, `applicationInsightsConnectionString`
  - Removed invalid parameters like `appServicePlanId`, `appInsightsInstrumentationKey`, etc.
- Fixed Key Vault Access module to use correct parameters (`keyVaultName`, `managedIdentityName`)
- Fixed Key Vault Secrets module to use the structured interface instead of the array-based approach

### ✅ **Output Reference Corrections**

**Problem**: Incorrect output property names were being referenced.

**Solution**:

- Fixed Cosmos DB output reference from `connectionString` to `primaryMasterKey`
- Fixed Service Bus output reference from `connectionString` to `primaryConnectionString`
- Updated SQL Database outputs to use `academicDatabaseConnectionString` and `eventStoreDatabaseConnectionString`

### ✅ **Deployment Dependencies**

**Problem**: Incorrect and unnecessary dependency declarations.

**Solution**:

- Removed circular dependencies between modules
- Added proper dependency on `managedIdentity` module for services that need it
- Streamlined Key Vault Access dependencies to only depend on `managedIdentity`

## Infrastructure Deployment Order

The fixed deployment order is now:

1. **Resource Group** - Container for all resources
2. **Log Analytics Workspace** - Centralized logging
3. **Application Insights** - Application monitoring
4. **Key Vault** - Secrets management
5. **Managed Identity** - Security identity for resource access
6. **SQL Database** - Write operations and event store
7. **Cosmos DB** - Read model operations
8. **Service Bus** - Domain event messaging
9. **App Service Plan** - Hosting infrastructure
10. **App Service** - Web application
11. **Key Vault Access** - RBAC assignments
12. **Key Vault Secrets** - Connection strings storage

## Validation Status

- ✅ All 11 Bicep modules compile without errors
- ✅ Main template compiles without errors
- ✅ All circular dependencies resolved
- ✅ Proper parameter interfaces implemented
- ✅ Correct output references throughout
- ✅ Secure managed identity integration

## Ready for Deployment

The Azure Bicep templates are now ready for deployment with:

- Proper security practices (managed identity, RBAC, secure parameters)
- Environment-specific configurations (dev/staging/prod)
- Comprehensive monitoring and logging
- Modular architecture for maintainability
- AZD-compatible outputs for integration

**Duration**: Circular dependency resolution and module interface alignment completed.
