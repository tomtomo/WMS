// Availability test
param baseName string
param location string
param appInsightsId string
param webUiUrl string
param alertEmail string

// Standard ping test ke /health WebUI
resource healthWebTest 'Microsoft.Insights/webtests@2022-06-15' = {
  name: 'webtest-${baseName}-webui-health'
  location: location
  tags: {
    'hidden-link:${appInsightsId}': 'Resource'
  }
  kind: 'standard'
  properties: {
    SyntheticMonitorId: 'webtest-${baseName}-webui-health'
    Name: 'WebUI health'
    Enabled: true
    Frequency: 300
    Timeout: 30
    Kind: 'standard'
    RetryEnabled: true
    Locations: [
      { Id: 'apac-sg-sin-azr' }
      { Id: 'emea-nl-ams-azr' }
      { Id: 'us-ca-sjc-azr' }
    ]
    Request: {
      RequestUrl: '${webUiUrl}/health'
      HttpVerb: 'GET'
    }
    ValidationRules: {
      ExpectedHttpStatusCode: 200
      SSLCheck: true
      SSLCertRemainingLifetimeCheck: 7
    }
  }
}

// Action group email untuk notifikasi alert.
resource actionGroup 'Microsoft.Insights/actionGroups@2023-01-01' = {
  name: 'ag-${baseName}-availability'
  location: 'global'
  properties: {
    groupShortName: 'wms-avail'
    enabled: true
    emailReceivers: [
      {
        name: 'ops-email'
        emailAddress: alertEmail
        useCommonAlertSchema: true
      }
    ]
  }
}

// Alert saat availability < 100%
resource availabilityAlert 'Microsoft.Insights/metricAlerts@2018-03-01' = {
  name: 'alert-${baseName}-webui-availability'
  location: 'global'
  properties: {
    severity: 1
    enabled: true
    scopes: [appInsightsId]
    evaluationFrequency: 'PT1M'
    windowSize: 'PT5M'
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
      allOf: [
        {
          name: 'availability'
          metricNamespace: 'microsoft.insights/components'
          metricName: 'availabilityResults/availabilityPercentage'
          operator: 'LessThan'
          threshold: 100
          timeAggregation: 'Average'
          criterionType: 'StaticThresholdCriterion'
        }
      ]
    }
    autoMitigate: true
    actions: [
      {
        actionGroupId: actionGroup.id
      }
    ]
  }
  dependsOn: [healthWebTest]
}
