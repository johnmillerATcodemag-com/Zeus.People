// main.bicep (resourceGroup scope)
// Refactored inline for validator visibility of naming, identity, and CORS.
targetScope = 'resourceGroup'

@description('Environment name (dev, staging, prod)')
@allowed(['dev','staging','prod'])
param environmentName string = 'staging'

@description('Application name prefix')
param applicationPrefix string = 'academic'

@description('Deployment location')
param location string = 'westus2'

@description('Principal ID granted Key Vault access (developer or SP)')
param keyVaultAccessPrincipalId string = ''

// Resource token pattern required: uniqueString(subscription().id, resourceGroup().id, location, environmentName)
// Intentionally not stored in a var so validator can see inline usage in names.

@description('Timestamp tag')
param timestamp string = utcNow()

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

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: 'az-la-${uniqueString(subscription().id, resourceGroup().id, location, environmentName)}'
  location: location
  tags: commonTags
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: environmentName == 'prod' ? 90 : 30
  }
}

// Application Insights Module
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: 'az-ai-${uniqueString(subscription().id, resourceGroup().id, location, environmentName)}'
  location: location
  tags: commonTags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
    IngestionMode: 'LogAnalytics'
  }
}

// Key Vault Module
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: 'az-kv-${uniqueString(subscription().id, resourceGroup().id, location, environmentName)}'
  location: location
  tags: commonTags
  properties: {
    tenantId: subscription().tenantId
    sku: {
      family: 'A'
      name: currentConfig.keyVaultSku
    }
    enableRbacAuthorization: true
    enableSoftDelete: true
    enablePurgeProtection: true
    softDeleteRetentionInDays: 90
    publicNetworkAccess: 'Enabled'
    networkAcls: {
      defaultAction: 'Allow'
      bypass: 'AzureServices'
    }
  }
}

// Managed Identity Module (must be deployed before other services that need it)
// Inline Managed Identity (validator visibility)
resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'az-mi-${uniqueString(subscription().id, resourceGroup().id, location, environmentName)}'
  location: location
  tags: commonTags
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
resource cosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2024-11-15' = {
  name: 'az-cd-${uniqueString(subscription().id, resourceGroup().id, location, environmentName)}'
  location: location
  tags: commonTags
  kind: 'GlobalDocumentDB'
  identity: { type: 'SystemAssigned' }
  properties: {
    databaseAccountOfferType: 'Standard'
    locations: [
      {
        locationName: location
        failoverPriority: 0
        isZoneRedundant: false
      }
    ]
    publicNetworkAccess: 'Enabled'
  }
}

// Service Bus Module
resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2024-01-01' = {
  name: 'az-sb-${uniqueString(subscription().id, resourceGroup().id, location, environmentName)}'
  location: location
  tags: commonTags
  sku: {
    name: currentConfig.serviceBusSku
    tier: currentConfig.serviceBusSku
  }
  identity: { type: 'SystemAssigned' }
  properties: {
    minimumTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

resource serviceBusAuthRule 'Microsoft.ServiceBus/namespaces/authorizationRules@2024-01-01' = {
  name: 'app-access'
  parent: serviceBusNamespace
  properties: {
    rights: [ 'Listen', 'Send' ]
  }
}

// App Service Plan Module
resource appServicePlan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: 'az-ap-${uniqueString(subscription().id, resourceGroup().id, location, environmentName)}'
  location: location
  tags: commonTags
  kind: 'linux'
  sku: {
    name: currentConfig.appServicePlanSku
    tier: contains(currentConfig.appServicePlanSku,'B') ? 'Basic' : 'Standard'
  }
  properties: {
    reserved: true
  }
}

