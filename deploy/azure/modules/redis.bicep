// Gunakan Azure Managed Redis dengan satu endpoint yang kompatibel dengan StackExchange.Redis.
param baseName string
param location string
param uniqueSuffix string

resource redis 'Microsoft.Cache/redisEnterprise@2024-10-01' = {
  name: 'redis-${baseName}-${uniqueSuffix}'
  location: location
  sku: { name: 'Balanced_B0' }
}

resource database 'Microsoft.Cache/redisEnterprise/databases@2024-10-01' = {
  parent: redis
  name: 'default'
  properties: {
    clientProtocol: 'Encrypted'
    port: 10000
    clusteringPolicy: 'EnterpriseCluster'
    evictionPolicy: 'VolatileLRU'
  }
}

output hostName string = redis.properties.hostName
output databaseName string = database.name
output redisName string = redis.name
