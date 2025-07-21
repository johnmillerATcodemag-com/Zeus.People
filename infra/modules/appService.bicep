// Azure App Service module for Zeus.People API
// This module deploys the web app with managed identity and Key Vault integration

@description('The name of the App Service')
param appServiceName string

@description('The name of the App Service Plan')
param appServicePlanName string

@description('Location for the resources')
param location string = resourceGroup().location

@description('Environment configuration object')
param envConfig object

@description('Tags to apply to all resources')
param tags object = {}

@description('The name of the managed identity')
param managedIdentityName string

@description('The name of the Key Vault')
param keyVaultName string

@description('Application Insights connection string')
@secure()
param applicationInsightsConnectionString string

@description('Log Analytics workspace ID for diagnostics')
param logAnalyticsWorkspaceId string

// Reference existing resources
resource appServicePlan 'Microsoft.Web/serverfarms@2024-04-01' existing = {
  name: appServicePlanName
}

resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: managedIdentityName
}

// App Service
resource appService 'Microsoft.Web/sites@2024-04-01' = {
  name: appServiceName
  location: location
  kind: 'app,linux'
  tags: tags

  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentity.id}': {}
    }
  }

  properties: {
    serverFarmId: appServicePlan.id
    reserved: true // Required for Linux
    httpsOnly: true
    clientAffinityEnabled: false

    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      alwaysOn: envConfig.alwaysOn

      // Health check
      healthCheckPath: '/health'

      // HTTPS settings
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'

      // CORS settings
      cors: {
        allowedOrigins: [
          'https://${appServiceName}.azurewebsites.net'
        ]
        supportCredentials: false
      }

      // App settings
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: envConfig.aspnetcoreEnvironment
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: applicationInsightsConnectionString
        }
        {
          name: 'ApplicationInsights__InstrumentationKey'
          value: applicationInsightsConnectionString
        }
        {
          name: 'ConnectionStrings__DefaultConnection'
          value: '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=DefaultConnection)'
        }
        {
          name: 'ConnectionStrings__EventStoreConnection'
          value: '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=EventStoreConnection)'
        }
        {
          name: 'ConnectionStrings__CosmosDbConnection'
          value: '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=CosmosDbConnection)'
        }
        {
          name: 'ConnectionStrings__ServiceBusConnection'
          value: '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=ServiceBusConnection)'
        }
        {
          name: 'KeyVaultSettings__VaultName'
          value: keyVaultName
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

      // IP restrictions for SCM site (Kudu)
      scmIpSecurityRestrictionsUseMain: true

      // Enable detailed error logging for troubleshooting
      detailedErrorLoggingEnabled: true
      httpLoggingEnabled: true
      requestTracingEnabled: true

      // Auto heal settings
      autoHealEnabled: true
      autoHealRules: {
        triggers: {
          requests: {
            count: 100
            timeInterval: '00:01:00'
          }
          privateBytesInKB: 0
          statusCodes: [
            {
              status: 500
              subStatus: 0
              win32Status: 0
              count: 10
              timeInterval: '00:01:00'
            }
          ]
        }
        actions: {
          actionType: 'Recycle'
          minProcessExecutionTime: '00:01:00'
        }
      }
    }

    // Key Vault reference identity
    keyVaultReferenceIdentity: managedIdentity.id

    // Virtual network integration (if enabled)
    vnetRouteAllEnabled: envConfig.vnetIntegration

    // Public network access
    publicNetworkAccess: envConfig.publicNetworkAccess
  }
}

// Diagnostic settings
resource appServiceDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: '${appServiceName}-diagnostics'
  scope: appService
  properties: {
    logs: [
      {
        categoryGroup: 'audit'
        enabled: true
      }
    ]
    metrics: [
      {
        category: 'AllMetrics'
        enabled: true
      }
    ]
    workspaceId: logAnalyticsWorkspaceId
  }
}

// Output values
@description('The resource ID of the App Service')
output appServiceId string = appService.id

@description('The name of the App Service')
output appServiceName string = appService.name

@description('The default hostname of the App Service')
output defaultHostName string = appService.properties.defaultHostName

@description('The URL of the App Service')
output appServiceUrl string = 'https://${appService.properties.defaultHostName}'

@description('The outbound IP addresses of the App Service')
output outboundIpAddresses string = appService.properties.outboundIpAddresses

@description('The possible outbound IP addresses of the App Service')
output possibleOutboundIpAddresses string = appService.properties.possibleOutboundIpAddresses

@description('The principal ID of the managed identity')
output managedIdentityPrincipalId string = managedIdentity.properties.principalId

@description('The client ID of the managed identity')
output managedIdentityClientId string = managedIdentity.properties.clientId
