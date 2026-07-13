// WebUI selalu aktif, ARR affinity untuk sesi Blazor Server, tarik image ACR lewat Managed Identity.
// Depth: SKU B1 default
param baseName string
param location string
param uniqueSuffix string
param image string
param acrLoginServer string
param appsIdentityId string
param appsIdentityClientId string
param gatewayAddress string
param appInsightsConnectionString string
param logAnalyticsId string
param vaultUri string
param webUiSku string = 'B1'
param entraTenantId string = ''
param entraClientId string = ''

// B1 dipakai sebagai default. Slot staging dan autoscale hanya tersedia untuk SKU Standard atau Premium.
var skuTier = startsWith(webUiSku, 'S') ? 'Standard' : startsWith(webUiSku, 'P') ? 'PremiumV3' : 'Basic'
var supportsSlots = startsWith(webUiSku, 'S') || startsWith(webUiSku, 'P')

// Tambahkan konfigurasi Entra jika app registration sudah disiapkan agar Key Vault reference tidak menggantung.
var entraSettings = empty(entraClientId) ? [] : [
  { name: 'Entra__TenantId', value: entraTenantId }
  { name: 'Entra__ClientId', value: entraClientId }
  { name: 'Entra__ClientSecret', value: '@Microsoft.KeyVault(SecretUri=${vaultUri}secrets/entra-client-secret/)' }
]

var baseAppSettings = concat([
  { name: 'Bff__GatewayAddress', value: gatewayAddress }
  { name: 'ConnectionStrings__appinsights', value: appInsightsConnectionString }
  { name: 'WEBSITES_PORT', value: '8080' }
  { name: 'DOCKER_REGISTRY_SERVER_URL', value: 'https://${acrLoginServer}' }
], entraSettings)

resource plan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: 'plan-${baseName}-webui'
  location: location
  kind: 'linux'
  sku: { name: webUiSku, tier: skuTier }
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
    keyVaultReferenceIdentity: appsIdentityId
    siteConfig: {
      linuxFxVersion: 'DOCKER|${image}'
      alwaysOn: true
      acrUseManagedIdentityCreds: true
      acrUserManagedIdentityID: appsIdentityClientId
      appSettings: concat(baseAppSettings, [
        { name: 'Deployment__Slot', value: 'production' }
      ])
    }
  }
}

// Slot staging untuk warm up dan smoke test sebelum swap tanpa downtime.
resource stagingSlot 'Microsoft.Web/sites/slots@2024-04-01' = if (supportsSlots) {
  parent: site
  name: 'staging'
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
    keyVaultReferenceIdentity: appsIdentityId
    siteConfig: {
      linuxFxVersion: 'DOCKER|${image}'
      alwaysOn: true
      acrUseManagedIdentityCreds: true
      acrUserManagedIdentityID: appsIdentityClientId
      appSettings: concat(baseAppSettings, [
        { name: 'Deployment__Slot', value: 'staging' }
      ])
    }
  }
}

// Pertahankan App Insights dan penanda deployment di slot masing-masing saat proses swap.
resource slotConfigNames 'Microsoft.Web/sites/config@2024-04-01' = if (supportsSlots) {
  parent: site
  name: 'slotConfigNames'
  properties: {
    appSettingNames: [ 'ConnectionStrings__appinsights', 'Deployment__Slot' ]
  }
}

// Autoscale untuk SKU S1 ke atas: tambah instance saat CPU di atas 70% dan kurangi saat di bawah 30%.
resource autoscale 'Microsoft.Insights/autoscalesettings@2022-10-01' = if (supportsSlots) {
  name: 'autoscale-${baseName}-webui'
  location: location
  properties: {
    targetResourceUri: plan.id
    enabled: true
    profiles: [
      {
        name: 'cpu-scale-out'
        capacity: { minimum: '1', maximum: '2', default: '1' }
        rules: [
          {
            metricTrigger: {
              metricName: 'CpuPercentage'
              metricResourceUri: plan.id
              timeGrain: 'PT1M'
              statistic: 'Average'
              timeWindow: 'PT5M'
              timeAggregation: 'Average'
              operator: 'GreaterThan'
              threshold: 70
            }
            scaleAction: { direction: 'Increase', type: 'ChangeCount', value: '1', cooldown: 'PT5M' }
          }
          {
            metricTrigger: {
              metricName: 'CpuPercentage'
              metricResourceUri: plan.id
              timeGrain: 'PT1M'
              statistic: 'Average'
              timeWindow: 'PT5M'
              timeAggregation: 'Average'
              operator: 'LessThan'
              threshold: 30
            }
            scaleAction: { direction: 'Decrease', type: 'ChangeCount', value: '1', cooldown: 'PT5M' }
          }
        ]
      }
    ]
  }
}

// Aktifkan HTTP logging, detail error, dan failed request tracing dengan retensi log selama 3 hari.
resource siteLogs 'Microsoft.Web/sites/config@2024-04-01' = {
  parent: site
  name: 'logs'
  properties: {
    httpLogs: {
      fileSystem: { enabled: true, retentionInMb: 35, retentionInDays: 3 }
    }
    detailedErrorMessages: { enabled: true }
    failedRequestsTracing: { enabled: true }
  }
}

// Kirim HTTP logs, console logs, dan seluruh metrik App Service ke Log Analytics.
resource diagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'diag-${baseName}-webui'
  scope: site
  properties: {
    workspaceId: logAnalyticsId
    logs: [
      { category: 'AppServiceHTTPLogs', enabled: true }
      { category: 'AppServiceConsoleLogs', enabled: true }
    ]
    metrics: [
      { category: 'AllMetrics', enabled: true }
    ]
  }
}

output webUiUrl string = 'https://${site.properties.defaultHostName}'
output siteName string = site.name
output stagingSlotUrl string = supportsSlots ? 'https://${stagingSlot!.properties.defaultHostName}' : ''
