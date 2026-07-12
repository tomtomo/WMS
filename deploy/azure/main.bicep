// Provisioning dilakukan bertahap agar deployment infra, aplikasi, dan Event Grid mengikuti dependency runtime.
// fase 1. deployApps=false, infra inti (ACR belum berisi image);
// fase 2. deployApps=true, compute (image sudah di push `az acr build`);
// fase 3. wireEventSubscriptions=true, event subscription EG (function code sudah ter deploy).
targetScope = 'subscription'

param rgName string = 'rg-wms'
param location string = 'southeastasia'
param baseName string = 'wms'
param imageTag string = 'latest'
param publisherEmail string
param deployApps bool = false
param wireEventSubscriptions bool = false

var uniqueSuffix = uniqueString(subscription().subscriptionId, rgName)
var jwtIssuer = 'wms-azure'

module rg 'modules/rg.bicep' = {
  name: 'rg'
  params: { rgName: rgName, location: location }
}

module telemetry 'modules/telemetry.bicep' = {
  name: 'telemetry'
  scope: resourceGroup(rgName)
  dependsOn: [rg]
  params: { baseName: baseName, location: location }
}

module network 'modules/network.bicep' = {
  name: 'network'
  scope: resourceGroup(rgName)
  dependsOn: [rg]
  params: { baseName: baseName, location: location }
}

module identity 'modules/managed-identity.bicep' = {
  name: 'identity'
  scope: resourceGroup(rgName)
  dependsOn: [rg]
  params: { baseName: baseName, location: location }
}

module acr 'modules/acr.bicep' = {
  name: 'acr'
  scope: resourceGroup(rgName)
  dependsOn: [rg]
  params: { baseName: baseName, location: location, uniqueSuffix: uniqueSuffix }
}

module keyVault 'modules/key-vault.bicep' = {
  name: 'key-vault'
  scope: resourceGroup(rgName)
  dependsOn: [rg]
  params: {
    baseName: baseName
    location: location
    uniqueSuffix: uniqueSuffix
    appsIdentityPrincipalId: identity.outputs.appsIdentityPrincipalId
    deployIdentityPrincipalId: identity.outputs.deployIdentityPrincipalId
  }
}

module secrets 'modules/secrets-generate.bicep' = {
  name: 'secrets-generate'
  scope: resourceGroup(rgName)
  params: {
    baseName: baseName
    location: location
    vaultName: keyVault.outputs.vaultName
    deployIdentityId: identity.outputs.deployIdentityId
  }
}

module storage 'modules/storage.bicep' = {
  name: 'storage'
  scope: resourceGroup(rgName)
  dependsOn: [rg]
  params: { baseName: baseName, location: location, uniqueSuffix: uniqueSuffix }
}

resource vaultRef 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVault.outputs.vaultName
  scope: resourceGroup(rgName)
}

module postgres 'modules/flexible-server.bicep' = {
  name: 'flexible-server'
  scope: resourceGroup(rgName)
  dependsOn: [secrets]
  params: {
    baseName: baseName
    location: location
    uniqueSuffix: uniqueSuffix
    administratorPassword: vaultRef.getSecret('pg-admin-password')
  }
}

module serviceBus 'modules/service-bus.bicep' = {
  name: 'service-bus'
  scope: resourceGroup(rgName)
  dependsOn: [rg]
  params: { baseName: baseName, location: location, uniqueSuffix: uniqueSuffix }
}

// Event Hubs (stream telemetry) dan Cosmos (hot store telemetry), keduanya identity-based, RBAC ke mi-apps.
module eventHubs 'modules/event-hubs.bicep' = {
  name: 'event-hubs'
  scope: resourceGroup(rgName)
  dependsOn: [rg]
  params: {
    baseName: baseName
    location: location
    uniqueSuffix: uniqueSuffix
    appsIdentityPrincipalId: identity.outputs.appsIdentityPrincipalId
  }
}

