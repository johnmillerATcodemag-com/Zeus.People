# Monitoring Configuration Diagnostic Script
param(
    [Parameter(Mandatory = $false)]
    [ValidateSet("staging", "production")]
    [string]$Environment = "staging"
)

$timestamp = Get-Date -Format "yyyy-MM-dd-HHmmss"

# Environment configuration
$envConfig = @{
    "staging"    = @{
        "resourceGroup" = "rg-academic-staging-westus2"
        "appName"       = "app-academic-staging-2ymnmfmrvsb3w"
        "appInsights"   = "ai-academic-staging-2ymnmfmrvsb3w"
        "logAnalytics"  = "law-academic-staging-2ymnmfmrvsb3w"
        "keyVault"      = "kv2ymnmfmrvsb3w"
    }
    "production" = @{
        "resourceGroup" = "rg-academic-production-westus2"
        "appName"       = "app-academic-production"
        "appInsights"   = "ai-academic-production"
        "logAnalytics"  = "law-academic-production"
        "keyVault"      = "kv-academic-production"
    }
}

$config = $envConfig[$Environment]

function Write-DiagLog {
    param([string]$Message, [string]$Level = "INFO")
    
    $timestamp = Get-Date -Format "HH:mm:ss"
    $color = switch ($Level) {
        "SUCCESS" { "Green" }
        "ERROR" { "Red" }
        "WARNING" { "Yellow" }
        "INFO" { "Cyan" }
        default { "White" }
    }
    
    Write-Host "[$timestamp] [$Level] $Message" -ForegroundColor $color
}

Write-DiagLog "=====================================" "INFO"
Write-DiagLog "MONITORING CONFIGURATION DIAGNOSTICS" "INFO"
Write-DiagLog "=====================================" "INFO"
Write-DiagLog "Environment: $Environment" "INFO"

# Check resource group
Write-DiagLog "Checking resource group: $($config.resourceGroup)" "INFO"
$resourceGroup = az group show --name $config.resourceGroup --output json 2>$null | ConvertFrom-Json
if ($resourceGroup) {
    Write-DiagLog "✅ Resource group exists" "SUCCESS"
    Write-DiagLog "  - Location: $($resourceGroup.location)" "INFO"
    Write-DiagLog "  - Provisioning State: $($resourceGroup.properties.provisioningState)" "INFO"
}
else {
    Write-DiagLog "❌ Resource group not found" "ERROR"
    exit 1
}

# Check App Service
Write-DiagLog "Checking App Service: $($config.appName)" "INFO"
$appService = az webapp show --name $config.appName --resource-group $config.resourceGroup --output json 2>$null | ConvertFrom-Json
if ($appService) {
    Write-DiagLog "✅ App Service exists" "SUCCESS"
    Write-DiagLog "  - State: $($appService.state)" "INFO"
    Write-DiagLog "  - Default Host Name: $($appService.defaultHostName)" "INFO"
    Write-DiagLog "  - Location: $($appService.location)" "INFO"
    Write-DiagLog "  - App Service Plan: $($appService.appServicePlanId.Split('/')[-1])" "INFO"
    
    # Check if app is running
    if ($appService.state -eq "Running") {
        Write-DiagLog "✅ App Service is running" "SUCCESS"
    }
    else {
        Write-DiagLog "⚠️ App Service state: $($appService.state)" "WARNING"
    }
    
    # Check app settings for monitoring
    Write-DiagLog "Checking App Service configuration..." "INFO"
    $appSettings = az webapp config appsettings list --name $config.appName --resource-group $config.resourceGroup --output json 2>$null | ConvertFrom-Json
    
    $monitoringSettings = @(
        "APPLICATIONINSIGHTS_CONNECTION_STRING",
        "APPINSIGHTS_INSTRUMENTATIONKEY",
        "ApplicationInsights__ConnectionString",
        "ApplicationInsights__InstrumentationKey"
    )
    
    $foundMonitoringSettings = @()
    foreach ($setting in $monitoringSettings) {
        $found = $appSettings | Where-Object { $_.name -eq $setting }
        if ($found) {
            $foundMonitoringSettings += $setting
            $value = if ($found.value.Length -gt 20) { $found.value.Substring(0, 20) + "..." } else { $found.value }
            Write-DiagLog "  - $($setting): $value" "SUCCESS"
        }
    }
    
    if ($foundMonitoringSettings.Count -gt 0) {
        Write-DiagLog "✅ Found $($foundMonitoringSettings.Count) Application Insights settings" "SUCCESS"
    }
    else {
        Write-DiagLog "⚠️ No Application Insights settings found in App Service" "WARNING"
    }
    
    # Check deployment status
    Write-DiagLog "Checking deployment status..." "INFO"
    $deployments = az webapp deployment list --name $config.appName --resource-group $config.resourceGroup --output json 2>$null | ConvertFrom-Json
    if ($deployments -and $deployments.Count -gt 0) {
        $latestDeployment = $deployments[0]
        Write-DiagLog "✅ Latest deployment found" "SUCCESS"
        Write-DiagLog "  - ID: $($latestDeployment.id)" "INFO"
        Write-DiagLog "  - Status: $($latestDeployment.status)" "INFO"
        Write-DiagLog "  - Start Time: $($latestDeployment.start_time)" "INFO"
        Write-DiagLog "  - End Time: $($latestDeployment.end_time)" "INFO"
    }
    else {
        Write-DiagLog "⚠️ No deployments found" "WARNING"
    }
    
}
else {
    Write-DiagLog "❌ App Service not found" "ERROR"
}

