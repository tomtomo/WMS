// PostgreSQL Flexible Server: Gunakan satu PostgreSQL server dengan database terpisah untuk setiap modul.
param baseName string
param location string
param uniqueSuffix string
@secure()
param administratorPassword string

var databases = [
  'wms_inbound'
  'wms_inventory'
  'wms_outbound'
  'wms_masterdata'
  'wms_auth'
  'wms_reporting'
  'wms_notifications'
]

resource server 'Microsoft.DBforPostgreSQL/flexibleServers@2024-08-01' = {
  name: 'pg-${baseName}-${uniqueSuffix}'
  location: location
  sku: {
    name: 'Standard_B1ms'
    tier: 'Burstable'
  }
  properties: {
    version: '16'
    administratorLogin: 'wmsadmin'
    administratorLoginPassword: administratorPassword
    storage: { storageSizeGB: 32 }
    backup: { backupRetentionDays: 7, geoRedundantBackup: 'Disabled' }
    highAvailability: { mode: 'Disabled' }
  }
}

resource dbs 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2024-08-01' = [
  for db in databases: {
    parent: server
    name: db
  }
]

// Izinkan koneksi dari layanan Azure untuk kebutuhan environment sandbox.
resource allowAzure 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2024-08-01' = {
  parent: server
  name: 'AllowAllAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

output serverFqdn string = server.properties.fullyQualifiedDomainName
output serverName string = server.name