module cosmos 'modules/cosmos.bicep' = {
  name: 'cosmos'
  scope: resourceGroup(rgName)
  dependsOn: [rg]
  params: {
    baseName: baseName
    location: location
    uniqueSuffix: uniqueSuffix
    appsIdentityPrincipalId: identity.outputs.appsIdentityPrincipalId
  }
}

module eventGrid 'modules/event-grid.bicep' = {
  name: 'event-grid'
  scope: resourceGroup(rgName)
  params: {
    baseName: baseName
    location: location
    uniqueSuffix: uniqueSuffix
    storageId: storage.outputs.storageId
  }
}

module redis 'modules/redis.bicep' = {
  name: 'redis'
  scope: resourceGroup(rgName)
  dependsOn: [rg]
  params: { baseName: baseName, location: location, uniqueSuffix: uniqueSuffix }
}

module acs 'modules/acs.bicep' = {
  name: 'acs'
  scope: resourceGroup(rgName)
  dependsOn: [rg]
  params: { baseName: baseName, uniqueSuffix: uniqueSuffix }
}

module kvSecrets 'modules/kv-secrets.bicep' = {
  name: 'kv-secrets'
  scope: resourceGroup(rgName)
  params: {
    vaultName: keyVault.outputs.vaultName
    serviceBusNamespaceName: serviceBus.outputs.namespaceName
    redisHostName: redis.outputs.hostName
    redisName: redis.outputs.redisName
    communicationName: acs.outputs.communicationName
    eventGridTopicName: eventGrid.outputs.notificationTopicName
    pgServerFqdn: postgres.outputs.serverFqdn
    pgAdminPassword: vaultRef.getSecret('pg-admin-password')
  }
}

module roles 'modules/role-assignments.bicep' = {
  name: 'role-assignments'
  scope: resourceGroup(rgName)
  params: {
    acrName: acr.outputs.acrName
    storageName: storage.outputs.storageName
    appsIdentityPrincipalId: identity.outputs.appsIdentityPrincipalId
  }
}

module acaEnv 'modules/container-app-env.bicep' = {
  name: 'container-app-env'
  scope: resourceGroup(rgName)
  params: {
    baseName: baseName
    location: location
    infraSubnetId: network.outputs.acaInfraSubnetId
    logAnalyticsId: telemetry.outputs.logAnalyticsId
  }
}

module privateDns 'modules/private-dns.bicep' = {
  name: 'private-dns'
  scope: resourceGroup(rgName)
  params: {
    defaultDomain: acaEnv.outputs.defaultDomain
    staticIp: acaEnv.outputs.staticIp
    vnetId: network.outputs.vnetId
  }
}

module apim 'modules/apim.bicep' = {
  name: 'apim'
  scope: resourceGroup(rgName)
  params: {
    baseName: baseName
    location: location
    uniqueSuffix: uniqueSuffix
    apimSubnetId: network.outputs.apimSubnetId
    acaDefaultDomain: acaEnv.outputs.defaultDomain
    jwtModulus: secrets.outputs.jwtModulus
    jwtExponent: secrets.outputs.jwtExponent
    publisherEmail: publisherEmail
  }
}

// ---- Fase 2: compute yang butuh image / code ----

// Gunakan port gRPC yang berbeda untuk setiap Container App dalam environment internal.
var masterDataGrpc = 'http://wms-masterdata.${acaEnv.outputs.defaultDomain}:8081'
var authGrpc = 'http://wms-auth.${acaEnv.outputs.defaultDomain}:8082'

