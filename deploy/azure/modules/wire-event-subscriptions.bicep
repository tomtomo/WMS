// Buat subscription Event Grid setelah kode Functions selesai di deploy.
param notificationTopicName string
param blobEventsTopicName string
param notificationsFunctionAppId string
param scheduledFunctionAppId string

resource notificationTopic 'Microsoft.EventGrid/topics@2024-06-01-preview' existing = {
  name: notificationTopicName
}

resource blobEventsTopic 'Microsoft.EventGrid/systemTopics@2024-06-01-preview' existing = {
  name: blobEventsTopicName
}

// Kirim seluruh event notifikasi ke function NotificationsRail.
resource notificationsSubscription 'Microsoft.EventGrid/topics/eventSubscriptions@2024-06-01-preview' = {
  parent: notificationTopic
  name: 'wms-notifications'
  properties: {
    eventDeliverySchema: 'CloudEventSchemaV1_0'
    destination: {
      endpointType: 'AzureFunction'
      properties: {
        resourceId: '${notificationsFunctionAppId}/functions/NotificationsRail'
        maxEventsPerBatch: 1
      }
    }
    retryPolicy: {
      maxDeliveryAttempts: 5
      eventTimeToLiveInMinutes: 1440
    }
  }
}

// Proses file baru di container file-drop melalui function FileIngestionDropped.
resource blobSubscription 'Microsoft.EventGrid/systemTopics/eventSubscriptions@2024-06-01-preview' = {
  parent: blobEventsTopic
  name: 'wms-file-ingestion'
  properties: {
    eventDeliverySchema: 'CloudEventSchemaV1_0'
    filter: {
      includedEventTypes: ['Microsoft.Storage.BlobCreated']
      subjectBeginsWith: '/blobServices/default/containers/file-drop/'
    }
    destination: {
      endpointType: 'AzureFunction'
      properties: {
        resourceId: '${scheduledFunctionAppId}/functions/FileIngestionDropped'
        maxEventsPerBatch: 1
      }
    }
  }
}
