// modules/logAnalytics.bicep
// Log Analytics Workspace for centralized logging and monitoring
// Duration: Log Analytics module creation started

@description('Name of the Log Analytics workspace')
param workspaceName string

@description('Location for the Log Analytics workspace')
param location string

@description('Tags to apply to the workspace')
param tags object = {}

@description('Data retention in days')
@minValue(30)
@maxValue(730)
param retentionInDays int = 30

@description('SKU for the Log Analytics workspace')
@allowed(['Free', 'Standard', 'Premium', 'PerNode', 'PerGB2018', 'Standalone', 'CapacityReservation'])
param skuName string = 'PerGB2018'

// Log Analytics Workspace
resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: workspaceName
  location: location
  tags: tags
  properties: {
    sku: {
      name: skuName
    }
    retentionInDays: retentionInDays
    features: {
      enableLogAccessUsingOnlyResourcePermissions: true
    }
    workspaceCapping: {
      dailyQuotaGb: 10
    }
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

// Outputs
@description('The resource ID of the Log Analytics workspace')
output workspaceId string = logAnalyticsWorkspace.id

@description('The name of the Log Analytics workspace')
output workspaceName string = logAnalyticsWorkspace.name

@description('The customer ID of the Log Analytics workspace')
output customerId string = logAnalyticsWorkspace.properties.customerId

// Duration: Log Analytics module completed