# Check Application Insights
Write-DiagLog "Checking Application Insights: $($config.appInsights)" "INFO"
$appInsights = az monitor app-insights component show --app $config.appInsights --resource-group $config.resourceGroup --output json 2>$null | ConvertFrom-Json
if ($appInsights) {
    Write-DiagLog "✅ Application Insights exists" "SUCCESS"
    Write-DiagLog "  - Instrumentation Key: $($appInsights.instrumentationKey.Substring(0,8))..." "INFO"
    Write-DiagLog "  - Connection String: Available" "SUCCESS"
    Write-DiagLog "  - Application Type: $($appInsights.applicationType)" "INFO"
    Write-DiagLog "  - Provisioning State: $($appInsights.provisioningState)" "INFO"
    
    # Test Application Insights connectivity
    Write-DiagLog "Testing Application Insights data ingestion..." "INFO"
    $testQuery = "requests | take 1"
    $testResult = az monitor app-insights query --app $config.appInsights --analytics-query $testQuery --output json 2>$null | ConvertFrom-Json
    if ($testResult) {
        Write-DiagLog "✅ Application Insights query interface accessible" "SUCCESS"
    }
    else {
        Write-DiagLog "⚠️ Application Insights query interface may not be ready" "WARNING"
    }
}
else {
    Write-DiagLog "❌ Application Insights not found" "ERROR"
    
    # Check if it exists with a different name pattern
    Write-DiagLog "Searching for Application Insights resources..." "INFO"
    $allAppInsights = az monitor app-insights component list --resource-group $config.resourceGroup --output json 2>$null | ConvertFrom-Json
    if ($allAppInsights -and $allAppInsights.Count -gt 0) {
        Write-DiagLog "Found $($allAppInsights.Count) Application Insights resources:" "INFO"
        foreach ($ai in $allAppInsights) {
            Write-DiagLog "  - $($ai.name)" "INFO"
        }
    }
}

# Check Log Analytics Workspace
Write-DiagLog "Checking Log Analytics: $($config.logAnalytics)" "INFO"
$logAnalytics = az monitor log-analytics workspace show --workspace-name $config.logAnalytics --resource-group $config.resourceGroup --output json 2>$null | ConvertFrom-Json
if ($logAnalytics) {
    Write-DiagLog "✅ Log Analytics Workspace exists" "SUCCESS"
    Write-DiagLog "  - Customer ID: $($logAnalytics.customerId.Substring(0,8))..." "INFO"
    Write-DiagLog "  - Retention Days: $($logAnalytics.retentionInDays)" "INFO"
    Write-DiagLog "  - Provisioning State: $($logAnalytics.provisioningState)" "INFO"
}
else {
    Write-DiagLog "❌ Log Analytics Workspace not found" "ERROR"
    
    # Search for Log Analytics workspaces
    Write-DiagLog "Searching for Log Analytics workspaces..." "INFO"
    $allWorkspaces = az monitor log-analytics workspace list --resource-group $config.resourceGroup --output json 2>$null | ConvertFrom-Json
    if ($allWorkspaces -and $allWorkspaces.Count -gt 0) {
        Write-DiagLog "Found $($allWorkspaces.Count) Log Analytics workspaces:" "INFO"
        foreach ($ws in $allWorkspaces) {
            Write-DiagLog "  - $($ws.name)" "INFO"
        }
    }
}

# Check Key Vault
Write-DiagLog "Checking Key Vault: $($config.keyVault)" "INFO"
$keyVault = az keyvault show --name $config.keyVault --output json 2>$null | ConvertFrom-Json
if ($keyVault) {
    Write-DiagLog "✅ Key Vault exists" "SUCCESS"
    Write-DiagLog "  - Vault URI: $($keyVault.properties.vaultUri)" "INFO"
    Write-DiagLog "  - Provisioning State: $($keyVault.properties.provisioningState)" "INFO"
    
    # Check for monitoring-related secrets
    Write-DiagLog "Checking for monitoring secrets..." "INFO"
    $secrets = az keyvault secret list --vault-name $config.keyVault --output json 2>$null | ConvertFrom-Json
    if ($secrets) {
        $monitoringSecrets = $secrets | Where-Object { $_.name -like "*insights*" -or $_.name -like "*connection*" -or $_.name -like "*instrumentation*" }
        if ($monitoringSecrets) {
            Write-DiagLog "✅ Found monitoring-related secrets:" "SUCCESS"
            foreach ($secret in $monitoringSecrets) {
                Write-DiagLog "  - $($secret.name)" "INFO"
            }
        }
        else {
            Write-DiagLog "⚠️ No monitoring-related secrets found" "WARNING"
        }
    }
}
else {
    Write-DiagLog "❌ Key Vault not found" "ERROR"
}

