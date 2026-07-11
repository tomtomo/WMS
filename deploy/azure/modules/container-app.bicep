// Container App menyediakan endpoint REST dan gRPC terpisah, autoscaling, health check, dan secret dari Key Vault.
param name string
param location string
param envId string
param image string
param appsIdentityId string
param acrLoginServer string
param vaultUri string
param envVars array
param secretNames array

// Ekspos port gRPC hanya saat diperlukan, dengan nomor port yang unik dalam environment.
param grpcExposedPort int = 0
param minReplicas int = 0

// Nama subscription Service Bus milik modul (kosong = tanpa scale rule Service Bus).
param serviceBusSubscription string = ''

var kvSecrets = [
  for secretName in secretNames: {
    name: secretName
    keyVaultUrl: '${vaultUri}secrets/${secretName}'
    identity: appsIdentityId
  }
]

var serviceBusRules = serviceBusSubscription == '' ? [] : [
  {
    name: 'servicebus-backlog'
    custom: {
      type: 'azure-servicebus'
      metadata: {
        topicName: 'wms-core-flow'
        subscriptionName: serviceBusSubscription
        messageCount: '20'
      }
      auth: [
        { secretRef: 'sb-connection', triggerParameter: 'connection' }
      ]
    }
  }
]

resource app 'Microsoft.App/containerApps@2024-10-02-preview' = {
  name: name
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: { '${appsIdentityId}': {} }
  }
  properties: {
    environmentId: envId
    workloadProfileName: 'Consumption'
    configuration: {
      // external di env internal = terekspos di ILB VNet, bukan internet.
      ingress: {
        external: true
        targetPort: 8080
        transport: 'http'
        additionalPortMappings: grpcExposedPort > 0 ? [
          { external: true, targetPort: 8081, exposedPort: grpcExposedPort }
        ] : []
      }
      registries: [
        { server: acrLoginServer, identity: appsIdentityId }
      ]
      secrets: kvSecrets
    }
    template: {
      containers: [
        {
          name: name
          image: image
          resources: { cpu: json('0.5'), memory: '1Gi' }
          env: envVars
          probes: [
            {
              type: 'Liveness'
              httpGet: { path: '/alive', port: 8080 }
              initialDelaySeconds: 10
              periodSeconds: 15
            }
            {
              type: 'Readiness'
              httpGet: { path: '/health', port: 8080 }
              initialDelaySeconds: 5
              periodSeconds: 10
            }
          ]
        }
      ]
      scale: {
        minReplicas: minReplicas
        maxReplicas: 5
        rules: concat(serviceBusRules, [
          {
            name: 'http-concurrency'
            http: { metadata: { concurrentRequests: '50' } }
          }
        ])
      }
    }
  }
}

output fqdn string = app.properties.configuration.ingress.fqdn
output appName string = app.name
