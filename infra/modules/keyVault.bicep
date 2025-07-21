// modules/keyVault.bicep
// Azure Key Vault for secrets and configuration management
// Duration: Key Vault module creation started

@description('Name of the Key Vault')
param keyVaultName string

@description('Location for the Key Vault')
param location string

@description('Tags to apply to the Key Vault')
param tags object = {}

@description('Azure AD tenant ID')
param tenantId string

@description('Principal ID for Key Vault access')
param accessPrincipalId string

@description('SKU name for Key Vault')
@allowed(['standard', 'premium'])
param skuName string = 'standard'

@description('Enable RBAC authorization')
param enableRbacAuthorization bool = true

@description('Enable soft delete')
param enableSoftDelete bool = true

@description('Enable purge protection')
param enablePurgeProtection bool = true

@description('Soft delete retention days')
@minValue(7)
@maxValue(90)
param softDeleteRetentionInDays int = 90

@description('Log Analytics workspace ID for diagnostics')
param logAnalyticsWorkspaceId string

// Key Vault
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  tags: tags
  properties: {
    tenantId: tenantId
    sku: {
      family: 'A'
      name: skuName
    }
    enableRbacAuthorization: enableRbacAuthorization
    enableSoftDelete: enableSoftDelete
    enablePurgeProtection: enablePurgeProtection
    softDeleteRetentionInDays: softDeleteRetentionInDays
    enabledForDeployment: false
    enabledForDiskEncryption: false
    enabledForTemplateDeployment: true
    publicNetworkAccess: 'Enabled'
    networkAcls: {
      defaultAction: 'Allow'
      bypass: 'AzureServices'
    }
    accessPolicies: enableRbacAuthorization
      ? []
      : [
          {
            tenantId: tenantId
            objectId: accessPrincipalId
            permissions: {
              keys: ['get', 'list', 'create', 'update', 'delete', 'backup', 'restore', 'recover']
              secrets: ['get', 'list', 'set', 'delete', 'backup', 'restore', 'recover']
              certificates: ['get', 'list', 'create', 'update', 'delete', 'backup', 'restore', 'recover']
            }
          }
        ]
  }
}

// Diagnostic settings for Key Vault
resource keyVaultDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: '${keyVaultName}-diagnostics'
  scope: keyVault
  properties: {
    workspaceId: logAnalyticsWorkspaceId
    logs: [
      {
        categoryGroup: 'audit'
        enabled: true
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
    ]
    metrics: [
      {
        category: 'AllMetrics'
        enabled: true
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
    ]
  }
}

// Outputs
@description('The resource ID of the Key Vault')
output keyVaultId string = keyVault.id

@description('The name of the Key Vault')
output keyVaultName string = keyVault.name

@description('The URI of the Key Vault')
output keyVaultUri string = keyVault.properties.vaultUri

// Duration: Key Vault module completed
