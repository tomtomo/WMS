// Container registry: pull hanya via Managed Identity
@minLength(3)
param baseName string
param location string
param uniqueSuffix string

resource acr 'Microsoft.ContainerRegistry/registries@2023-11-01-preview' = {
  name: 'acr${replace(baseName, '-', '')}${uniqueSuffix}'
  location: location
  sku: { name: 'Basic' }
  properties: {
    adminUserEnabled: false
  }
}

output acrId string = acr.id
output acrName string = acr.name
output loginServer string = acr.properties.loginServer
