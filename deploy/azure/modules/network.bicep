// Gunakan subnet terpisah untuk ACA, Functions, dan APIM sesuai kebutuhan delegasi masing-masing.
param baseName string
param location string

resource apimNsg 'Microsoft.Network/networkSecurityGroups@2024-05-01' = {
  name: 'nsg-${baseName}-apim'
  location: location
  properties: {
    securityRules: [
      {
        // Dependensi wajib APIM v2 VNet integration: Storage + Key Vault.
        name: 'allow-storage'
        properties: {
          direction: 'Outbound'
          access: 'Allow'
          priority: 100
          protocol: 'Tcp'
          sourceAddressPrefix: 'VirtualNetwork'
          sourcePortRange: '*'
          destinationAddressPrefix: 'Storage'
          destinationPortRange: '443'
        }
      }
      {
        name: 'allow-keyvault'
        properties: {
          direction: 'Outbound'
          access: 'Allow'
          priority: 110
          protocol: 'Tcp'
          sourceAddressPrefix: 'VirtualNetwork'
          sourcePortRange: '*'
          destinationAddressPrefix: 'AzureKeyVault'
          destinationPortRange: '443'
        }
      }
    ]
  }
}

resource vnet 'Microsoft.Network/virtualNetworks@2024-05-01' = {
  name: 'vnet-${baseName}'
  location: location
  properties: {
    addressSpace: { addressPrefixes: ['10.60.0.0/16'] }
    subnets: [
      {
        name: 'snet-aca-infra'
        properties: {
          addressPrefix: '10.60.0.0/23'
          delegations: [
            { name: 'aca', properties: { serviceName: 'Microsoft.App/environments' } }
          ]
        }
      }
      {
        name: 'snet-functions'
        properties: {
          addressPrefix: '10.60.2.0/26'
          delegations: [
            { name: 'flex', properties: { serviceName: 'Microsoft.App/environments' } }
          ]
        }
      }
      {
        name: 'snet-apim'
        properties: {
          addressPrefix: '10.60.3.0/27'
          networkSecurityGroup: { id: apimNsg.id }
          delegations: [
            { name: 'apim', properties: { serviceName: 'Microsoft.Web/serverFarms' } }
          ]
        }
      }
    ]
  }
}

output vnetId string = vnet.id
output acaInfraSubnetId string = vnet.properties.subnets[0].id
output functionsSubnetId string = vnet.properties.subnets[1].id
output apimSubnetId string = vnet.properties.subnets[2].id
