// main.bicep - Zeus.People Academic Management System Infrastructure
// Comprehensive Azure infrastructure provisioning using Bicep templates
// Duration: Initial template creation started

targetScope = 'subscription'

// Parameters
@description('Environment name (dev, staging, prod)')
@allowed(['dev', 'staging', 'prod'])
param environmentName string = 'staging'

@description('Application name prefix')
param applicationPrefix string = 'academic'

@description('Primary Azure region for deployment')
param primaryLocation string = 'westus2'

@description('Secondary Azure region for high availability (prod only)')
param secondaryLocation string = 'westus2'

@description('Resource token for unique resource naming')
param resourceToken string = uniqueString(subscription().subscriptionId, environmentName)

// SQL parameters - DISABLED DUE TO AZURE SQL PASSWORD VALIDATION RESTRICTIONS
/*
@description('SQL Database administrator login')
@secure()
param sqlAdminLogin string

@description('SQL Database administrator password')
@secure()
param sqlAdminPassword string
*/

@description('Current timestamp for tagging')
param timestamp string = utcNow()

@description('Principal ID for Key Vault access (service principal or user)')
param keyVaultAccessPrincipalId string

// Variables for consistent naming and configuration
var resourceGroupName = 'rg-${applicationPrefix}-${environmentName}-${primaryLocation}'
var commonTags = {
  'azd-env-name': environmentName
  environment: environmentName
  application: applicationPrefix
  'created-by': 'bicep'
  'created-on': timestamp
  project: 'Zeus.People'
  purpose: 'Academic Management System'
}

// Environment-specific configuration
var environmentConfig = {
  dev: {
    sqlSkuName: 'Basic'
    sqlSkuTier: 'Basic'
    appServicePlanSku: 'B1'
    cosmosDbThroughput: 400
    serviceBusSku: 'Standard'
    keyVaultSku: 'standard'
    enableHighAvailability: false
    enablePrivateEndpoints: false
    alwaysOn: false
    aspnetcoreEnvironment: 'Development'
    vnetIntegration: false
    publicNetworkAccess: 'Enabled'
    logRetentionDays: 30
    metricRetentionDays: 30
  }
  staging: {
    sqlSkuName: 'S2'
    sqlSkuTier: 'Standard'
    appServicePlanSku: 'S2'
    cosmosDbThroughput: 1000
    serviceBusSku: 'Standard'
    keyVaultSku: 'standard'
    enableHighAvailability: false
    enablePrivateEndpoints: true
    alwaysOn: true
    aspnetcoreEnvironment: 'Staging'
    vnetIntegration: true
    publicNetworkAccess: 'Disabled'
    logRetentionDays: 60
    metricRetentionDays: 60
  }
  prod: {
    sqlSkuName: 'P2'
    sqlSkuTier: 'Premium'
    appServicePlanSku: 'P2v3'
    cosmosDbThroughput: 4000
    serviceBusSku: 'Premium'
    keyVaultSku: 'premium'
    enableHighAvailability: true
    enablePrivateEndpoints: true
    alwaysOn: true
    aspnetcoreEnvironment: 'Production'
    vnetIntegration: true
    publicNetworkAccess: 'Disabled'
    logRetentionDays: 90
    metricRetentionDays: 90
  }
}

var currentConfig = environmentConfig[environmentName]

// Resource Group
resource resourceGroup 'Microsoft.Resources/resourceGroups@2024-11-01' = {
  name: resourceGroupName
  location: primaryLocation
  tags: union(commonTags, {
    'azd-env-name': environmentName
  })
}

// Log Analytics Workspace Module
module logAnalytics './modules/logAnalytics.bicep' = {
  scope: resourceGroup
  name: 'logAnalytics-deployment'
  params: {
    workspaceName: 'law-${applicationPrefix}-${environmentName}-${resourceToken}'
    location: primaryLocation
    tags: commonTags
    retentionInDays: environmentName == 'prod' ? 90 : 30
  }
}