var acaSharedEnv = [
  { name: 'ConnectionStrings__servicebus', secretRef: 'sb-connection' }
  { name: 'ConnectionStrings__redis', secretRef: 'redis-connection' }
  { name: 'ConnectionStrings__acs', secretRef: 'acs-connection' }
  { name: 'ConnectionStrings__appinsights', value: telemetry.outputs.appInsightsConnectionString }
  { name: 'AzurePlatform__Messaging__EventGridTopicEndpoint', value: eventGrid.outputs.notificationTopicEndpoint }
  { name: 'AzurePlatform__Messaging__EventGridTopicKey', secretRef: 'eg-topic-key' }
  { name: 'AzurePlatform__Secrets__VaultUri', value: keyVault.outputs.vaultUri }
  { name: 'AzurePlatform__ObjectStore__AccountUrl', value: storage.outputs.blobEndpoint }
  // Telemetry: emitter publish ke Event Hubs, thin Reporting host membaca Cosmos (endpoint dan MI).
  { name: 'AzurePlatform__Messaging__EventHubsFullyQualifiedNamespace', value: eventHubs.outputs.fullyQualifiedNamespace }
  { name: 'AzurePlatform__Persistence__Cosmos__AccountEndpoint', value: cosmos.outputs.accountEndpoint }
  { name: 'AzurePlatform__Notifications__Acs__SenderAddress', value: acs.outputs.senderAddress }
  { name: 'Jwt__Issuer', value: jwtIssuer }
  { name: 'Jwt__Audience', value: jwtIssuer }
  { name: 'Jwt__PublicKeyPem', value: secrets.outputs.jwtPublicPem }
  { name: 'AZURE_CLIENT_ID', value: identity.outputs.appsIdentityClientId }
]

var sharedSecretNames = ['sb-connection', 'redis-connection', 'acs-connection', 'eg-topic-key']

// Lookup host gRPC selalu memiliki minimal satu replika dan menggunakan port unik.
var coreApps = [
  { name: 'wms-inbound', module: 'inbound', sub: '', min: 0, needsMasterData: true, grpcPort: 0 }
  { name: 'wms-inventory', module: 'inventory', sub: 'wms.inventory', min: 0, needsMasterData: false, grpcPort: 0 }
  { name: 'wms-outbound', module: 'outbound', sub: 'wms.outbound', min: 0, needsMasterData: true, grpcPort: 0 }
  { name: 'wms-masterdata', module: 'masterdata', sub: '', min: 1, needsMasterData: false, grpcPort: 8081 }
  { name: 'wms-auth', module: 'auth', sub: '', min: 1, needsMasterData: false, grpcPort: 8082 }

  // Thin REST host: read API Reporting/inbox Notifications — consumer tetap di Functions.
  { name: 'wms-reporting', module: 'reporting', sub: '', min: 0, needsMasterData: false, grpcPort: 0 }
  { name: 'wms-notifications', module: 'notifications', sub: '', min: 0, needsMasterData: false, grpcPort: 0 }
]

module containerApps 'modules/container-app.bicep' = [
  for app in coreApps: if (deployApps) {
    name: 'ca-${app.name}'
    scope: resourceGroup(rgName)
    dependsOn: [kvSecrets, roles, privateDns]
    params: {
      name: app.name
      location: location
      envId: acaEnv.outputs.envId
      image: '${acr.outputs.loginServer}/${app.name}:${imageTag}'
      appsIdentityId: identity.outputs.appsIdentityId
      acrLoginServer: acr.outputs.loginServer
      vaultUri: keyVault.outputs.vaultUri
      envVars: concat(acaSharedEnv, [
        { name: 'ConnectionStrings__wms', secretRef: 'conn-wms-${app.module}' }
        { name: 'AzurePlatform__Telemetry__ServiceName', value: app.name }
      ], app.needsMasterData ? [
        { name: 'Services__MasterData__Grpc', value: masterDataGrpc }
      ] : [], app.name != 'wms-auth' ? [
        // Checker user aktif lintas host ke AuthLookup.
        { name: 'Services__Auth__Grpc', value: authGrpc }
      ] : [], app.module == 'inventory' ? [
        // Gunakan ID hasil seed Master Data sebagai konfigurasi default proses receiving.
        { name: 'Inventory__Receiving__ReceivingLocationId', value: 'b0000000-0000-0000-0000-000000000001' }
        { name: 'Inventory__Receiving__QuarantineLocationId', value: 'b0000000-0000-0000-0000-000000000003' }
        { name: 'Inventory__Receiving__PutawayDestinationId', value: 'b0000000-0000-0000-0000-000000000002' }
        { name: 'Inventory__Receiving__PutawayAssignee', value: 'c0000000-0000-0000-0000-000000000001' }
      ] : [], app.module == 'outbound' ? [
        { name: 'Outbound__Picking__DefaultPickerId', value: 'c0000000-0000-0000-0000-000000000001' }
      ] : [])
      secretNames: concat(['conn-wms-${app.module}'], sharedSecretNames)
      grpcExposedPort: app.grpcPort
      minReplicas: app.min
      serviceBusSubscription: app.sub
    }
  }
]

