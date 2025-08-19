targetScope = 'subscription'

@description('Name of the environment (e.g., dev, staging, prod)')
param environmentName string = 'staging'

@description('Prefix for all application resources')
param applicationPrefix string = 'academic'

@description('Primary location for resources')
param primaryLocation string = 'westus2'

@description('Secondary location for resources')
param secondaryLocation string = 'eastus2'

@description('Current timestamp for tagging')
param timestamp string = utcNow()

@description('Principal ID for Key Vault access (service principal or user)')
param keyVaultAccessPrincipalId string

// Variables for consistent naming and configuration
var resourceGroupName = 'rg-${applicationPrefix}-${environmentName}-${primaryLocation}'
var commonTags = {
  Environment: environmentName
  Project: 'Zeus.People'
  CreatedDate: timestamp
  ManagedBy: 'AzureDevCLI'
}

var resourceToken = uniqueString(subscription().id, resourceGroupName)

// Environment-specific configurations
var configurations = {
  dev: {
    appServicePlanSku: 'B1'
    serviceBusSkuName: 'Basic'
    cosmosDbTier: 'Standard'
    cosmosDbMaxThroughput: 1000
  }
  staging: {
    appServicePlanSku: 'S1'
    serviceBusSkuName: 'Standard'
    cosmosDbTier: 'Standard'
    cosmosDbMaxThroughput: 4000
  }
  prod: {
    appServicePlanSku: 'P1v3'
    serviceBusSkuName: 'Premium'
    cosmosDbTier: 'Standard'
    cosmosDbMaxThroughput: 10000
  }
}

var currentConfig = configurations[environmentName]

// Resource Group
resource resourceGroup 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: resourceGroupName
  location: primaryLocation
  tags: commonTags
}

// Managed Identity for App Service
module managedIdentity 'modules/managedIdentity.bicep' = {
  scope: resourceGroup
  name: 'managedIdentity-deployment'
  params: {
    identityName: 'id-${applicationPrefix}-${environmentName}-${resourceToken}'
    location: primaryLocation
    tags: commonTags
  }
}

// Log Analytics Workspace
module logAnalytics 'modules/logAnalytics.bicep' = {
  scope: resourceGroup
  name: 'logAnalytics-deployment'
  params: {
    workspaceName: 'law-${applicationPrefix}-${environmentName}-${resourceToken}'
    location: primaryLocation
    tags: commonTags
  }
}

// Application Insights
module applicationInsights 'modules/applicationInsights.bicep' = {
  scope: resourceGroup
  name: 'applicationInsights-deployment'
  params: {
    applicationInsightsName: 'ai-${applicationPrefix}-${environmentName}-${resourceToken}'
    location: primaryLocation
    tags: commonTags
    logAnalyticsWorkspaceId: logAnalytics.outputs.workspaceId
  }
}

// Key Vault
module keyVault 'modules/keyVault.bicep' = {
  scope: resourceGroup
  name: 'keyVault-deployment'
  params: {
    keyVaultName: 'kv${resourceToken}'
    location: primaryLocation
    tags: commonTags
    managedIdentityPrincipalId: managedIdentity.outputs.principalId
    accessPrincipalId: keyVaultAccessPrincipalId
  }
}

// Service Bus
module serviceBus 'modules/serviceBus.bicep' = {
  scope: resourceGroup
  name: 'serviceBus-deployment'
  params: {
    namespaceName: 'sb-${applicationPrefix}-${environmentName}-${resourceToken}'
    location: primaryLocation
    tags: commonTags
    skuName: currentConfig.serviceBusSkuName
    managedIdentityPrincipalId: managedIdentity.outputs.principalId
  }
}

// Cosmos DB
module cosmosDb 'modules/cosmosDb.bicep' = {
  scope: resourceGroup
  name: 'cosmosDb-deployment'
  params: {
    accountName: 'cosmos-${applicationPrefix}-${environmentName}-${resourceToken}'
    location: primaryLocation
    tags: commonTags
    consistencyLevel: 'Session'
    multipleWriteLocations: false
    maxThroughput: currentConfig.cosmosDbMaxThroughput
    managedIdentityPrincipalId: managedIdentity.outputs.principalId
  }
}

// App Service Plan
module appServicePlan 'modules/appServicePlan.bicep' = {
  scope: resourceGroup
  name: 'appServicePlan-deployment'
  params: {
    planName: 'asp-${applicationPrefix}-${environmentName}-${resourceToken}'
    location: primaryLocation
    tags: commonTags
    skuName: currentConfig.appServicePlanSku
    kind: 'linux'
    reserved: true
  }
}

// App Service
module appService 'modules/appService.bicep' = {
  scope: resourceGroup
  name: 'appService-deployment'
  params: {
    appName: 'app-${applicationPrefix}-${environmentName}-${resourceToken}'
    location: primaryLocation
    tags: commonTags
    appServicePlanId: appServicePlan.outputs.id
    managedIdentityId: managedIdentity.outputs.id
    keyVaultName: keyVault.outputs.name
    applicationInsightsConnectionString: applicationInsights.outputs.connectionString
    serviceBusConnectionString: serviceBus.outputs.connectionString
    cosmosDbConnectionString: cosmosDb.outputs.connectionString
    linuxFxVersion: 'DOTNETCORE|8.0'
  }
}

// Outputs
output resourceGroupName string = resourceGroup.name
output appServiceName string = appService.outputs.name
output appServiceUrl string = appService.outputs.url
output managedIdentityId string = managedIdentity.outputs.id
output keyVaultName string = keyVault.outputs.name
output serviceBusNamespace string = serviceBus.outputs.namespaceName
output cosmosDbAccountName string = cosmosDb.outputs.accountName
output applicationInsightsName string = applicationInsights.outputs.name
output logAnalyticsWorkspaceName string = logAnalytics.outputs.workspaceName
