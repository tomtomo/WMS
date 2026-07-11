// APIM Standard v2 sebagai edge gateway: APIM memvalidasi JWT, membatasi request, dan meneruskan trafik ke ACA melalui jaringan internal.
param baseName string
param location string
param uniqueSuffix string
param apimSubnetId string
param acaDefaultDomain string
param jwtModulus string
param jwtExponent string
param publisherEmail string

var jwtIssuer = 'wms-azure'
var jwtAudience = 'wms-azure'

// auth = anonymous (login/refresh), modul lain wajib JWT
var apis = [
  { name: 'auth', requiresJwt: false }
  { name: 'inbound', requiresJwt: true }
  { name: 'inventory', requiresJwt: true }
  { name: 'outbound', requiresJwt: true }
  { name: 'masterdata', requiresJwt: true }
  { name: 'reporting', requiresJwt: true }
  { name: 'notifications', requiresJwt: true }
]

var httpMethods = ['GET', 'POST', 'PUT', 'PATCH', 'DELETE']

resource apim 'Microsoft.ApiManagement/service@2024-06-01-preview' = {
  name: 'apim-${baseName}-${uniqueSuffix}'
  location: location
  sku: { name: 'StandardV2', capacity: 1 }
  identity: { type: 'SystemAssigned' }
  properties: {
    publisherEmail: publisherEmail
    publisherName: 'WMS Sandbox'
    virtualNetworkType: 'External'
    virtualNetworkConfiguration: {
      subnetResourceId: apimSubnetId
    }
  }
}

// Nonaktifkan validasi sertifikat karena backend ACA masih menggunakan default domain internal.
resource backends 'Microsoft.ApiManagement/service/backends@2024-06-01-preview' = [
  for api in apis: {
    parent: apim
    name: 'wms-${api.name}'
    properties: {
      protocol: 'http'
      url: 'https://wms-${api.name}.${acaDefaultDomain}'
      tls: {
        validateCertificateChain: false
        validateCertificateName: false
      }
    }
  }
]

resource apiResources 'Microsoft.ApiManagement/service/apis@2024-06-01-preview' = [
  for api in apis: {
    parent: apim
    name: 'wms-${api.name}'
    properties: {
      displayName: 'WMS ${api.name}'
      path: api.name
      protocols: ['https']
      subscriptionRequired: false
      serviceUrl: 'https://wms-${api.name}.${acaDefaultDomain}'
    }
  }
]

// Teruskan semua metode dan subpath API ke backend terkait.
resource operations 'Microsoft.ApiManagement/service/apis/operations@2024-06-01-preview' = [
  for pair in flatten(map(range(0, length(apis)), apiIndex => map(httpMethods, method => {
    apiIndex: apiIndex
    method: method
  }))): {
    parent: apiResources[pair.apiIndex]
    name: 'wildcard-${toLower(pair.method)}'
    properties: {
      displayName: '${pair.method} /*'
      method: pair.method
      urlTemplate: '/*'
    }
  }
]

resource apiPolicies 'Microsoft.ApiManagement/service/apis/policies@2024-06-01-preview' = [
  for (api, index) in apis: {
    parent: apiResources[index]
    name: 'policy'
    properties: {
      format: 'xml'
      value: api.requiresJwt
        ? '<policies><inbound><base /><rate-limit-by-key calls="120" renewal-period="60" counter-key="@(context.Request.IpAddress)" /><validate-jwt header-name="Authorization" require-scheme="Bearer" failed-validation-httpcode="401" failed-validation-error-message="Token tidak valid."><issuer-signing-keys><key n="${jwtModulus}" e="${jwtExponent}" /></issuer-signing-keys><audiences><audience>${jwtAudience}</audience></audiences><issuers><issuer>${jwtIssuer}</issuer></issuers></validate-jwt><set-backend-service backend-id="wms-${api.name}" /></inbound><backend><base /></backend><outbound><base /></outbound><on-error><base /></on-error></policies>'
        : '<policies><inbound><base /><rate-limit-by-key calls="30" renewal-period="60" counter-key="@(context.Request.IpAddress)" /><set-backend-service backend-id="wms-${api.name}" /></inbound><backend><base /></backend><outbound><base /></outbound><on-error><base /></on-error></policies>'
    }
    dependsOn: [backends[index]]
  }
]

output gatewayUrl string = apim.properties.gatewayUrl
output apimName string = apim.name