module migrationJob 'modules/migration-job.bicep' = if (deployApps) {
  name: 'migration-job'
  scope: resourceGroup(rgName)
  dependsOn: [kvSecrets, roles]
  params: {
    baseName: baseName
    location: location
    envId: acaEnv.outputs.envId
    image: '${acr.outputs.loginServer}/wms-migrations:${imageTag}'
    appsIdentityId: identity.outputs.appsIdentityId
    acrLoginServer: acr.outputs.loginServer
    vaultUri: keyVault.outputs.vaultUri
  }
}

// App setting Functions: secret via Key Vault reference, sisanya biasa.
var functionsSharedSettings = [
  { name: 'ConnectionStrings__servicebus', value: '@Microsoft.KeyVault(SecretUri=${keyVault.outputs.vaultUri}secrets/sb-connection/)' }
  { name: 'ServiceBusConnection', value: '@Microsoft.KeyVault(SecretUri=${keyVault.outputs.vaultUri}secrets/sb-connection/)' }
  { name: 'ConnectionStrings__redis', value: '@Microsoft.KeyVault(SecretUri=${keyVault.outputs.vaultUri}secrets/redis-connection/)' }
  { name: 'ConnectionStrings__acs', value: '@Microsoft.KeyVault(SecretUri=${keyVault.outputs.vaultUri}secrets/acs-connection/)' }
  { name: 'ConnectionStrings__appinsights', value: telemetry.outputs.appInsightsConnectionString }
  { name: 'AzurePlatform__Messaging__EventGridTopicEndpoint', value: eventGrid.outputs.notificationTopicEndpoint }
  { name: 'AzurePlatform__Messaging__EventGridTopicKey', value: '@Microsoft.KeyVault(SecretUri=${keyVault.outputs.vaultUri}secrets/eg-topic-key/)' }
  { name: 'AzurePlatform__Secrets__VaultUri', value: keyVault.outputs.vaultUri }
  { name: 'AzurePlatform__ObjectStore__AccountUrl', value: storage.outputs.blobEndpoint }
  { name: 'AzurePlatform__Persistence__Cosmos__AccountEndpoint', value: cosmos.outputs.accountEndpoint }
  // Identity-based connection "EventHubs" untuk EventHubTrigger Reporting (fullyQualifiedNamespace, managedidentity, clientId).
  { name: 'EventHubs__fullyQualifiedNamespace', value: eventHubs.outputs.fullyQualifiedNamespace }
  { name: 'EventHubs__credential', value: 'managedidentity' }
  { name: 'EventHubs__clientId', value: identity.outputs.appsIdentityClientId }
  { name: 'AzurePlatform__Notifications__Acs__SenderAddress', value: acs.outputs.senderAddress }
]

