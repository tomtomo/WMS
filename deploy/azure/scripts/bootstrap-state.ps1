# Siapkan resource penyimpanan state deployment dan blob lock untuk satu subscription.
param(
    [string]$StateRgName = 'rg-wms-state',
    [string]$Location = 'southeastasia'
)

$ErrorActionPreference = 'Stop'

az group create --name $StateRgName --location $Location --output none

$deployment = az deployment group create `
    --resource-group $StateRgName `
    --template-file "$PSScriptRoot/../modules/remote-state.bicep" `
    --parameters baseName=wms location=$Location `
    --query 'properties.outputs' -o json | ConvertFrom-Json

Write-Host "State storage : $($deployment.storageName.value)"
Write-Host "State container: $($deployment.containerName.value)"
Write-Host ''
Write-Host 'Set variable repo GitHub:'
Write-Host "  gh variable set AZURE_STATE_RG --body $StateRgName"
Write-Host "  gh variable set AZURE_STATE_ACCOUNT --body $($deployment.storageName.value)"
