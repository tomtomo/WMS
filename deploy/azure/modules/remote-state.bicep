// Simpan state deployment dan lock-nya di Blob Storage dengan versioning aktif.
param baseName string
param location string

resource stateStorage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: 'st${replace(baseName, '-', '')}state${uniqueString(resourceGroup().id)}'
  location: location
  sku: { name: 'Standard_LRS' }
  kind: 'StorageV2'
  properties: {
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: stateStorage
  name: 'default'
  properties: {
    isVersioningEnabled: true
  }
}

resource stateContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: 'deploy-state'
}

output storageName string = stateStorage.name
output containerName string = stateContainer.name
