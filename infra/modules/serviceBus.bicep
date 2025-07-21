// modules/serviceBus.bicep
// Azure Service Bus for domain event messaging
// Duration: Service Bus module creation started

@description('Name of the Service Bus namespace')
param serviceBusNamespaceName string

@description('Location for Service Bus')
param location string

@description('Tags to apply to Service Bus')
param tags object = {}

@description('SKU name for Service Bus')
@allowed(['Basic', 'Standard', 'Premium'])
param skuName string = 'Standard'

@description('SKU tier for Service Bus')
@allowed(['Basic', 'Standard', 'Premium'])
param skuTier string = 'Standard'

@description('Name of the topic for domain events')
param topicName string = 'domain-events'

@description('Name of the subscription')
param subscriptionName string = 'zeus-people-subscription'

@description('Managed identity principal ID for RBAC')
param managedIdentityPrincipalId string

@description('Log Analytics workspace ID for diagnostics')
param logAnalyticsWorkspaceId string

@description('Enable zone redundancy (Premium only)')
param enableZoneRedundant bool = false

@description('Minimum TLS version')
@allowed(['1.0', '1.1', '1.2'])
param minimumTlsVersion string = '1.2'

// Service Bus Namespace
resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2024-01-01' = {
  name: serviceBusNamespaceName
  location: location
  tags: tags
  sku: {
    name: skuName
    tier: skuTier
    capacity: skuName == 'Premium' ? 1 : null
  }
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    minimumTlsVersion: minimumTlsVersion
    publicNetworkAccess: 'Enabled'
    disableLocalAuth: false
    zoneRedundant: skuName == 'Premium' ? enableZoneRedundant : false
    premiumMessagingPartitions: skuName == 'Premium' ? 1 : null
  }
}

// Service Bus Topic for domain events
resource serviceBusTopic 'Microsoft.ServiceBus/namespaces/topics@2024-01-01' = {
  parent: serviceBusNamespace
  name: topicName
  properties: {
    maxSizeInMegabytes: skuName == 'Premium' ? 81920 : 5120
    requiresDuplicateDetection: true
    duplicateDetectionHistoryTimeWindow: 'PT10M'
    enableBatchedOperations: true
    supportOrdering: true
    autoDeleteOnIdle: 'P14D'
    enablePartitioning: skuName != 'Premium'
    enableExpress: false
    maxMessageSizeInKilobytes: skuName == 'Premium' ? 100000 : 256
  }
}

// Service Bus Subscription
resource serviceBusSubscription 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2024-01-01' = {
  parent: serviceBusTopic
  name: subscriptionName
  properties: {
    requiresSession: false
    enableBatchedOperations: true
    lockDuration: 'PT1M'
    defaultMessageTimeToLive: 'P14D'
    deadLetteringOnFilterEvaluationExceptions: true
    deadLetteringOnMessageExpiration: true
    maxDeliveryCount: 3
    autoDeleteOnIdle: 'P14D'
  }
}

// Dead letter subscription for failed messages
resource deadLetterSubscription 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2024-01-01' = {
  parent: serviceBusTopic
  name: '${subscriptionName}-deadletter'
  properties: {
    requiresSession: false
    enableBatchedOperations: true
    lockDuration: 'PT5M'
    defaultMessageTimeToLive: 'P30D'
    deadLetteringOnFilterEvaluationExceptions: false
    deadLetteringOnMessageExpiration: false
    maxDeliveryCount: 1
    autoDeleteOnIdle: 'P30D'
  }
}

// Authorization rule for the namespace
resource serviceBusAuthRule 'Microsoft.ServiceBus/namespaces/authorizationRules@2024-01-01' = {
  parent: serviceBusNamespace
  name: 'zeus-people-auth-rule'
  properties: {
    rights: ['Send', 'Listen', 'Manage']
  }
}

// RBAC assignments for the managed identity
// Service Bus Data Sender role
resource serviceBusDataSenderRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(serviceBusNamespace.id, managedIdentityPrincipalId, '69a216fc-b8fb-44d8-bc22-1f3c2cd27a39')
  scope: serviceBusNamespace
  properties: {
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      '69a216fc-b8fb-44d8-bc22-1f3c2cd27a39'
    ) // Service Bus Data Sender
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Service Bus Data Receiver role
resource serviceBusDataReceiverRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(serviceBusNamespace.id, managedIdentityPrincipalId, '4f6d3b9b-027b-4f4c-9142-0e5a2a2247e0')
  scope: serviceBusNamespace
  properties: {
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      '4f6d3b9b-027b-4f4c-9142-0e5a2a2247e0'
    ) // Service Bus Data Receiver
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Diagnostic settings
resource serviceBusDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: '${serviceBusNamespaceName}-diagnostics'
  scope: serviceBusNamespace
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
@description('The resource ID of the Service Bus namespace')
output serviceBusNamespaceId string = serviceBusNamespace.id

@description('The name of the Service Bus namespace')
output serviceBusNamespaceName string = serviceBusNamespace.name

@description('The FQDN of the Service Bus namespace')
output serviceBusNamespaceFqdn string = '${serviceBusNamespace.name}.servicebus.windows.net'

@description('The Service Bus endpoint')
output serviceBusEndpoint string = serviceBusNamespace.properties.serviceBusEndpoint

@description('The primary connection string (secure)')
output primaryConnectionString string = serviceBusAuthRule.listKeys().primaryConnectionString

@description('The topic name')
output topicName string = serviceBusTopic.name

@description('The subscription name')
output subscriptionName string = serviceBusSubscription.name

// Duration: Service Bus module completed
