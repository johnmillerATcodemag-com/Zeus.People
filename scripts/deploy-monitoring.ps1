# Deploy Monitoring Alert Rules to Azure
# This script deploys the comprehensive alert rules ARM template to Azure

param(
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroupName = "rg-academic-staging-westus2",
    
    [Parameter(Mandatory = $true)]  
    [string]$AlertEmailAddress,
    
    [Parameter(Mandatory = $false)]
    [string]$WebAppName = "app-academic-staging-dvjm4oxxoy2g6",
    
    [Parameter(Mandatory = $false)]
    [string]$ActionGroupName = "zeus-people-staging-alerts",
    
    [Parameter(Mandatory = $false)]
    [string]$TemplateFile = "./monitoring/alert-rules.json"
)

# Ensure we're in the correct directory
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Definition
$rootPath = Split-Path -Parent $scriptPath
Set-Location $rootPath

Write-Host "=== Zeus.People Monitoring Alert Deployment ===" -ForegroundColor Cyan
Write-Host "Resource Group: $ResourceGroupName" -ForegroundColor Yellow
Write-Host "Alert Email: $AlertEmailAddress" -ForegroundColor Yellow
Write-Host "Web App: $WebAppName" -ForegroundColor Yellow
Write-Host ""

# Check if user is logged into Azure
Write-Host "🔐 Checking Azure authentication..." -ForegroundColor Magenta
try {
    $account = az account show --query "user.name" --output tsv 2>$null
    if (-not $account) {
        Write-Host "❌ Not logged into Azure. Please run 'az login' first." -ForegroundColor Red
        exit 1
    }
    Write-Host "✅ Logged in as: $account" -ForegroundColor Green
} catch {
    Write-Host "❌ Error checking Azure authentication. Please run 'az login'." -ForegroundColor Red
    exit 1
}

# Verify template file exists
if (-not (Test-Path $TemplateFile)) {
    Write-Host "❌ Template file not found: $TemplateFile" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Template file found: $TemplateFile" -ForegroundColor Green

# Get Azure SQL Server and Database information
Write-Host ""
Write-Host "🔍 Discovering Azure SQL resources..." -ForegroundColor Magenta

try {
    $sqlServers = az sql server list --resource-group $ResourceGroupName --query "[?contains(name,'sql-academic-staging')].name" --output tsv
    
    if (-not $sqlServers) {
        Write-Host "❌ No SQL servers found in resource group $ResourceGroupName" -ForegroundColor Red
        exit 1
    }
    
    $sqlServerName = ($sqlServers -split "`n")[0].Trim()
    Write-Host "📋 Found SQL Server: $sqlServerName" -ForegroundColor Green
    
    $databases = az sql db list --resource-group $ResourceGroupName --server $sqlServerName --query "[?name!='master'].name" --output tsv
    
    if (-not $databases) {
        Write-Host "❌ No user databases found on server $sqlServerName" -ForegroundColor Red
        exit 1
    }
    
    $databaseName = ($databases -split "`n")[0].Trim()  
    Write-Host "📋 Found Database: $databaseName" -ForegroundColor Green
    
} catch {
    Write-Host "❌ Error discovering SQL resources: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Deploy the ARM template
Write-Host ""
Write-Host "🚨 Deploying comprehensive alert rules..." -ForegroundColor Magenta

$deploymentName = "alert-rules-deployment-$(Get-Date -Format 'yyyyMMdd-HHmmss')"

try {
    $deploymentResult = az deployment group create `
        --resource-group $ResourceGroupName `
        --name $deploymentName `
        --template-file $TemplateFile `
        --parameters `
            actionGroupName=$ActionGroupName `
            emailAddress=$AlertEmailAddress `
            webAppName=$WebAppName `
            databaseServerName=$sqlServerName `
            databaseName=$databaseName `
        --mode Incremental `
        --output json | ConvertFrom-Json
        
    if ($deploymentResult.properties.provisioningState -eq "Succeeded") {
        Write-Host "✅ Alert rules deployed successfully!" -ForegroundColor Green
    } else {
        Write-Host "❌ Deployment failed with state: $($deploymentResult.properties.provisioningState)" -ForegroundColor Red
        exit 1
    }
    
} catch {
    Write-Host "❌ Error deploying alert rules: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Verify deployment by listing alert rules
Write-Host ""
Write-Host "🔔 Verifying alert rules deployment..." -ForegroundColor Magenta

try {
    $alertRules = az monitor metrics alert list --resource-group $ResourceGroupName --output json | ConvertFrom-Json
    $alertCount = $alertRules.Count
    
    Write-Host "📊 Total alert rules deployed: $alertCount" -ForegroundColor Green
    
    foreach ($alert in $alertRules) {
        $status = if ($alert.enabled) { "✅ Enabled" } else { "⚠️ Disabled" }
        Write-Host "   - $($alert.name): $status (Severity: $($alert.severity))" -ForegroundColor White
    }
    
} catch {
    Write-Host "⚠️ Could not verify alert rules: $($_.Exception.Message)" -ForegroundColor Yellow
}

# Verify action group
Write-Host ""
Write-Host "📧 Verifying action group configuration..." -ForegroundColor Magenta

try {
    $actionGroups = az monitor action-group list --resource-group $ResourceGroupName --query "[?name=='$ActionGroupName']" --output json | ConvertFrom-Json
    
    if ($actionGroups.Count -gt 0) {
        $actionGroup = $actionGroups[0]
        Write-Host "✅ Action group '$ActionGroupName' is configured" -ForegroundColor Green
        Write-Host "   - Enabled: $($actionGroup.enabled)" -ForegroundColor White
        Write-Host "   - Email receivers: $($actionGroup.emailReceivers.Count)" -ForegroundColor White
        
        foreach ($receiver in $actionGroup.emailReceivers) {
            Write-Host "   - Email: $($receiver.emailAddress) (Status: $($receiver.status))" -ForegroundColor White
        }
    } else {
        Write-Host "❌ Action group '$ActionGroupName' not found" -ForegroundColor Red
    }
    
} catch {
    Write-Host "⚠️ Could not verify action group: $($_.Exception.Message)" -ForegroundColor Yellow
}

# Display monitoring links
Write-Host ""
Write-Host "🔗 Monitoring Links" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan

$subscriptionId = az account show --query "id" --output tsv

Write-Host "📊 Azure Portal - Alert Rules:" -ForegroundColor Yellow
Write-Host "   https://portal.azure.com/#@microsoft.onmicrosoft.com/resource/subscriptions/$subscriptionId/resourceGroups/$ResourceGroupName/providers/Microsoft.Insights/metricAlerts" -ForegroundColor White

Write-Host "📧 Azure Portal - Action Groups:" -ForegroundColor Yellow  
Write-Host "   https://portal.azure.com/#@microsoft.onmicrosoft.com/resource/subscriptions/$subscriptionId/resourceGroups/$ResourceGroupName/providers/Microsoft.Insights/actionGroups" -ForegroundColor White

Write-Host "📈 Application Insights:" -ForegroundColor Yellow
Write-Host "   https://portal.azure.com/#@microsoft.onmicrosoft.com/resource/subscriptions/$subscriptionId/resourceGroups/$ResourceGroupName/providers/Microsoft.Insights/components" -ForegroundColor White

Write-Host ""
Write-Host "🎉 Monitoring deployment completed successfully!" -ForegroundColor Green
Write-Host "Check your email ($AlertEmailAddress) for alert notifications." -ForegroundColor Yellow
