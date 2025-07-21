// Managed Identity module for Zeus.People API
// This module deploys a user-assigned managed identity for secure resource access

@description('The name of the managed identity')
param managedIdentityName string

@description('Location for the resources')
param location string = resourceGroup().location

@description('Tags to apply to all resources')
param tags object = {}

// User-assigned managed identity
resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: managedIdentityName
  location: location
  tags: tags
}

// Output values
@description('The resource ID of the managed identity')
output managedIdentityId string = managedIdentity.id

@description('The name of the managed identity')
output managedIdentityName string = managedIdentity.name

@description('The principal ID of the managed identity')
output principalId string = managedIdentity.properties.principalId

@description('The client ID of the managed identity')
output clientId string = managedIdentity.properties.clientId
