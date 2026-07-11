// Cosmos DB menyimpan projection dan change feed, dengan akses hanya melalui Managed Identity.
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

resource projections 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-11-15' = {
  parent: database
  name: 'projections'
  properties: {
    resource: {
      id: 'projections'
      partitionKey: { paths: ['/partitionKey'], kind: 'Hash' }
    }
  }
}

resource leases 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-11-15' = {
  parent: database
  name: 'projections-leases'
  properties: {
    resource: {
      id: 'projections-leases'
      partitionKey: { paths: ['/id'], kind: 'Hash' }
    }
  }
}

// Berikan aplikasi akses baca dan tulis ke data Cosmos DB.
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