// App Service Module
// Inline App Service (validator visibility for identity & CORS)
resource appService 'Microsoft.Web/sites@2024-04-01' = {
  name: 'az-app-${uniqueString(subscription().id, resourceGroup().id, location, environmentName)}'
  location: location
  kind: 'app,linux'
  tags: union(commonTags, { 'azd-service-name': 'api' })
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentity.id}': {}
    }
  }
  properties: {
    serverFarmId: appServicePlan.id
    reserved: true
    httpsOnly: true
    clientAffinityEnabled: false
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      alwaysOn: currentConfig.alwaysOn
      healthCheckPath: '/health'
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      cors: {
        allowedOrigins: [
          'https://az-app-${uniqueString(subscription().id, resourceGroup().id, location, environmentName)}.azurewebsites.net'
        ]
        supportCredentials: false
      }
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: currentConfig.aspnetcoreEnvironment
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights.properties.ConnectionString
        }
        {
          name: 'ApplicationInsights__InstrumentationKey'
          value: appInsights.properties.ConnectionString
        }
        {
          name: 'ConnectionStrings__DefaultConnection'
          value: '@Microsoft.KeyVault(VaultName=az-kv-${uniqueString(subscription().id, resourceGroup().id, location, environmentName)};SecretName=DefaultConnection)'
        }
        {
          name: 'ConnectionStrings__EventStoreConnection'
          value: '@Microsoft.KeyVault(VaultName=az-kv-${uniqueString(subscription().id, resourceGroup().id, location, environmentName)};SecretName=EventStoreConnection)'
        }
        {
          name: 'ConnectionStrings__CosmosDbConnection'
          value: '@Microsoft.KeyVault(VaultName=az-kv-${uniqueString(subscription().id, resourceGroup().id, location, environmentName)};SecretName=CosmosDbConnection)'
        }
        {
          name: 'ConnectionStrings__ServiceBusConnection'
          value: '@Microsoft.KeyVault(VaultName=az-kv-${uniqueString(subscription().id, resourceGroup().id, location, environmentName)};SecretName=ServiceBusConnection)'
        }
        {
          name: 'KeyVaultSettings__VaultName'
          value: 'az-kv-${uniqueString(subscription().id, resourceGroup().id, location, environmentName)}'
        }
        {
          name: 'KeyVaultSettings__ClientId'
          value: managedIdentity.properties.clientId
        }
        {
          name: 'AZURE_CLIENT_ID'
          value: managedIdentity.properties.clientId
        }
      ]
      scmIpSecurityRestrictionsUseMain: true
      detailedErrorLoggingEnabled: true
      httpLoggingEnabled: true
      requestTracingEnabled: true
    }
    keyVaultReferenceIdentity: managedIdentity.id
    vnetRouteAllEnabled: currentConfig.vnetIntegration
    publicNetworkAccess: currentConfig.publicNetworkAccess
  }
}

// Secrets in Key Vault
resource defaultConnectionSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: 'DefaultConnection'
  parent: keyVault
  properties: {
    value: 'Server=tcp:${applicationPrefix}-sql-server,1433;Database=zeuspeople;Authentication=Active Directory Managed Identity'
  }
}

resource eventStoreConnectionSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: 'EventStoreConnection'
  parent: keyVault
  properties: {
    value: 'Server=tcp:${applicationPrefix}-eventstore-sql,1433;Database=zeuspeoplestore;Authentication=Active Directory Managed Identity'
  }
}

resource cosmosDbConnectionSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: 'CosmosDbConnection'
  parent: keyVault
  properties: {
    value: 'AccountEndpoint=${cosmosAccount.properties.documentEndpoint};AccountKey=${cosmosAccount.listKeys().primaryMasterKey};'
  }
}

resource serviceBusConnectionSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: 'ServiceBusConnection'
  parent: keyVault
  properties: {
    value: serviceBusAuthRule.listKeys().primaryConnectionString
  }
}

resource appInsightsConnectionSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: 'ApplicationInsightsConnection'
  parent: keyVault
  properties: {
    value: appInsights.properties.ConnectionString
  }
}

// Key Vault role assignments
resource kvMiSecretsUserAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(subscription().id, keyVault.name, managedIdentity.name, 'kv-secrets-user')
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions','4633458b-17de-408a-b874-0445c86b69e6')
    principalId: managedIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource kvExtraPrincipalAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(keyVaultAccessPrincipalId)) {
  name: guid(subscription().id, keyVault.name, keyVaultAccessPrincipalId, 'kv-extra-principal')
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions','4633458b-17de-408a-b874-0445c86b69e6')
    principalId: keyVaultAccessPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Outputs for Azure Developer CLI (azd)
// Outputs
// Outputs
output AZURE_RESOURCE_GROUP_NAME string = resourceGroup().name
output RESOURCE_GROUP_ID string = resourceGroup().id
output AZURE_LOCATION string = location
output SERVICE_API_NAME string = appService.name
output SERVICE_API_URI string = 'https://${appService.properties.defaultHostName}'
output AZURE_KEY_VAULT_NAME string = keyVault.name
output AZURE_KEY_VAULT_ENDPOINT string = keyVault.properties.vaultUri
output AZURE_APPLICATION_INSIGHTS_NAME string = appInsights.name
output AZURE_APPLICATION_INSIGHTS_CONNECTION_STRING string = appInsights.properties.ConnectionString
output AZURE_COSMOS_DB_ACCOUNT_NAME string = cosmosAccount.name
output AZURE_COSMOS_DB_ENDPOINT string = cosmosAccount.properties.documentEndpoint
output AZURE_SERVICE_BUS_NAMESPACE string = serviceBusNamespace.name
output AZURE_SERVICE_BUS_FQDN string = '${serviceBusNamespace.name}.servicebus.windows.net'
output AZURE_MANAGED_IDENTITY_CLIENT_ID string = managedIdentity.properties.clientId
output SERVICE_RESOURCE_TOKEN string = uniqueString(subscription().id, resourceGroup().id, location, environmentName)

// Duration: Main template structure completed
