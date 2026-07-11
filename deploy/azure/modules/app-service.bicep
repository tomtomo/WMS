// WebUI selalu aktif, menggunakan ARR affinity untuk sesi Blazor Server, dan menarik image ACR lewat Managed Identity.
param baseName string
param location string
param uniqueSuffix string
param image string
param acrLoginServer string
param appsIdentityId string
param appsIdentityClientId string
param gatewayAddress string
param appInsightsConnectionString string

resource plan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: 'plan-${baseName}-webui'
  location: location
  kind: 'linux'
  sku: { name: 'B1', tier: 'Basic' }
  properties: {
    reserved: true
  }
}

resource site 'Microsoft.Web/sites@2024-04-01' = {
  name: 'app-${baseName}-webui-${uniqueSuffix}'
  location: location
  kind: 'app,linux,container'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: { '${appsIdentityId}': {} }
  }
  properties: {
    serverFarmId: plan.id
    clientAffinityEnabled: true
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOCKER|${image}'
      alwaysOn: true
      acrUseManagedIdentityCreds: true
      acrUserManagedIdentityID: appsIdentityClientId
      appSettings: [
        { name: 'Bff__GatewayAddress', value: gatewayAddress }
        { name: 'ConnectionStrings__appinsights', value: appInsightsConnectionString }
        { name: 'WEBSITES_PORT', value: '8080' }
        { name: 'DOCKER_REGISTRY_SERVER_URL', value: 'https://${acrLoginServer}' }
      ]
    }
  }
}

output webUiUrl string = 'https://${site.properties.defaultHostName}'
output siteName string = site.name
