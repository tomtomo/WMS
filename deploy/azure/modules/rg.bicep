// Kelola seluruh resource environment dalam satu resource group agar dapat dihapus bersama.
targetScope = 'subscription'

param rgName string
param location string

resource rg 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: rgName
  location: location
}

output rgName string = rg.name
