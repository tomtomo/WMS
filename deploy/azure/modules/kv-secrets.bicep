// Simpan connection string dan key resource di Key Vault agar aplikasi membacanya melalui Managed Identity.
param vaultName string
param serviceBusNamespaceName string
param redisHostName string
param redisName string
param communicationName string
param eventGridTopicName string
param pgServerFqdn string
@secure()
param pgAdminPassword string

var moduleDatabases = [
  { module: 'inbound', db: 'wms_inbound' }
  { module: 'inventory', db: 'wms_inventory' }
  { module: 'outbound', db: 'wms_outbound' }
  { module: 'masterdata', db: 'wms_masterdata' }
  { module: 'auth', db: 'wms_auth' }
  { module: 'reporting', db: 'wms_reporting' }
  { module: 'notifications', db: 'wms_notifications' }
]

resource vault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: vaultName
}

resource sbNamespace 'Microsoft.ServiceBus/namespaces@2024-01-01' existing = {
  name: serviceBusNamespaceName
}

resource redisDb 'Microsoft.Cache/redisEnterprise/databases@2024-10-01' existing = {
  name: '${redisName}/default'
}

resource communication 'Microsoft.Communication/communicationServices@2023-06-01-preview' existing = {
  name: communicationName
}

resource egTopic 'Microsoft.EventGrid/topics@2024-06-01-preview' existing = {
  name: eventGridTopicName
}

resource pgConnSecrets 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = [
  for entry in moduleDatabases: {
    parent: vault
    name: 'conn-wms-${entry.module}'
    properties: {
      // Koneksi PostgreSQL menggunakan SSL dan divalidasi lebih lanjut oleh adapter.
      value: 'Host=${pgServerFqdn};Database=${entry.db};Username=wmsadmin;Password=${pgAdminPassword};Ssl Mode=Require'
    }
  }
]

resource sbConnSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: vault
  name: 'sb-connection'
  properties: {
    value: listKeys('${sbNamespace.id}/AuthorizationRules/RootManageSharedAccessKey', '2024-01-01').primaryConnectionString
  }
}

resource redisConnSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: vault
  name: 'redis-connection'
  properties: {
    value: '${redisHostName}:10000,password=${redisDb.listKeys().primaryKey},ssl=True,abortConnect=False'
  }
}

resource acsConnSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: vault
  name: 'acs-connection'
  properties: {
    value: communication.listKeys().primaryConnectionString
  }
}

resource egKeySecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: vault
  name: 'eg-topic-key'
  properties: {
    value: egTopic.listKeys().key1
  }
}

output secretNames array = [for entry in moduleDatabases: 'conn-wms-${entry.module}']
