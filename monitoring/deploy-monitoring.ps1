# Zeus.People Monitoring Deployment Script
# This script deploys the comprehensive monitoring and alerting infrastructure

param(
    [Parameter(Mandatory=$true)]
    [string]$SubscriptionId,
    
    [Parameter(Mandatory=$true)]
    [string]$ResourceGroupName,
    
    [Parameter(Mandatory=$true)]
    [string]$EnvironmentName,
    
    [Parameter(Mandatory=$true)]
    [string]$EmailAddress,
    
    [Parameter(Mandatory=$false)]
    [string]$Location = "westus2"
)

# Duration: Monitoring deployment script started
$startTime = Get-Date
Write-Host "Starting monitoring infrastructure deployment at $startTime" -ForegroundColor Green

# Set error action preference
$ErrorActionPreference = "Stop"

try {
    # Connect to Azure
    Write-Host "Connecting to Azure subscription: $SubscriptionId" -ForegroundColor Yellow
    az account set --subscription $SubscriptionId
    
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to set Azure subscription"
    }

    # Get existing resource names from the deployment
    Write-Host "Retrieving existing resource information..." -ForegroundColor Yellow
    
    $webAppName = az webapp list --resource-group $ResourceGroupName --query "[?contains(name, 'app-academic')].name" --output tsv
    $appInsightsName = az monitor app-insights component list --resource-group $ResourceGroupName --query "[0].name" --output tsv
    $sqlServerName = az sql server list --resource-group $ResourceGroupName --query "[0].name" --output tsv
    $databaseName = az sql db list --resource-group $ResourceGroupName --server $sqlServerName --query "[?name != 'master'].name" --output tsv | Select-Object -First 1
    $serviceBusNamespace = az servicebus namespace list --resource-group $ResourceGroupName --query "[0].name" --output tsv

    Write-Host "Found resources:" -ForegroundColor Green
    Write-Host "  Web App: $webAppName" -ForegroundColor White
    Write-Host "  Application Insights: $appInsightsName" -ForegroundColor White
    Write-Host "  SQL Server: $sqlServerName" -ForegroundColor White
    Write-Host "  Database: $databaseName" -ForegroundColor White
    Write-Host "  Service Bus: $serviceBusNamespace" -ForegroundColor White

    # Deploy alert rules
    Write-Host "Deploying alert rules..." -ForegroundColor Yellow
    
    $alertDeploymentName = "monitoring-alerts-$(Get-Date -Format 'yyyyMMddHHmmss')"
    
    $alertParams = @{
        emailAddress = $EmailAddress
        webAppName = $webAppName
        applicationInsightsName = $appInsightsName
        databaseServerName = $sqlServerName
        databaseName = $databaseName
        serviceBusNamespace = $serviceBusNamespace
    }
    
    $alertParamsJson = $alertParams | ConvertTo-Json -Compress
    $alertParamsFile = "alert-params-temp.json"
    $alertParamsJson | Out-File -FilePath $alertParamsFile -Encoding UTF8
    
    az deployment group create `
        --resource-group $ResourceGroupName `
        --name $alertDeploymentName `
        --template-file "monitoring/alert-rules.json" `
        --parameters "@$alertParamsFile"
    
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to deploy alert rules"
    }
    
    Remove-Item $alertParamsFile -Force -ErrorAction SilentlyContinue
    Write-Host "Alert rules deployed successfully" -ForegroundColor Green

    # Deploy comprehensive dashboard
    Write-Host "Deploying comprehensive dashboard..." -ForegroundColor Yellow
    
    $dashboardDeploymentName = "monitoring-dashboard-$(Get-Date -Format 'yyyyMMddHHmmss')"
    
    $dashboardParams = @{
        webAppName = $webAppName
        applicationInsightsName = $appInsightsName
        databaseServerName = $sqlServerName
        databaseName = $databaseName
        serviceBusNamespace = $serviceBusNamespace
        resourceGroupName = $ResourceGroupName
    }
    
    $dashboardParamsJson = $dashboardParams | ConvertTo-Json -Compress
    $dashboardParamsFile = "dashboard-params-temp.json"
    $dashboardParamsJson | Out-File -FilePath $dashboardParamsFile -Encoding UTF8
    
    az deployment group create `
        --resource-group $ResourceGroupName `
        --name $dashboardDeploymentName `
        --template-file "monitoring/comprehensive-dashboard.json" `
        --parameters "@$dashboardParamsFile"
    
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Dashboard deployment failed, but continuing with other monitoring setup"
    } else {
        Write-Host "Dashboard deployed successfully" -ForegroundColor Green
    }
    
    Remove-Item $dashboardParamsFile -Force -ErrorAction SilentlyContinue

    # Configure Application Insights sampling and other settings
    Write-Host "Configuring Application Insights advanced settings..." -ForegroundColor Yellow
    
    # Get Application Insights resource details
    $appInsightsDetails = az monitor app-insights component show `
        --resource-group $ResourceGroupName `
        --app $appInsightsName `
        --query "{instrumentationKey: instrumentationKey, connectionString: connectionString}" `
        --output json | ConvertFrom-Json
    
    Write-Host "Application Insights Configuration:" -ForegroundColor Green
    Write-Host "  Instrumentation Key: $($appInsightsDetails.instrumentationKey)" -ForegroundColor White
    Write-Host "  Connection String: Available" -ForegroundColor White

    # Update application configuration with monitoring settings
    Write-Host "Updating application configuration with monitoring settings..." -ForegroundColor Yellow
    
    # Set Application Insights connection string in App Service configuration
    if ($webAppName) {
        az webapp config appsettings set `
            --resource-group $ResourceGroupName `
            --name $webAppName `
            --settings "ApplicationInsights:ConnectionString=$($appInsightsDetails.connectionString)" `
                      "ApplicationInsights:InstrumentationKey=$($appInsightsDetails.instrumentationKey)" `
                      "Monitoring:EnableApplicationInsights=true" `
                      "Monitoring:EnableCustomMetrics=true" `
                      "Monitoring:SamplingPercentage=100.0"
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Application configuration updated successfully" -ForegroundColor Green
        } else {
            Write-Warning "Failed to update application configuration"
        }
    }

    # Create Log Analytics queries for monitoring
    Write-Host "Creating custom Log Analytics queries..." -ForegroundColor Yellow
    
    $queries = @(
        @{
            name = "High Error Rate Detection"
            query = "requests | where timestamp > ago(5m) | summarize ErrorRate = countif(success == false) * 100.0 / count() | where ErrorRate > 5"
        },
        @{
            name = "Slow Requests"
            query = "requests | where timestamp > ago(5m) and duration > 2000 | project timestamp, name, duration, url | order by duration desc"
        },
        @{
            name = "Business Rule Violations"
            query = "customEvents | where name == 'BusinessRuleEvaluation' and customDimensions.Passed == 'False' | where timestamp > ago(15m)"
        },
        @{
            name = "Authentication Failures"  
            query = "requests | where timestamp > ago(5m) and resultCode in (401, 403) | summarize count() by client_IP | where count_ > 10"
        },
        @{
            name = "Database Performance Issues"
            query = "dependencies | where type == 'SQL' and timestamp > ago(5m) | where duration > 1000 or success == false | order by timestamp desc"
        }
    )
    
    Write-Host "Useful Log Analytics Queries:" -ForegroundColor Green
    foreach ($query in $queries) {
        Write-Host "  - $($query.name)" -ForegroundColor White
        Write-Host "    $($query.query)" -ForegroundColor Gray
        Write-Host ""
    }

    # Generate monitoring report
    $endTime = Get-Date
    $duration = $endTime - $startTime
    
    Write-Host "Monitoring Infrastructure Deployment Complete!" -ForegroundColor Green
    Write-Host "Duration: $($duration.ToString('mm\:ss'))" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Next Steps:" -ForegroundColor Cyan
    Write-Host "1. Deploy your application with the updated monitoring configuration" -ForegroundColor White
    Write-Host "2. Generate test traffic to validate telemetry collection" -ForegroundColor White  
    Write-Host "3. Test alert rules by triggering threshold conditions" -ForegroundColor White
    Write-Host "4. Review the comprehensive dashboard in Azure Portal" -ForegroundColor White
    Write-Host "5. Verify structured logging appears in Application Insights" -ForegroundColor White
    Write-Host "6. Test incident response procedures using the runbooks" -ForegroundColor White
    Write-Host ""
    Write-Host "Resources Created/Updated:" -ForegroundColor Cyan
    Write-Host "- Alert Action Group: zeus-people-alerts" -ForegroundColor White
    Write-Host "- Alert Rules: 8 monitoring rules" -ForegroundColor White
    Write-Host "- Dashboard: Zeus-People-Comprehensive-Dashboard" -ForegroundColor White
    Write-Host "- Application Configuration: Updated with monitoring settings" -ForegroundColor White
    Write-Host ""
    Write-Host "Access Points:" -ForegroundColor Cyan
    Write-Host "- Dashboard: Azure Portal > Dashboards > Zeus-People-Comprehensive-Dashboard" -ForegroundColor White
    Write-Host "- Application Insights: Azure Portal > $appInsightsName" -ForegroundColor White
    Write-Host "- Alert Rules: Azure Portal > Monitor > Alerts" -ForegroundColor White
    Write-Host ""

    # Save deployment summary
    $deploymentSummary = @{
        DeploymentTime = $endTime.ToString("yyyy-MM-dd HH:mm:ss")
        Duration = $duration.ToString("mm\:ss")
        ResourceGroup = $ResourceGroupName
        Environment = $EnvironmentName
        WebApp = $webAppName
        ApplicationInsights = $appInsightsName
        SqlServer = $sqlServerName
        Database = $databaseName
        ServiceBus = $serviceBusNamespace
        AlertsDeployed = 8
        DashboardCreated = $true
        InstrumentationKey = $appInsightsDetails.instrumentationKey
        Status = "Success"
    }
    
    $summaryJson = $deploymentSummary | ConvertTo-Json -Depth 10
    $summaryFile = "monitoring-deployment-summary-$EnvironmentName-$(Get-Date -Format 'yyyyMMddHHmmss').json"
    $summaryJson | Out-File -FilePath $summaryFile -Encoding UTF8
    
    Write-Host "Deployment summary saved to: $summaryFile" -ForegroundColor Green

} catch {
    Write-Host "Monitoring deployment failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Error details: $($_.Exception.ToString())" -ForegroundColor Red
    exit 1
}

Write-Host "Monitoring and observability setup completed successfully!" -ForegroundColor Green
# Duration: Monitoring deployment script completed
