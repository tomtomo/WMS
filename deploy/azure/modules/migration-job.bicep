// Jalankan migrasi dan seed database melalui ACA Job sebelum aplikasi dimulai.
param baseName string
param location string
param envId string
param image string
param appsIdentityId string
param acrLoginServer string
param vaultUri string

var moduleConnections = [
  { env: 'ConnectionStrings__inbounddb', secret: 'conn-wms-inbound' }
  { env: 'ConnectionStrings__inventorydb', secret: 'conn-wms-inventory' }
  { env: 'ConnectionStrings__outbounddb', secret: 'conn-wms-outbound' }
  { env: 'ConnectionStrings__masterdatadb', secret: 'conn-wms-masterdata' }
  { env: 'ConnectionStrings__authdb', secret: 'conn-wms-auth' }
  { env: 'ConnectionStrings__reportingdb', secret: 'conn-wms-reporting' }
  { env: 'ConnectionStrings__notificationsdb', secret: 'conn-wms-notifications' }
]

resource job 'Microsoft.App/jobs@2024-10-02-preview' = {
  name: 'job-${baseName}-migrations'
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: { '${appsIdentityId}': {} }
  }
  properties: {
    environmentId: envId
    workloadProfileName: 'Consumption'
    configuration: {
      triggerType: 'Manual'
      replicaTimeout: 1800
      replicaRetryLimit: 1
      manualTriggerConfig: { parallelism: 1, replicaCompletionCount: 1 }
      registries: [
        { server: acrLoginServer, identity: appsIdentityId }
      ]
      secrets: [
        for entry in moduleConnections: {
          name: entry.secret
          keyVaultUrl: '${vaultUri}secrets/${entry.secret}'
          identity: appsIdentityId
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'migrations'
          image: image
          resources: { cpu: json('0.5'), memory: '1Gi' }
          env: [
            for entry in moduleConnections: {
              name: entry.env
              secretRef: entry.secret
            }
          ]
        }
      ]
    }
  }
}

output jobName string = job.name