# Check network connectivity
Write-DiagLog "Testing network connectivity..." "INFO"
try {
    $appUrl = "https://$($config.appName).azurewebsites.net"
    Write-DiagLog "Testing connectivity to: $appUrl" "INFO"
    
    # Test basic connectivity
    $response = Invoke-WebRequest -Uri $appUrl -Method HEAD -TimeoutSec 10 -SkipHttpErrorCheck -ErrorAction SilentlyContinue
    if ($response) {
        Write-DiagLog "✅ App Service is reachable (Status: $($response.StatusCode))" "SUCCESS"
        
        if ($response.StatusCode -eq 403) {
            Write-DiagLog "⚠️ Access forbidden (403) - possible IP restrictions" "WARNING"
        }
        elseif ($response.StatusCode -eq 200) {
            Write-DiagLog "✅ App Service responding normally" "SUCCESS"
        }
        else {
            Write-DiagLog "⚠️ Unexpected status code: $($response.StatusCode)" "WARNING"
        }
    }
    else {
        Write-DiagLog "❌ App Service not reachable" "ERROR"
    }
}
catch {
    Write-DiagLog "❌ Network connectivity test failed: $($_.Exception.Message)" "ERROR"
}

# Check alert rules
Write-DiagLog "Checking alert rules..." "INFO"
$alertRules = az monitor metrics alert list --resource-group $config.resourceGroup --output json 2>$null | ConvertFrom-Json
if ($alertRules -and $alertRules.Count -gt 0) {
    Write-DiagLog "✅ Found $($alertRules.Count) alert rules" "SUCCESS"
    $enabledAlerts = $alertRules | Where-Object { $_.enabled -eq $true }
    Write-DiagLog "  - Enabled alerts: $($enabledAlerts.Count)" "INFO"
    
    foreach ($alert in $alertRules | Select-Object -First 5) {
        $status = if ($alert.enabled) { "Enabled" } else { "Disabled" }
        Write-DiagLog "  - $($alert.name): $status (Severity: $($alert.severity))" "INFO"
    }
}
else {
    Write-DiagLog "⚠️ No alert rules found" "WARNING"
}

# Generate recommendations
Write-DiagLog "=============================" "INFO"
Write-DiagLog "RECOMMENDATIONS" "INFO"
Write-DiagLog "=============================" "INFO"

$recommendations = @()

if ($appService -and $appService.state -ne "Running") {
    $recommendations += "• Start the App Service to enable monitoring data collection"
}

if ($foundMonitoringSettings.Count -eq 0) {
    $recommendations += "• Configure Application Insights connection string in App Service settings"
}

if (-not $appInsights) {
    $recommendations += "• Deploy or configure Application Insights resource"
}

if ($response -and $response.StatusCode -eq 403) {
    $recommendations += "• Review IP restrictions on App Service if needed for monitoring tests"
}

if ($alertRules.Count -eq 0) {
    $recommendations += "• Create basic alert rules for monitoring critical metrics"
}

if ($recommendations.Count -gt 0) {
    Write-DiagLog "Action items to improve monitoring setup:" "WARNING"
    foreach ($rec in $recommendations) {
        Write-DiagLog "  $rec" "WARNING"
    }
}
else {
    Write-DiagLog "✅ Monitoring configuration looks good!" "SUCCESS"
}

# Summary
Write-DiagLog "=============================" "INFO"
Write-DiagLog "DIAGNOSTIC SUMMARY" "INFO"
Write-DiagLog "=============================" "INFO"

$resources = @{
    "Resource Group"       = if ($resourceGroup) { "✅ Exists" } else { "❌ Missing" }
    "App Service"          = if ($appService) { "✅ Exists" } else { "❌ Missing" }
    "Application Insights" = if ($appInsights) { "✅ Exists" } else { "❌ Missing" }
    "Log Analytics"        = if ($logAnalytics) { "✅ Exists" } else { "❌ Missing" }
    "Key Vault"            = if ($keyVault) { "✅ Exists" } else { "❌ Missing" }
}

foreach ($resource in $resources.GetEnumerator()) {
    Write-DiagLog "$($resource.Key): $($resource.Value)" "INFO"
}

$overallHealth = if ($resourceGroup -and $appService -and ($appInsights -or $logAnalytics)) { 
    "HEALTHY" 
}
elseif ($resourceGroup -and $appService) { 
    "PARTIAL" 
}
else { 
    "UNHEALTHY" 
}

$color = switch ($overallHealth) {
    "HEALTHY" { "SUCCESS" }
    "PARTIAL" { "WARNING" }
    "UNHEALTHY" { "ERROR" }
}

Write-DiagLog "Overall Monitoring Health: $overallHealth" $color
Write-DiagLog "=====================================" "INFO"
