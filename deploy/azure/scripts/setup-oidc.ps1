# Siapkan autentikasi OIDC GitHub Actions ke Azure tanpa client secret.
param(
    [Parameter(Mandatory = $true)][string]$GitHubRepo,   # mis. tomtomo/WMS
    [string]$AppName = 'github-wms-deploy'
)

$ErrorActionPreference = 'Stop'

$subscriptionId = az account show --query id -o tsv
$tenantId = az account show --query tenantId -o tsv

$appId = az ad app list --display-name $AppName --query '[0].appId' -o tsv
if (-not $appId) {
    $appId = az ad app create --display-name $AppName --query appId -o tsv
    az ad sp create --id $appId --output none
}

$subjects = @(
    "repo:${GitHubRepo}:ref:refs/heads/main",
    "repo:${GitHubRepo}:environment:azure"
)
foreach ($subject in $subjects) {
    $name = ($subject -replace '[^a-zA-Z0-9]', '-').Trim('-')
    $existing = az ad app federated-credential list --id $appId --query "[?subject=='$subject'] | length(@)" -o tsv
    if ($existing -eq '0') {
        $body = @{ name = $name; issuer = 'https://token.actions.githubusercontent.com'; subject = $subject; audiences = @('api://AzureADTokenExchange') } | ConvertTo-Json -Compress
        az ad app federated-credential create --id $appId --parameters $body --output none
    }
}

# Gunakan role Owner karena deployment perlu membuat role assignment.
az role assignment create --assignee $appId --role 'Owner' --scope "/subscriptions/$subscriptionId" --output none 2>$null

Write-Host 'Set variable repo GitHub:'
Write-Host "  gh variable set AZURE_CLIENT_ID --body $appId"
Write-Host "  gh variable set AZURE_TENANT_ID --body $tenantId"
Write-Host "  gh variable set AZURE_SUBSCRIPTION_ID --body $subscriptionId"
