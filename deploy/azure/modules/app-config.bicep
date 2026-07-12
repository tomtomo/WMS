// Sediakan Azure App Configuration untuk feature flag terpusat dan beri MI akses tanpa access key.
param baseName string
param location string
param uniqueSuffix string
param appsIdentityPrincipalId string

// Gunakan SKU Free untuk sandbox, ganti ke Standard jika kuota store gratis sudah terpakai.
// Akses hanya melalui Managed Identity dan RBAC, tanpa access key.
resource appConfig 'Microsoft.AppConfiguration/configurationStores@2024-05-01' = {
  name: 'appcs-${baseName}-${uniqueSuffix}'
  location: location
  sku: { name: 'free' }
  properties: {
    // MI-only: matikan akses berbasis access-key, data-plane lewat RBAC saja.
    disableLocalAuth: true
  }
}

// Aktifkan feature flag Reporting.TelemetrySummary secara default untuk mengontrol endpoint ringkasan telemetry.
// Nama resource memakai ~2F sebagai pengganti karakter '/' pada key App Configuration.
resource telemetrySummaryFlag 'Microsoft.AppConfiguration/configurationStores/keyValues@2024-05-01' = {
  parent: appConfig
  name: '.appconfig.featureflag~2FReporting.TelemetrySummary'
  properties: {
    contentType: 'application/vnd.microsoft.appconfig.ff+json;charset=utf-8'
    value: '{"id":"Reporting.TelemetrySummary","description":"Gate endpoint ringkasan telemetry operasional.","enabled":true,"conditions":{"client_filters":[]}}'
  }
}

// Berikan MI akses read ke konfigurasi dan feature flag di App Configuration.
var dataReaderRoleId = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  '516239f1-63e1-4d78-a4de-a74fb236a071')

resource dataReader 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(appConfig.id, appsIdentityPrincipalId, dataReaderRoleId)
  scope: appConfig
  properties: {
    roleDefinitionId: dataReaderRoleId
    principalId: appsIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

output endpoint string = appConfig.properties.endpoint
output storeName string = appConfig.name
