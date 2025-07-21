@description('The environment name (dev, stg, prod)')
@allowed(['dev', 'stg', 'prod'])
param environment string = 'dev'

@description('The location for all resources')
param location string = resourceGroup().location

@description('The unique suffix for resource names')
param uniqueSuffix string = substring(uniqueString(resourceGroup().id), 0, 4)

@description('Enable soft delete for Key Vault')
param enableSoftDelete bool = true

@description('Soft delete retention days')
@minValue(7)
@maxValue(90)
param softDeleteRetentionDays int = 7

@description('Tags for all resources')
param tags object = {
  Environment: environment
  Application: 'Zeus.People'
  Component: 'Configuration'
  ManagedBy: 'Azure-Bicep'
}

// Variables
var keyVaultName = 'kv-zeus-people-${environment}-${uniqueSuffix}'
var managedIdentityName = 'id-zeus-people-${environment}'
var appServiceName = 'app-zeus-people-${environment}'
var appServicePlanName = 'plan-zeus-people-${environment}'

// User-Assigned Managed Identity
resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: managedIdentityName
  location: location
  tags: tags
}

// Key Vault
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  tags: tags
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enabledForDeployment: true
    enabledForTemplateDeployment: true
    enabledForDiskEncryption: true
    enableRbacAuthorization: false
    enableSoftDelete: enableSoftDelete
    softDeleteRetentionInDays: softDeleteRetentionDays
    enablePurgeProtection: environment == 'prod' ? true : false
    publicNetworkAccess: 'Enabled'
    networkAcls: {
      defaultAction: 'Allow'
      bypass: 'AzureServices'
    }
    accessPolicies: [
      {
        tenantId: subscription().tenantId
        objectId: managedIdentity.properties.principalId
        permissions: {
          secrets: [
            'get'
            'list'
          ]
          keys: [
            'get'
            'list'
          ]
          certificates: [
            'get'
            'list'
          ]
        }
      }
    ]
  }
}

// App Service Plan
resource appServicePlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: appServicePlanName
  location: location
  tags: tags
  sku: {
    name: environment == 'prod' ? 'P1v3' : (environment == 'stg' ? 'S1' : 'F1')
    tier: environment == 'prod' ? 'PremiumV3' : (environment == 'stg' ? 'Standard' : 'Free')
  }
  kind: 'app'
  properties: {
    reserved: false
  }
}

// App Service
resource appService 'Microsoft.Web/sites@2023-01-01' = {
  name: appServiceName
  location: location
  tags: tags
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
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      netFrameworkVersion: 'v8.0'
      use32BitWorkerProcess: false
      webSocketsEnabled: false
      alwaysOn: environment != 'dev'
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: environment == 'dev' ? 'Development' : (environment == 'stg' ? 'Staging' : 'Production')
        }
        {
          name: 'KeyVault__VaultUrl'
          value: keyVault.properties.vaultUri
        }
        {
          name: 'KeyVault__UseManagedIdentity'
          value: 'true'
        }
        {
          name: 'KeyVault__ClientId'
          value: managedIdentity.properties.clientId
        }
        {
          name: 'AZURE_CLIENT_ID'
          value: managedIdentity.properties.clientId
        }
      ]
    }
  }
}

// Key Vault Secrets
resource databaseConnectionStringSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'Database--ConnectionString'
  properties: {
    value: environment == 'dev'
      ? 'Server=(localdb)\\MSSQLLocalDB;Database=ZeusPeopleDev;Integrated Security=true;Encrypt=False'
      : 'Server=tcp:sql-zeus-people-${environment}.database.windows.net,1433;Initial Catalog=ZeusPeople${toUpper(substring(environment, 0, 1))}${substring(environment, 1)};Authentication=Active Directory Managed Identity;Encrypt=True;TrustServerCertificate=False'
    attributes: {
      enabled: true
    }
  }
}

resource serviceBusConnectionStringSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'ServiceBus--ConnectionString'
  properties: {
    value: 'Endpoint=sb://sb-zeus-people-${environment}.servicebus.windows.net/;Authentication=Managed Identity'
    attributes: {
      enabled: true
    }
  }
}

resource jwtSecretKeySecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'JwtSettings--SecretKey'
  properties: {
    value: base64('${guid(resourceGroup().id)}-${uniqueSuffix}')
    attributes: {
      enabled: true
    }
  }
}

resource jwtIssuerSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'JwtSettings--Issuer'
  properties: {
    value: 'https://${appService.properties.defaultHostName}'
    attributes: {
      enabled: true
    }
  }
}

resource jwtAudienceSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'JwtSettings--Audience'
  properties: {
    value: 'https://${appService.properties.defaultHostName}'
    attributes: {
      enabled: true
    }
  }
}

resource azureAdClientSecretSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'AzureAd--ClientSecret'
  properties: {
    value: guid(resourceGroup().id, 'azuread-client-secret')
    attributes: {
      enabled: true
    }
  }
}

resource applicationInsightsConnectionStringSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'ApplicationInsights--ConnectionString'
  properties: {
    value: 'InstrumentationKey=${guid(resourceGroup().id, 'app-insights')};IngestionEndpoint=https://${location}-8.in.applicationinsights.azure.com/'
    attributes: {
      enabled: true
    }
  }
}

// Outputs
output keyVaultName string = keyVault.name
output keyVaultUri string = keyVault.properties.vaultUri
output managedIdentityName string = managedIdentity.name
output managedIdentityClientId string = managedIdentity.properties.clientId
output managedIdentityPrincipalId string = managedIdentity.properties.principalId
output appServiceName string = appService.name
output appServiceDefaultHostName string = appService.properties.defaultHostName
output resourceGroupName string = resourceGroup().name
output environment string = environment
