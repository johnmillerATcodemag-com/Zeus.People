// modules/appServicePlan.bicep
// Azure App Service Plan for hosting the API
// Duration: App Service Plan module creation started

@description('Name of the App Service Plan')
param appServicePlanName string

@description('Location for the App Service Plan')
param location string

@description('Tags to apply to the App Service Plan')
param tags object = {}

@description('SKU name for the App Service Plan')
param skuName string = 'B1'

@description('SKU tier for the App Service Plan')
param skuTier string = 'Basic'

@description('Kind of App Service Plan')
@allowed(['app', 'linux', 'windows'])
param kind string = 'linux'

@description('Is Linux App Service Plan')
param reserved bool = true

@description('Enable zone redundancy')
param zoneRedundant bool = false

@description('Target worker count')
param targetWorkerCount int = 1

@description('Maximum elastic worker count')
param maximumElasticWorkerCount int = 1

@description('Enable per-site scaling')
param perSiteScaling bool = false

// App Service Plan
resource appServicePlan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: appServicePlanName
  location: location
  tags: tags
  kind: kind
  sku: {
    name: skuName
    tier: skuTier
    capacity: targetWorkerCount
  }
  properties: {
    reserved: reserved
    zoneRedundant: zoneRedundant
    targetWorkerCount: targetWorkerCount
    maximumElasticWorkerCount: maximumElasticWorkerCount
    perSiteScaling: perSiteScaling
    elasticScaleEnabled: false
    hyperV: false
    isXenon: false
  }
}

// Outputs
@description('The resource ID of the App Service Plan')
output appServicePlanId string = appServicePlan.id

@description('The name of the App Service Plan')
output appServicePlanName string = appServicePlan.name

@description('The kind of the App Service Plan')
output appServicePlanKind string = appServicePlan.kind

// Duration: App Service Plan module completed
