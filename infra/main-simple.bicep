// main-simple.bicep
// Simplified deployment for testing
targetScope = 'subscription'

@description('Environment suffix')
param environmentSuffix string = 'dev'

@description('Primary location for resources')
param location string = 'eastus2'

@description('Tags to apply to all resources')
param tags object = {
  Environment: environmentSuffix
  Project: 'Zeus.People'
  Department: 'Academic'
  CostCenter: 'Research'
}

@description('Unique token for resource naming')
param resourceToken string = uniqueString(subscription().subscriptionId, environmentSuffix, location)

@description('Principal ID for RBAC assignments')
param principalId string

// Resource group
resource resourceGroup 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: 'rg-academic-${environmentSuffix}-${location}'
  location: location
  tags: tags
}

// Log Analytics Workspace
module logAnalytics 'modules/logAnalytics.bicep' = {
  scope: resourceGroup
  name: 'logAnalytics-deployment'
  params: {
    workspaceName: 'law-academic-${environmentSuffix}-${resourceToken}'
    location: location
    tags: tags
    skuName: 'PerGB2018'
    retentionInDays: 30
  }
}

// Key Vault (simplified)
module keyVault 'modules/keyVault.bicep' = {
  scope: resourceGroup
  name: 'keyVault-deployment'
  params: {
    keyVaultName: 'kv${take(resourceToken, 12)}'
    location: location
    tags: tags
    tenantId: subscription().tenantId
    accessPrincipalId: principalId
    logAnalyticsWorkspaceId: logAnalytics.outputs.workspaceId
  }
}

// Output the resource group name for reference
output resourceGroupName string = resourceGroup.name
output logAnalyticsWorkspaceId string = logAnalytics.outputs.workspaceId
output keyVaultName string = keyVault.outputs.keyVaultName
