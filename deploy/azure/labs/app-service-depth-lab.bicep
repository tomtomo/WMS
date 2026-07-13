// Lab terpisah untuk test slot staging, autoscale, dan diagnostics pada App Service S1 tanpa mendeploy seluruh stack WMS.
targetScope = 'resourceGroup'

param location string = resourceGroup().location
param baseName string = 'wmslab'

// Gunakan image ASP.NET publik sebagai aplikasi sederhana untuk pengujian slot swap dan logging.
var image = 'mcr.microsoft.com/dotnet/samples:aspnetapp'

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: 'law-${baseName}'
  location: location
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: 30
  }
}

resource plan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: 'plan-${baseName}'
  location: location
  kind: 'linux'
  sku: { name: 'S1', tier: 'Standard' }
  properties: {
    reserved: true
  }
}

resource site 'Microsoft.Web/sites@2024-04-01' = {
  name: 'app-${baseName}-${uniqueString(resourceGroup().id)}'
  location: location
  kind: 'app,linux,container'
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOCKER|${image}'
      alwaysOn: true
      appSettings: [
        { name: 'WEBSITES_PORT', value: '8080' }
        { name: 'Deployment__Slot', value: 'production' }
      ]
    }
  }
}

resource stagingSlot 'Microsoft.Web/sites/slots@2024-04-01' = {
  parent: site
  name: 'staging'
  location: location
  kind: 'app,linux,container'
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOCKER|${image}'
      alwaysOn: true
      appSettings: [
        { name: 'WEBSITES_PORT', value: '8080' }
        { name: 'Deployment__Slot', value: 'staging' }
      ]
    }
  }
}

resource slotConfigNames 'Microsoft.Web/sites/config@2024-04-01' = {
  parent: site
  name: 'slotConfigNames'
  properties: {
    appSettingNames: [ 'Deployment__Slot' ]
  }
}

resource autoscale 'Microsoft.Insights/autoscalesettings@2022-10-01' = {
  name: 'autoscale-${baseName}'
  location: location
  properties: {
    targetResourceUri: plan.id
    enabled: true
    profiles: [
      {
        name: 'cpu-scale-out'
        capacity: { minimum: '1', maximum: '2', default: '1' }
        rules: [
          {
            metricTrigger: {
              metricName: 'CpuPercentage'
              metricResourceUri: plan.id
              timeGrain: 'PT1M'
              statistic: 'Average'
              timeWindow: 'PT5M'
              timeAggregation: 'Average'
              operator: 'GreaterThan'
              threshold: 70
            }
            scaleAction: { direction: 'Increase', type: 'ChangeCount', value: '1', cooldown: 'PT5M' }
          }
          {
            metricTrigger: {
              metricName: 'CpuPercentage'
              metricResourceUri: plan.id
              timeGrain: 'PT1M'
              statistic: 'Average'
              timeWindow: 'PT5M'
              timeAggregation: 'Average'
              operator: 'LessThan'
              threshold: 30
            }
            scaleAction: { direction: 'Decrease', type: 'ChangeCount', value: '1', cooldown: 'PT5M' }
          }
        ]
      }
    ]
  }
}

resource siteLogs 'Microsoft.Web/sites/config@2024-04-01' = {
  parent: site
  name: 'logs'
  properties: {
    httpLogs: {
      fileSystem: { enabled: true, retentionInMb: 35, retentionInDays: 3 }
    }
    detailedErrorMessages: { enabled: true }
  }
}

resource diagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'diag-${baseName}'
  scope: site
  properties: {
    workspaceId: logAnalytics.id
    logs: [
      { category: 'AppServiceHTTPLogs', enabled: true }
      { category: 'AppServiceConsoleLogs', enabled: true }
    ]
    metrics: [
      { category: 'AllMetrics', enabled: true }
    ]
  }
}

output siteName string = site.name
output siteUrl string = 'https://${site.properties.defaultHostName}'
output stagingUrl string = 'https://${stagingSlot.properties.defaultHostName}'
