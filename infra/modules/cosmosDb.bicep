// modules/cosmosDb.bicep
// Azure Cosmos DB for read model operations (CQRS pattern)
// Duration: Cosmos DB module creation started

@description('Name of the Cosmos DB account')
param cosmosDbAccountName string

@description('Location for Cosmos DB')
param location string

@description('Tags to apply to Cosmos DB')
param tags object = {}

@description('Name of the Cosmos DB database')
param databaseName string = 'Zeus.People'

@description('Throughput for the database (RU/s)')
@minValue(400)
@maxValue(100000)
param throughput int = 400

@description('Enable multiple write locations')
param enableMultipleWriteLocations bool = false

@description('Secondary location for geo-replication')
param secondaryLocation string = ''

@description('Managed identity principal ID for RBAC')
param managedIdentityPrincipalId string

@description('Log Analytics workspace ID for diagnostics')
param logAnalyticsWorkspaceId string

@description('API kind for Cosmos DB')
@allowed(['GlobalDocumentDB', 'MongoDB', 'Parse'])
param kind string = 'GlobalDocumentDB'

@description('Consistency level')
@allowed(['Eventual', 'Session', 'BoundedStaleness', 'Strong', 'ConsistentPrefix'])
param consistencyLevel string = 'Session'

// Define locations array based on whether secondary location is provided
var locations = enableMultipleWriteLocations && !empty(secondaryLocation)
  ? [
      {
        locationName: location
        failoverPriority: 0
        isZoneRedundant: false
      }
      {
        locationName: secondaryLocation
        failoverPriority: 1
        isZoneRedundant: false
      }
    ]
  : [
      {
        locationName: location
        failoverPriority: 0
        isZoneRedundant: false
      }
    ]

// Cosmos DB Account
resource cosmosDbAccount 'Microsoft.DocumentDB/databaseAccounts@2024-11-15' = {
  name: cosmosDbAccountName
  location: location
  tags: tags
  kind: kind
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    databaseAccountOfferType: 'Standard'
    consistencyPolicy: {
      defaultConsistencyLevel: consistencyLevel
    }
    locations: locations
    enableMultipleWriteLocations: false
    enableAutomaticFailover: false
    publicNetworkAccess: 'Enabled'
  }
}

// Cosmos DB SQL Database
resource cosmosDbDatabase 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-11-15' = {
  parent: cosmosDbAccount
  name: databaseName
  properties: {
    resource: {
      id: databaseName
    }
    options: throughput <= 4000
      ? {
          throughput: throughput
        }
      : {
          autoscaleSettings: {
            maxThroughput: throughput
          }
        }
  }
}

// Container for Academics read model
resource academicsContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-11-15' = {
  parent: cosmosDbDatabase
  name: 'academics'
  properties: {
    resource: {
      id: 'academics'
      partitionKey: {
        paths: ['/id']
        kind: 'Hash'
      }
      indexingPolicy: {
        indexingMode: 'consistent'
        automatic: true
        includedPaths: [
          {
            path: '/*'
          }
        ]
        excludedPaths: [
          {
            path: '/"_etag"/?'
          }
        ]
        compositeIndexes: [
          [
            {
              path: '/departmentId'
              order: 'ascending'
            }
            {
              path: '/title'
              order: 'ascending'
            }
          ]
        ]
      }
      uniqueKeyPolicy: {
        uniqueKeys: [
          {
            paths: ['/employeeId']
          }
        ]
      }
    }
  }
}

// Container for Departments read model
resource departmentsContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-11-15' = {
  parent: cosmosDbDatabase
  name: 'departments'
  properties: {
    resource: {
      id: 'departments'
      partitionKey: {
        paths: ['/id']
        kind: 'Hash'
      }
      indexingPolicy: {
        indexingMode: 'consistent'
        automatic: true
        includedPaths: [
          {
            path: '/*'
          }
        ]
        excludedPaths: [
          {
            path: '/"_etag"/?'
          }
        ]
        compositeIndexes: [
          [
            {
              path: '/building'
              order: 'ascending'
            }
            {
              path: '/name'
              order: 'ascending'
            }
          ]
        ]
      }
    }
  }
}

// Container for Rooms read model
resource roomsContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-11-15' = {
  parent: cosmosDbDatabase
  name: 'rooms'
  properties: {
    resource: {
      id: 'rooms'
      partitionKey: {
        paths: ['/id']
        kind: 'Hash'
      }
      indexingPolicy: {
        indexingMode: 'consistent'
        automatic: true
        includedPaths: [
          {
            path: '/*'
          }
        ]
        excludedPaths: [
          {
            path: '/"_etag"/?'
          }
        ]
        compositeIndexes: [
          [
            {
              path: '/building'
              order: 'ascending'
            }
            {
              path: '/roomNumber'
              order: 'ascending'
            }
          ]
        ]
      }
      uniqueKeyPolicy: {
        uniqueKeys: [
          {
            paths: ['/roomNumber']
          }
        ]
      }
    }
  }
}

// Container for Extensions read model
resource extensionsContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-11-15' = {
  parent: cosmosDbDatabase
  name: 'extensions'
  properties: {
    resource: {
      id: 'extensions'
      partitionKey: {
        paths: ['/id']
        kind: 'Hash'
      }
      indexingPolicy: {
        indexingMode: 'consistent'
        automatic: true
        includedPaths: [
          {
            path: '/*'
          }
        ]
        excludedPaths: [
          {
            path: '/"_etag"/?'
          }
        ]
        compositeIndexes: [
          [
            {
              path: '/academicId'
              order: 'ascending'
            }
            {
              path: '/phoneNumber'
              order: 'ascending'
            }
          ]
        ]
      }
      uniqueKeyPolicy: {
        uniqueKeys: [
          {
            paths: ['/phoneNumber']
          }
        ]
      }
    }
  }
}

// Role assignment for Cosmos DB Data Contributor to managed identity
resource cosmosDbRoleAssignment 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2024-11-15' = {
  parent: cosmosDbAccount
  name: guid(cosmosDbAccount.id, managedIdentityPrincipalId, 'CosmosDBDataContributor')
  properties: {
    roleDefinitionId: '${cosmosDbAccount.id}/sqlRoleDefinitions/00000000-0000-0000-0000-000000000002' // Cosmos DB Built-in Data Contributor
    principalId: managedIdentityPrincipalId
    scope: cosmosDbAccount.id
  }
}

// Diagnostic settings
resource cosmosDbDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: '${cosmosDbAccountName}-diagnostics'
  scope: cosmosDbAccount
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
        category: 'Requests'
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
@description('The resource ID of the Cosmos DB account')
output cosmosDbAccountId string = cosmosDbAccount.id

@description('The name of the Cosmos DB account')
output cosmosDbAccountName string = cosmosDbAccount.name

@description('The endpoint of the Cosmos DB account')
output cosmosDbEndpoint string = cosmosDbAccount.properties.documentEndpoint

@description('The primary master key for Cosmos DB (secure)')
output primaryMasterKey string = cosmosDbAccount.listKeys().primaryMasterKey

@description('The database name')
output databaseName string = cosmosDbDatabase.name

// Duration: Cosmos DB module completed
