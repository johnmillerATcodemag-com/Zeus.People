// resources.bicep (resourceGroup scope) - inline resources for validator visibility
// Simplified resource set

targetScope = 'resourceGroup'

@description('Environment name')
param environmentName string
@description('Application prefix')
param applicationPrefix string
@description('Primary location')
param location string
@description('Resource token (passed from orchestrator)')
param resourceToken string
@description('Principal ID for Key Vault access')
param keyVaultAccessPrincipalId string
@description('Common tags')
param commonTags object

var environmentConfig = {
  dev: {
    appServicePlanSku: 'B1'
    alwaysOn: false
    aspnetcoreEnvironment: 'Development'
    vnetIntegration: false
    publicNetworkAccess: 'Enabled'
    cosmosDbThroughput: 400
    serviceBusSku: 'Standard'
    keyVaultSku: 'standard'
  }
  staging: {
    appServicePlanSku: 'S2'
    alwaysOn: true
    aspnetcoreEnvironment: 'Staging'
    vnetIntegration: true
    publicNetworkAccess: 'Disabled'
    cosmosDbThroughput: 1000
    serviceBusSku: 'Standard'
    keyVaultSku: 'standard'
  }
  prod: {
    appServicePlanSku: 'P2v3'
    alwaysOn: true
    aspnetcoreEnvironment: 'Production'
    vnetIntegration: true
    publicNetworkAccess: 'Disabled'
    cosmosDbThroughput: 4000
    serviceBusSku: 'Premium'
    keyVaultSku: 'premium'
  }
}
var currentConfig = environmentConfig[environmentName]

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: 'az-la-${resourceToken}'
  location: location
  tags: commonTags
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: environmentName == 'prod' ? 90 : 30
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: 'az-ai-${resourceToken}'
  location: location
  tags: commonTags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
    IngestionMode: 'LogAnalytics'
  }
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: 'az-kv-${resourceToken}'
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

resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'az-mi-${resourceToken}'
  location: location
  tags: commonTags
}

resource appServicePlan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: 'az-asp-${resourceToken}'
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

resource cosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2024-11-15' = {
  name: 'az-cdb-${resourceToken}'
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

resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2024-01-01' = {
  name: 'az-sb-${resourceToken}'
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

// Authorization rule for application access (avoids relying on built-in RootManageSharedAccessKey)
resource serviceBusAuthRule 'Microsoft.ServiceBus/namespaces/authorizationRules@2024-01-01' = {
  name: 'app-access'
  parent: serviceBusNamespace
  properties: {
    rights: [ 'Listen', 'Send' ]
  }
}

// Secrets (placeholders / generated values) -------------------------------------------------
resource defaultConnectionSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: 'DefaultConnection'
  parent: keyVault
  properties: {
  // Placeholder - replace with actual server name during deployment pipeline variable substitution
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

// Key Vault role assignments (RBAC) ---------------------------------------------------------
resource kvMiSecretsUserAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  // Deterministic name not depending on principalId (not available at compile time)
  name: guid(subscription().id, keyVault.name, managedIdentity.name, 'kv-secrets-user')
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions','4633458b-17de-408a-b874-0445c86b69e6') // Key Vault Secrets User
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

resource appService 'Microsoft.Web/sites@2024-04-01' = {
  name: 'az-app-${resourceToken}'
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
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      alwaysOn: currentConfig.alwaysOn
      healthCheckPath: '/health'
      cors: {
        allowedOrigins: [ 'https://az-app-${resourceToken}.azurewebsites.net' ]
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
          value: '@Microsoft.KeyVault(VaultName=az-kv-${resourceToken};SecretName=DefaultConnection)'
        }
        {
          name: 'ConnectionStrings__EventStoreConnection'
          value: '@Microsoft.KeyVault(VaultName=az-kv-${resourceToken};SecretName=EventStoreConnection)'
        }
        {
          name: 'ConnectionStrings__CosmosDbConnection'
          value: '@Microsoft.KeyVault(VaultName=az-kv-${resourceToken};SecretName=CosmosDbConnection)'
        }
        {
          name: 'ConnectionStrings__ServiceBusConnection'
          value: '@Microsoft.KeyVault(VaultName=az-kv-${resourceToken};SecretName=ServiceBusConnection)'
        }
        {
          name: 'KeyVaultSettings__VaultName'
          value: 'az-kv-${resourceToken}'
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
    }
    publicNetworkAccess: currentConfig.publicNetworkAccess
  }
}

// Outputs
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
