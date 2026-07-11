// Event Grid: Sediakan topic notifikasi dan event BlobCreated, subscription Functions dipasang setelah deployment kode.
param baseName string
param location string
param uniqueSuffix string
param storageId string

resource notificationTopic 'Microsoft.EventGrid/topics@2024-06-01-preview' = {
  name: 'egt-${baseName}-notifications-${uniqueSuffix}'
  location: location
  properties: {
    inputSchema: 'CloudEventSchemaV1_0'
    publicNetworkAccess: 'Enabled'
  }
}

resource blobEventsTopic 'Microsoft.EventGrid/systemTopics@2024-06-01-preview' = {
  name: 'egst-${baseName}-blob'
  location: location
  properties: {
    source: storageId
    topicType: 'Microsoft.Storage.StorageAccounts'
  }
}

output notificationTopicName string = notificationTopic.name
output notificationTopicEndpoint string = notificationTopic.properties.endpoint
output notificationTopicId string = notificationTopic.id
output blobEventsTopicName string = blobEventsTopic.name
