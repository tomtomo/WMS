// Sediakan Blob Storage untuk lampiran GR, paket deployment, dan kebutuhan runtime Functions.
@minLength(3)
param baseName string
param location string
param uniqueSuffix string

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: 'st${replace(baseName, '-', '')}${uniqueSuffix}'
  location: location
  sku: { name: 'Standard_LRS' }
  kind: 'StorageV2'
  properties: {
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    supportsHttpsTrafficOnly: true
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: storage
  name: 'default'
}

resource attachments 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: 'gr-attachments'
}

// Sediakan container untuk file masuk yang akan diproses melalui event BlobCreated.
resource fileDrop 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: 'file-drop'
}

resource deployContainers 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = [
  for app in ['reporting', 'notifications', 'scheduled']: {
    parent: blobService
    name: 'deploy-${app}'
  }
]

output storageId string = storage.id
output storageName string = storage.name
output blobEndpoint string = storage.properties.primaryEndpoints.blob
