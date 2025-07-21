// modules/sqlDatabase.bicep
// Azure SQL Database for write operations and event store
// Duration: SQL Database module creation started

@description('Name of the SQL Server')
param sqlServerName string

@description('Location for SQL Server')
param location string

@description('Tags to apply to SQL resources')
param tags object = {}

@description('SQL Server administrator login')
param administratorLogin string

@description('SQL Server administrator password')
@secure()
param administratorLoginPassword string

@description('Name of the main database')
param databaseName string = 'Zeus.People'

@description('Name of the event store database')
param eventStoreDatabaseName string = 'Zeus.People.EventStore'

@description('SKU name for the databases')
param skuName string = 'S2'

@description('SKU tier for the databases')
param skuTier string = 'Standard'

@description('Enable Advanced Threat Protection')
param enableAdvancedThreatProtection bool = true

@description('Enable auditing to Log Analytics')
param enableAuditingToLogAnalytics bool = true

@description('Log Analytics workspace ID for auditing')
param logAnalyticsWorkspaceId string

@description('Managed identity principal ID for RBAC')
param managedIdentityPrincipalId string

@description('Minimum TLS version')
@allowed(['1.0', '1.1', '1.2'])
param minimalTlsVersion string = '1.2'

// SQL Server
resource sqlServer 'Microsoft.Sql/servers@2021-11-01' = {
  name: sqlServerName
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    administratorLogin: administratorLogin
    administratorLoginPassword: administratorLoginPassword
    version: '12.0'
    minimalTlsVersion: minimalTlsVersion
    publicNetworkAccess: 'Enabled'
    restrictOutboundNetworkAccess: 'Disabled'
  }
}

// Azure AD Administrator (if principal ID is provided)
resource sqlServerAadAdmin 'Microsoft.Sql/servers/administrators@2021-11-01' = if (!empty(managedIdentityPrincipalId)) {
  parent: sqlServer
  name: 'ActiveDirectory'
  properties: {
    administratorType: 'ActiveDirectory'
    login: 'Zeus.People.API'
    sid: managedIdentityPrincipalId
    tenantId: subscription().tenantId
  }
}

// Firewall rule to allow Azure services
resource sqlServerFirewallRule 'Microsoft.Sql/servers/firewallRules@2021-11-01' = {
  parent: sqlServer
  name: 'AllowAllWindowsAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// Academic Database (Write model)
resource academicDatabase 'Microsoft.Sql/servers/databases@2021-11-01' = {
  parent: sqlServer
  name: databaseName
  location: location
  tags: tags
  sku: {
    name: skuName
    tier: skuTier
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 2147483648 // 2 GB (max for Basic/Standard S0/S1)
    catalogCollation: 'SQL_Latin1_General_CP1_CI_AS'
    zoneRedundant: false
    readScale: 'Disabled'
    requestedBackupStorageRedundancy: 'Geo'
    isLedgerOn: false
  }
}

// Event Store Database
resource eventStoreDatabase 'Microsoft.Sql/servers/databases@2021-11-01' = {
  parent: sqlServer
  name: eventStoreDatabaseName
  location: location
  tags: tags
  sku: {
    name: skuName
    tier: skuTier
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 2147483648 // 2 GB (max for Basic/Standard S0/S1)
    catalogCollation: 'SQL_Latin1_General_CP1_CI_AS'
    zoneRedundant: false
    readScale: 'Disabled'
    requestedBackupStorageRedundancy: 'Geo'
    isLedgerOn: false
  }
}

// Advanced Threat Protection
resource sqlServerSecurityAlertPolicy 'Microsoft.Sql/servers/securityAlertPolicies@2021-11-01' = if (enableAdvancedThreatProtection) {
  parent: sqlServer
  name: 'Default'
  properties: {
    state: 'Enabled'
    emailAddresses: []
    emailAccountAdmins: true
    retentionDays: 30
  }
}

// Vulnerability Assessment
resource sqlServerVulnerabilityAssessment 'Microsoft.Sql/servers/vulnerabilityAssessments@2021-11-01' = if (enableAdvancedThreatProtection) {
  parent: sqlServer
  name: 'Default'
  properties: {
    storageContainerPath: 'https://sqlsecurityaudit${uniqueString(sqlServer.id)}.blob.core.windows.net/vulnerability-assessment/'
    recurringScans: {
      isEnabled: true
      emailSubscriptionAdmins: true
      emails: []
    }
  }
  dependsOn: [
    sqlServerSecurityAlertPolicy
  ]
}

// Auditing configuration
resource sqlServerAuditing 'Microsoft.Sql/servers/auditingSettings@2021-11-01' = if (enableAuditingToLogAnalytics) {
  parent: sqlServer
  name: 'default'
  properties: {
    state: 'Enabled'
    isAzureMonitorTargetEnabled: true
    retentionDays: 30
  }
}

// Note: Diagnostic settings removed due to configuration complexity

// Note: Diagnostic settings removed due to configuration complexity

// Diagnostic settings for Event Store Database
// Note: Diagnostic settings removed due to configuration complexity

// Outputs
@description('The resource ID of the SQL Server')
output sqlServerId string = sqlServer.id

@description('The name of the SQL Server')
output sqlServerName string = sqlServer.name

@description('The FQDN of the SQL Server')
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName

@description('The name of the academic database')
output academicDatabaseName string = academicDatabase.name

@description('The name of the event store database')
output eventStoreDatabaseName string = eventStoreDatabase.name

@description('The academic database connection string')
output academicDatabaseConnectionString string = 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Initial Catalog=${academicDatabase.name};Authentication=Active Directory Default;'

@description('The event store database connection string')
output eventStoreDatabaseConnectionString string = 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Initial Catalog=${eventStoreDatabase.name};Authentication=Active Directory Default;'

// Duration: SQL Database module completed
