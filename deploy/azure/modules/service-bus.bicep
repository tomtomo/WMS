// Siapkan topic dan subscription per modul dengan session ordering dan batas lima kali pengiriman.
param baseName string
param location string
param uniqueSuffix string

// Gunakan logical name sebagai nama rule karena subscription ARM tidak membuat filter default secara otomatis.
var subscriptions = [
  {
    name: 'wms.reporting'
    logicalNames: [
      'inbound.gr_confirmed.v1'
      'inventory.stock_removed.v1'
      'inventory.putaway_completed.v1'
      'outbound.picking_completed.v1'
    ]
  }
  {
    name: 'wms.inventory'
    logicalNames: [
      'inbound.gr_confirmed.v1'
      'outbound.wave_released.v1'
      'outbound.picking_completed.v1'
      'outbound.shipment_dispatched.v1'
    ]
  }
  {
    name: 'wms.outbound'
    logicalNames: [
      'inventory.stock_allocation_completed.v1'
    ]
  }
]

resource namespace 'Microsoft.ServiceBus/namespaces@2024-01-01' = {
  name: 'sb-${baseName}-${uniqueSuffix}'
  location: location
  sku: { name: 'Standard', tier: 'Standard' }
}

resource coreFlowTopic 'Microsoft.ServiceBus/namespaces/topics@2024-01-01' = {
  parent: namespace
  name: 'wms-core-flow'
  properties: {
    supportOrdering: true
  }
}

resource delayedTasksQueue 'Microsoft.ServiceBus/namespaces/queues@2024-01-01' = {
  parent: namespace
  name: 'wms-delayed-tasks'
}

resource railSubscriptions 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2024-01-01' = [
  for sub in subscriptions: {
    parent: coreFlowTopic
    name: sub.name
    properties: {
      requiresSession: true
      maxDeliveryCount: 5
    }
  }
]

var subscriptionRules = flatten(map(range(0, length(subscriptions)), subIndex => map(
  range(0, length(subscriptions[subIndex].logicalNames)), ruleIndex => {
    subIndex: subIndex
    ruleName: subscriptions[subIndex].logicalNames[ruleIndex]
    logicalName: subscriptions[subIndex].logicalNames[ruleIndex]
  })))

resource rules 'Microsoft.ServiceBus/namespaces/topics/subscriptions/rules@2024-01-01' = [
  for rule in subscriptionRules: {
    parent: railSubscriptions[rule.subIndex]
    name: rule.ruleName
    properties: {
      filterType: 'CorrelationFilter'
      correlationFilter: {
        label: rule.logicalName
      }
    }
  }
]

output namespaceName string = namespace.name
output namespaceId string = namespace.id
