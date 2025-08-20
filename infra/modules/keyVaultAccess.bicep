// Key Vault Access module for RBAC role assignments
// This module assigns necessary permissions for the managed identity to access Key Vault

@description('The name of the Key Vault')
param keyVaultName string

@description('The name of the managed identity')
param managedIdentityName string

// Well-known role definition IDs for Key Vault
var keyVaultSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'

// Reference existing resources
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: managedIdentityName
}

// Role assignment for Key Vault Secrets User
resource keyVaultSecretsUserAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, managedIdentity.id, keyVaultSecretsUserRoleId)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRoleId)
    principalId: managedIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    description: 'Allows the managed identity to read secrets from Key Vault'
  }
}

// Output values
@description('The resource ID of the Key Vault Secrets User role assignment')
output keyVaultSecretsUserAssignmentId string = keyVaultSecretsUserAssignment.id

@description('The principal ID of the managed identity')
output managedIdentityPrincipalId string = managedIdentity.properties.principalId

@description('The client ID of the managed identity')
output managedIdentityClientId string = managedIdentity.properties.clientId