// Application Insights Module
module appInsights 'modules/appInsights.bicep' = {
  scope: resourceGroup
  name: 'appInsights-deployment'
  params: {
    appInsightsName: 'ai-${applicationPrefix}-${environmentName}-${resourceToken}'
    location: primaryLocation
    tags: commonTags
    logAnalyticsWorkspaceId: logAnalytics.outputs.workspaceId
  }
}

// Key Vault Module
module keyVault 'modules/keyVault.bicep' = {
  scope: resourceGroup
  name: 'keyVault-deployment'
  params: {
    keyVaultName: 'kv${take(resourceToken, 16)}'
    location: primaryLocation
    tags: commonTags
    tenantId: subscription().tenantId
    accessPrincipalId: keyVaultAccessPrincipalId
    skuName: currentConfig.keyVaultSku
    enableRbacAuthorization: true
    enableSoftDelete: true
    enablePurgeProtection: true
    logAnalyticsWorkspaceId: logAnalytics.outputs.workspaceId
  }
}

// Managed Identity Module (must be deployed before other services that need it)
module managedIdentity 'modules/managedIdentity.bicep' = {
  scope: resourceGroup
  name: 'managedIdentity-deployment'
  params: {
    managedIdentityName: 'mi-${applicationPrefix}-${environmentName}-${resourceToken}'
    location: primaryLocation
    tags: commonTags
  }
}

// SQL Server and Database Module - DISABLED DUE TO AZURE SQL PASSWORD VALIDATION RESTRICTIONS
/*
module sqlDatabase 'modules/sqlDatabase.bicep' = {
  scope: resourceGroup
  name: 'sqlDatabase-deployment'
  params: {
    sqlServerName: 'sql-${applicationPrefix}-${environmentName}-${resourceToken}'
    location: primaryLocation
    tags: commonTags
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    databaseName: 'Zeus.People'
    eventStoreDatabaseName: 'Zeus.People.EventStore'
    skuName: currentConfig.sqlSkuName
    skuTier: currentConfig.sqlSkuTier
    enableAdvancedThreatProtection: true
    enableAuditingToLogAnalytics: true
    logAnalyticsWorkspaceId: logAnalytics.outputs.workspaceId
    managedIdentityPrincipalId: managedIdentity.outputs.principalId
  }
}
*/

// Cosmos DB Module
module cosmosDb 'modules/cosmosDb.bicep' = {
  scope: resourceGroup
  name: 'cosmosDb-deployment'
  params: {
    cosmosDbAccountName: 'cosmos-${applicationPrefix}-${environmentName}-${resourceToken}'
    location: primaryLocation
    tags: commonTags
    databaseName: 'Zeus.People'
    throughput: currentConfig.cosmosDbThroughput
    enableMultipleWriteLocations: currentConfig.enableHighAvailability
    secondaryLocation: currentConfig.enableHighAvailability ? secondaryLocation : ''
    managedIdentityPrincipalId: managedIdentity.outputs.principalId
    logAnalyticsWorkspaceId: logAnalytics.outputs.workspaceId
  }
}

// Service Bus Module
module serviceBus 'modules/serviceBus.bicep' = {
  scope: resourceGroup
  name: 'serviceBus-deployment'
  params: {
    serviceBusNamespaceName: 'sb-${applicationPrefix}-${environmentName}-${resourceToken}'
    location: primaryLocation
    tags: commonTags
    skuName: currentConfig.serviceBusSku
    topicName: 'domain-events'
    subscriptionName: 'zeus-people-subscription'
    managedIdentityPrincipalId: managedIdentity.outputs.principalId
    logAnalyticsWorkspaceId: logAnalytics.outputs.workspaceId
  }
}

// App Service Plan Module
module appServicePlan 'modules/appServicePlan.bicep' = {
  scope: resourceGroup
  name: 'appServicePlan-deployment'
  params: {
    appServicePlanName: 'asp-${applicationPrefix}-${environmentName}-${resourceToken}'
    location: primaryLocation
    tags: commonTags
    skuName: currentConfig.appServicePlanSku
    kind: 'linux'
    reserved: true
  }
}