// Definisikan konfigurasi tiap Function App untuk deployment, database, telemetry, dan scheduled trigger.
var functionApps = [
  { name: 'func-${baseName}-reporting-${uniqueSuffix}', container: 'deploy-reporting', connModule: 'reporting', serviceName: 'func-wms-reporting', needsAuthGrpc: false, cron: '' }
  { name: 'func-${baseName}-notifications-${uniqueSuffix}', container: 'deploy-notifications', connModule: 'notifications', serviceName: 'func-wms-notifications', needsAuthGrpc: true, cron: '' }
  { name: 'func-${baseName}-scheduled-${uniqueSuffix}', container: 'deploy-scheduled', connModule: 'inventory', serviceName: 'wms-scheduled', needsAuthGrpc: false, cron: '0 0 2 * * *' }
]

// Deploy Function App satu per satu untuk menghindari konflik lease pada subnet VNet integration.
@batchSize(1)
module functions 'modules/functions.bicep' = [
  for app in functionApps: if (deployApps) {
    name: 'func-${app.container}'
    scope: resourceGroup(rgName)
    dependsOn: [kvSecrets, roles, privateDns]
    params: {
      name: app.name
      location: location
      functionsSubnetId: network.outputs.functionsSubnetId
      storageName: storage.outputs.storageName
      deploymentContainer: app.container
      appsIdentityId: identity.outputs.appsIdentityId
      appsIdentityClientId: identity.outputs.appsIdentityClientId
      appInsightsConnectionString: telemetry.outputs.appInsightsConnectionString
      extraAppSettings: concat(functionsSharedSettings, [
        { name: 'ConnectionStrings__wms', value: '@Microsoft.KeyVault(SecretUri=${keyVault.outputs.vaultUri}secrets/conn-wms-${app.connModule}/)' }
        { name: 'AzurePlatform__Telemetry__ServiceName', value: app.serviceName }
      ], app.needsAuthGrpc ? [
        { name: 'Services__Auth__Grpc', value: authGrpc }
      ] : [], app.cron == '' ? [] : [
        { name: 'Inventory__Expiry__Cron', value: app.cron }
      ])
    }
  }
]

module webUi 'modules/app-service.bicep' = if (deployApps) {
  name: 'app-service'
  scope: resourceGroup(rgName)
  dependsOn: [roles]
  params: {
    baseName: baseName
    location: location
    uniqueSuffix: uniqueSuffix
    image: '${acr.outputs.loginServer}/wms-webui:${imageTag}'
    acrLoginServer: acr.outputs.loginServer
    appsIdentityId: identity.outputs.appsIdentityId
    appsIdentityClientId: identity.outputs.appsIdentityClientId
    gatewayAddress: apim.outputs.gatewayUrl
    appInsightsConnectionString: telemetry.outputs.appInsightsConnectionString
  }
}

// ---- Fase 3: event subscription setelah function code terdeploy ----

module wireEvents 'modules/wire-event-subscriptions.bicep' = if (wireEventSubscriptions) {
  name: 'wire-event-subscriptions'
  scope: resourceGroup(rgName)
  params: {
    notificationTopicName: eventGrid.outputs.notificationTopicName
    blobEventsTopicName: eventGrid.outputs.blobEventsTopicName
    notificationsFunctionAppId: functions[1]!.outputs.appId
    scheduledFunctionAppId: functions[2]!.outputs.appId
  }
}

output acrName string = acr.outputs.acrName
output acrLoginServer string = acr.outputs.loginServer
output keyVaultName string = keyVault.outputs.vaultName
output acaDefaultDomain string = acaEnv.outputs.defaultDomain
output apimGatewayUrl string = apim.outputs.gatewayUrl
output migrationJobName string = deployApps ? migrationJob!.outputs.jobName : ''
output webUiUrl string = deployApps ? webUi!.outputs.webUiUrl : ''
output functionAppNames array = map(functionApps, app => app.name)
output coreAppFqdns array = [for (app, index) in coreApps: deployApps ? containerApps[index]!.outputs.fqdn : '']
