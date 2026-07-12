// Cosmos DB hot store telemetry operasional. Serverless, akses hanya Managed Identity.
param baseName string
param location string
param uniqueSuffix string
param appsIdentityPrincipalId string

resource account 'Microsoft.DocumentDB/databaseAccounts@2024-11-15' = {
  name: 'cos-${baseName}-${uniqueSuffix}'
  location: location
  kind: 'GlobalDocumentDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    consistencyPolicy: { defaultConsistencyLevel: 'Session' }
    locations: [
      { locationName: location, failoverPriority: 0, isZoneRedundant: false }
    ]
    capabilities: [
      { name: 'EnableServerless' }
    ]
    disableLocalAuth: true
  }
}

resource database 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-11-15' = {
  parent: account
  name: 'wms'
  properties: {
    resource: { id: 'wms' }
  }
}

resource telemetry 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-11-15' = {
  parent: database
  name: 'operational-telemetry'
  properties: {
    resource: {
      id: 'operational-telemetry'
      partitionKey: { paths: ['/warehouseId'], kind: 'Hash' }
      // TTL 7 hari: hot store
      defaultTtl: 604800
    }
  }
}

resource dataContributor 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2024-11-15' = {
  parent: account
  name: guid(account.id, appsIdentityPrincipalId, 'data-contributor')
  properties: {
    roleDefinitionId: '${account.id}/sqlRoleDefinitions/00000000-0000-0000-0000-000000000002'
    principalId: appsIdentityPrincipalId
    scope: account.id
  }
}

output accountEndpoint string = account.properties.documentEndpoint
output accountName string = account.name
