// modules/appInsights.bicep
// Application Insights for application performance monitoring
// Duration: Application Insights module creation started

@description('Name of the Application Insights instance')
param appInsightsName string

@description('Location for Application Insights')
param location string

@description('Tags to apply to the resource')
param tags object = {}

@description('Log Analytics workspace ID for Application Insights')
param logAnalyticsWorkspaceId string

@description('Application type')
@allowed(['web', 'other'])
param applicationType string = 'web'

@description('Ingestion mode')
@allowed(['ApplicationInsights', 'ApplicationInsightsWithDiagnosticSettings', 'LogAnalytics'])
param ingestionMode string = 'LogAnalytics'

// Application Insights
resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  tags: tags
  kind: applicationType
  properties: {
    Application_Type: applicationType
    WorkspaceResourceId: logAnalyticsWorkspaceId
    IngestionMode: ingestionMode
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
    DisableIpMasking: false
    DisableLocalAuth: false
  }
}

// Outputs
@description('The resource ID of Application Insights')
output appInsightsId string = applicationInsights.id

@description('The name of Application Insights')
output appInsightsName string = applicationInsights.name

@description('The instrumentation key for Application Insights')
output instrumentationKey string = applicationInsights.properties.InstrumentationKey

@description('The connection string for Application Insights')
output connectionString string = applicationInsights.properties.ConnectionString

@description('The App ID for Application Insights')
output appId string = applicationInsights.properties.AppId

// Duration: Application Insights module completed
