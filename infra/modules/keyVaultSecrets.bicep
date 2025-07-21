// Key Vault Secrets module for storing application secrets
// This module creates secrets in Key Vault for connection strings and other sensitive data

@description('The name of the Key Vault')
param keyVaultName string

@description('SQL Database connection strings')
@secure()
param sqlConnectionStrings object

@description('Cosmos DB connection string')
@secure()
param cosmosDbConnectionString string

@description('Service Bus connection string')
@secure()
param serviceBusConnectionString string

@description('Application Insights connection string')
@secure()
param applicationInsightsConnectionString string

// Reference existing Key Vault
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

// SQL Database connection string secret
resource defaultConnectionSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: 'DefaultConnection'
  parent: keyVault
  properties: {
    value: sqlConnectionStrings.defaultConnection
    contentType: 'SQL Server connection string'
    attributes: {
      enabled: true
    }
  }
}

// Event Store connection string secret
resource eventStoreConnectionSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: 'EventStoreConnection'
  parent: keyVault
  properties: {
    value: sqlConnectionStrings.eventStoreConnection
    contentType: 'SQL Server connection string for Event Store'
    attributes: {
      enabled: true
    }
  }
}

// Cosmos DB connection string secret
resource cosmosDbConnectionSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: 'CosmosDbConnection'
  parent: keyVault
  properties: {
    value: cosmosDbConnectionString
    contentType: 'Cosmos DB connection string'
    attributes: {
      enabled: true
    }
  }
}

// Service Bus connection string secret
resource serviceBusConnectionSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: 'ServiceBusConnection'
  parent: keyVault
  properties: {
    value: serviceBusConnectionString
    contentType: 'Service Bus connection string'
    attributes: {
      enabled: true
    }
  }
}

// Application Insights connection string secret
resource applicationInsightsConnectionSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: 'ApplicationInsightsConnection'
  parent: keyVault
  properties: {
    value: applicationInsightsConnectionString
    contentType: 'Application Insights connection string'
    attributes: {
      enabled: true
    }
  }
}

// Output secret references (for use in app settings)
@description('Key Vault reference for DefaultConnection')
output defaultConnectionReference string = '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=${defaultConnectionSecret.name})'

@description('Key Vault reference for EventStoreConnection')
output eventStoreConnectionReference string = '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=${eventStoreConnectionSecret.name})'

@description('Key Vault reference for CosmosDbConnection')
output cosmosDbConnectionReference string = '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=${cosmosDbConnectionSecret.name})'

@description('Key Vault reference for ServiceBusConnection')
output serviceBusConnectionReference string = '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=${serviceBusConnectionSecret.name})'

@description('Key Vault reference for ApplicationInsightsConnection')
output applicationInsightsConnectionReference string = '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=${applicationInsightsConnectionSecret.name})'
