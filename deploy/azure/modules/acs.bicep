// Azure Communication Services Email: gunakan managed domain bawaan Azure agar email dapat dikirim tanpa konfigurasi DNS tambahan.
param baseName string
param uniqueSuffix string

param dataLocation string = 'United States'

resource emailService 'Microsoft.Communication/emailServices@2023-06-01-preview' = {
  name: 'acsmail-${baseName}-${uniqueSuffix}'
  location: 'global'
  properties: {
    dataLocation: dataLocation
  }
}

resource managedDomain 'Microsoft.Communication/emailServices/domains@2023-06-01-preview' = {
  parent: emailService
  name: 'AzureManagedDomain'
  location: 'global'
  properties: {
    domainManagement: 'AzureManaged'
    userEngagementTracking: 'Disabled'
  }
}

resource communication 'Microsoft.Communication/communicationServices@2023-06-01-preview' = {
  name: 'acs-${baseName}-${uniqueSuffix}'
  location: 'global'
  properties: {
    dataLocation: dataLocation
    linkedDomains: [managedDomain.id]
  }
}

output communicationName string = communication.name
output senderAddress string = 'DoNotReply@${managedDomain.properties.mailFromSenderDomain}'
