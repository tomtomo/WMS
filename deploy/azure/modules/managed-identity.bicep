// Pisahkan identity runtime dan deployment agar aplikasi tidak memiliki akses create secret.
param baseName string
param location string

resource appsIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'mi-${baseName}-apps'
  location: location
}

resource deployIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'mi-${baseName}-deploy'
  location: location
}

output appsIdentityId string = appsIdentity.id
output appsIdentityPrincipalId string = appsIdentity.properties.principalId
output appsIdentityClientId string = appsIdentity.properties.clientId
output deployIdentityId string = deployIdentity.id
output deployIdentityPrincipalId string = deployIdentity.properties.principalId
