// Event Hubs untuk stream telemetry operasional. Akses lewat Managed Identity.
param baseName string
param location string
param uniqueSuffix string
param appsIdentityPrincipalId string

resource namespace 'Microsoft.EventHub/namespaces@2024-01-01' = {
  name: 'evhns-${baseName}-${uniqueSuffix}'
  location: location
  sku: {
    name: 'Standard'
    tier: 'Standard'
    capacity: 1
  }
  properties: {
    // Auto-inflate dimatikan karena 1 throughput unit sudah cukup untuk sandbox dan mencegah biaya tak terduga.
    isAutoInflateEnabled: false
    disableLocalAuth: true
  }
}

resource hub 'Microsoft.EventHub/namespaces/eventhubs@2024-01-01' = {
  parent: namespace
  name: 'wms-operational-telemetry'
  properties: {
    // Telemetry operasional cukup disimpan selama 1 hari dengan 2 partisi untuk kebutuhan sandbox.
    partitionCount: 2
    messageRetentionInDays: 1
  }
}

resource consumerGroup 'Microsoft.EventHub/namespaces/eventhubs/consumergroups@2024-01-01' = {
  parent: hub
  name: 'reporting-functions'
}

// Berikan MI akses untuk mengirim dan menerima event di Event Hubs tanpa access key.
var dataSenderRoleId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '2b629674-e913-4c01-ae53-ef4638d8f975')
var dataReceiverRoleId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'a638d3c7-ab3a-418d-83e6-5f17a39d4fde')

resource dataSender 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(namespace.id, appsIdentityPrincipalId, dataSenderRoleId)
  scope: namespace
  properties: {
    roleDefinitionId: dataSenderRoleId
    principalId: appsIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

resource dataReceiver 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(namespace.id, appsIdentityPrincipalId, dataReceiverRoleId)
  scope: namespace
  properties: {
    roleDefinitionId: dataReceiverRoleId
    principalId: appsIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

output namespaceName string = namespace.name
output fullyQualifiedNamespace string = '${namespace.name}.servicebus.windows.net'