// App Service Module
module appService 'modules/appService.bicep' = {
  scope: resourceGroup
  name: 'appService-deployment'
  params: {
    appServiceName: 'app-${applicationPrefix}-${environmentName}-${resourceToken}'
    appServicePlanName: 'asp-${applicationPrefix}-${environmentName}-${resourceToken}'
    location: primaryLocation
    envConfig: currentConfig
    tags: union(commonTags, {
      'azd-service-name': 'api'
    })
    managedIdentityName: 'mi-${applicationPrefix}-${environmentName}-${resourceToken}'
    keyVaultName: keyVault.outputs.keyVaultName
    applicationInsightsConnectionString: appInsights.outputs.connectionString
    logAnalyticsWorkspaceId: logAnalytics.outputs.workspaceId
  }
  dependsOn: [
    appServicePlan
    managedIdentity
  ]
}

// Grant Key Vault access to App Service managed identity
module keyVaultAccess 'modules/keyVaultAccess.bicep' = {
  scope: resourceGroup
  name: 'keyVaultAccess-deployment'
  params: {
    keyVaultName: keyVault.outputs.keyVaultName
    managedIdentityName: 'mi-${applicationPrefix}-${environmentName}-${resourceToken}'
  }
  dependsOn: [
    managedIdentity
  ]
}

// Store connection strings and secrets in Key Vault
module keyVaultSecrets 'modules/keyVaultSecrets.bicep' = {
  scope: resourceGroup
  name: 'keyVaultSecrets-deployment'
  params: {
    keyVaultName: keyVault.outputs.keyVaultName
    sqlConnectionStrings: {
      defaultConnection: 'PLACEHOLDER_SQL_CONNECTION'
      eventStoreConnection: 'PLACEHOLDER_EVENTSTORE_CONNECTION'
    }
    cosmosDbConnectionString: cosmosDb.outputs.primaryMasterKey
    serviceBusConnectionString: serviceBus.outputs.primaryConnectionString
    applicationInsightsConnectionString: appInsights.outputs.connectionString
  }
  dependsOn: [
    keyVaultAccess
  ]
}

// Outputs for Azure Developer CLI (azd)
@description('The name of the resource group')
output AZURE_RESOURCE_GROUP_NAME string = resourceGroup.name

@description('The location of the primary region')
output AZURE_LOCATION string = primaryLocation

@description('The name of the App Service')
output SERVICE_API_NAME string = appService.outputs.appServiceName

@description('The URL of the App Service')
output SERVICE_API_URI string = appService.outputs.appServiceUrl

@description('The name of the Key Vault')
output AZURE_KEY_VAULT_NAME string = keyVault.outputs.keyVaultName

@description('The URL of the Key Vault')
output AZURE_KEY_VAULT_ENDPOINT string = keyVault.outputs.keyVaultUri

@description('The name of the Application Insights instance')
output AZURE_APPLICATION_INSIGHTS_NAME string = appInsights.outputs.appInsightsName

@description('The connection string for Application Insights')
output AZURE_APPLICATION_INSIGHTS_CONNECTION_STRING string = appInsights.outputs.connectionString

// SQL Server outputs - DISABLED DUE TO AZURE SQL PASSWORD VALIDATION RESTRICTIONS
/*
@description('The name of the SQL Server')
output AZURE_SQL_SERVER_NAME string = sqlDatabase.outputs.sqlServerName

@description('The FQDN of the SQL Server')
output AZURE_SQL_SERVER_FQDN string = sqlDatabase.outputs.sqlServerFqdn

@description('The name of the academic database')
output AZURE_SQL_DATABASE_NAME string = sqlDatabase.outputs.academicDatabaseName
*/

@description('The name of the Cosmos DB account')
output AZURE_COSMOS_DB_ACCOUNT_NAME string = cosmosDb.outputs.cosmosDbAccountName

@description('The endpoint of the Cosmos DB account')
output AZURE_COSMOS_DB_ENDPOINT string = cosmosDb.outputs.cosmosDbEndpoint

@description('The name of the Service Bus namespace')
output AZURE_SERVICE_BUS_NAMESPACE string = serviceBus.outputs.serviceBusNamespaceName

@description('The FQDN of the Service Bus namespace')
output AZURE_SERVICE_BUS_FQDN string = serviceBus.outputs.serviceBusNamespaceFqdn

// Duration: Main template structure completed
