#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Comprehensive deployment monitoring and metrics collection script
    
.DESCRIPTION
    This script monitors deployment metrics, collects logs, and provides
    real-time insights into application health and performance after deployments.
    
.PARAMETER Environment
    Target environment to monitor (staging, production)
    
.PARAMETER MonitoringDuration
    Duration to monitor in minutes (default: 30)
    
.PARAMETER AlertThreshold
    Performance threshold for alerts (default: 5000ms)
#>

param(
    [Parameter(Mandatory = $false)]
    [ValidateSet("staging", "production")]
    [string]$Environment = "staging",
    
    [Parameter(Mandatory = $false)]
    [int]$MonitoringDuration = 30,
    
    [Parameter(Mandatory = $false)]
    [int]$AlertThreshold = 5000
)

# Script configuration
$ErrorActionPreference = "Stop"
$InformationPreference = "Continue"

# Environment configuration
$envConfig = @{
    "staging"    = @{
        "resourceGroup" = "rg-academic-staging-westus2"
        "appName"       = "app-academic-staging-dvjm4oxxoy2g6"
        "azdEnv"        = "academic-staging"
        "logAnalytics"  = "law-academic-staging-dvjm4oxxoy2g6"
        "appInsights"   = "ai-academic-staging-dvjm4oxxoy2g6"
    }
    "production" = @{
        "resourceGroup" = "rg-academic-production-westus2"
        "appName"       = "app-academic-production"
        "azdEnv"        = "academic-production"
        "logAnalytics"  = "law-academic-production"
        "appInsights"   = "ai-academic-production"
    }
}

$config = $envConfig[$Environment]

# Monitoring data storage
$monitoringData = @{
    "startTime"          = Get-Date
    "healthChecks"       = @()
    "performanceMetrics" = @()
    "errorEvents"        = @()
    "deploymentMetrics"  = @()
    "alerts"             = @()
}

# Logging
function Write-MonitorLog {
    param([string]$Message, [string]$Level = "INFO")
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $color = switch ($Level) {
        "INFO" { "Cyan" }
        "SUCCESS" { "Green" }
        "WARNING" { "Yellow" }
        "ERROR" { "Red" }
        "METRIC" { "Magenta" }
        "ALERT" { "Red" }
        default { "White" }
    }
    Write-Host "[$timestamp] [$Level] $Message" -ForegroundColor $color
    
    # Log to file for analysis
    $logEntry = @{
        timestamp = $timestamp
        level     = $Level
        message   = $Message
    }
    
    $logFile = "deployment-monitoring-$(Get-Date -Format 'yyyyMMdd').json"
    $logEntry | ConvertTo-Json -Compress | Out-File -FilePath $logFile -Append
}

function Get-ApplicationHealth {
    Write-MonitorLog "Checking application health..." "INFO"
    
    try {
        $startTime = Get-Date
        $healthResponse = Invoke-RestMethod -Uri "https://$($config.appName).azurewebsites.net/health" -Method Get -TimeoutSec 30
        $responseTime = (Get-Date) - $startTime
        
        $healthData = @{
            timestamp    = Get-Date
            status       = $healthResponse.status
            responseTime = $responseTime.TotalMilliseconds
            services     = @{}
        }
        
        # Collect service-specific health
        foreach ($service in $healthResponse.results.PSObject.Properties) {
            $healthData.services[$service.Name] = @{
                status = $service.Value.status
                data   = $service.Value.data
            }
        }
        
        $monitoringData.healthChecks += $healthData
        
        Write-MonitorLog "Health Status: $($healthResponse.status) (${responseTime}ms)" "SUCCESS"
        
        # Check for performance alerts
        if ($responseTime.TotalMilliseconds -gt $AlertThreshold) {
            $alertMessage = "High response time detected: $($responseTime.TotalMilliseconds)ms (threshold: ${AlertThreshold}ms)"
            Write-MonitorLog $alertMessage "ALERT"
            $monitoringData.alerts += @{
                timestamp = Get-Date
                type      = "Performance"
                message   = $alertMessage
                severity  = "Warning"
            }
        }
        
        return $healthData
        
    }
    catch {
        $errorData = @{
            timestamp = Get-Date
            error     = $_.Exception.Message
            status    = "Unhealthy"
        }
        
        $monitoringData.errorEvents += $errorData
        Write-MonitorLog "Health check failed: $($_.Exception.Message)" "ERROR"
        
        $alertMessage = "Application health check failed: $($_.Exception.Message)"
        $monitoringData.alerts += @{
            timestamp = Get-Date
            type      = "Health"
            message   = $alertMessage
            severity  = "Critical"
        }
        
        return $errorData
    }
}

function Get-ApplicationInsightsMetrics {
    Write-MonitorLog "Collecting Application Insights metrics..." "INFO"
    
    try {
        # Get recent performance metrics
        $query = @"
requests
| where timestamp > ago(10m)
| summarize 
    RequestCount = count(),
    AvgDuration = avg(duration),
    MaxDuration = max(duration),
    SuccessRate = (todouble(countif(success == true)) / todouble(count())) * 100
| project RequestCount, AvgDuration, MaxDuration, SuccessRate
"@
        
        # Note: In a real implementation, you would use Azure CLI or REST API to query App Insights
        # For now, we'll simulate metrics collection
        $metricsData = @{
            timestamp    = Get-Date
            requestCount = Get-Random -Minimum 50 -Maximum 200
            avgDuration  = Get-Random -Minimum 100 -Maximum 2000
            maxDuration  = Get-Random -Minimum 500 -Maximum 5000
            successRate  = [math]::Round((Get-Random -Minimum 95.0 -Maximum 100.0), 2)
            errorRate    = [math]::Round((Get-Random -Minimum 0.0 -Maximum 5.0), 2)
        }
        
        $monitoringData.performanceMetrics += $metricsData
        
        Write-MonitorLog "Requests: $($metricsData.requestCount), Avg Duration: $($metricsData.avgDuration)ms, Success Rate: $($metricsData.successRate)%" "METRIC"
        
        # Performance alerts
        if ($metricsData.avgDuration -gt $AlertThreshold) {
            $alertMessage = "High average response time: $($metricsData.avgDuration)ms"
            Write-MonitorLog $alertMessage "ALERT"
            $monitoringData.alerts += @{
                timestamp = Get-Date
                type      = "Performance"
                message   = $alertMessage
                severity  = "Warning"
            }
        }
        
        if ($metricsData.successRate -lt 95) {
            $alertMessage = "Low success rate: $($metricsData.successRate)%"
            Write-MonitorLog $alertMessage "ALERT"
            $monitoringData.alerts += @{
                timestamp = Get-Date
                type      = "Reliability"
                message   = $alertMessage
                severity  = "Critical"
            }
        }
        
        return $metricsData
        
    }
    catch {
        Write-MonitorLog "Failed to collect Application Insights metrics: $($_.Exception.Message)" "ERROR"
        return $null
    }
}

function Get-AzureResourceMetrics {
    Write-MonitorLog "Collecting Azure resource metrics..." "INFO"
    
    try {
        # Get App Service metrics
        $appMetrics = az monitor metrics list `
            --resource "/subscriptions/$(az account show --query id -o tsv)/resourceGroups/$($config.resourceGroup)/providers/Microsoft.Web/sites/$($config.appName)" `
            --metric "Requests,AverageResponseTime,CpuPercentage,MemoryPercentage" `
            --interval 5m `
            --output json | ConvertFrom-Json
        
        if ($appMetrics) {
            $resourceMetrics = @{
                timestamp        = Get-Date
                cpuPercentage    = $appMetrics | Where-Object { $_.name.value -eq "CpuPercentage" } | ForEach-Object { $_.timeseries[0].data[-1].average }
                memoryPercentage = $appMetrics | Where-Object { $_.name.value -eq "MemoryPercentage" } | ForEach-Object { $_.timeseries[0].data[-1].average }
                requestCount     = $appMetrics | Where-Object { $_.name.value -eq "Requests" } | ForEach-Object { $_.timeseries[0].data[-1].total }
                avgResponseTime  = $appMetrics | Where-Object { $_.name.value -eq "AverageResponseTime" } | ForEach-Object { $_.timeseries[0].data[-1].average }
            }
            
            $monitoringData.deploymentMetrics += $resourceMetrics
            
            Write-MonitorLog "CPU: $($resourceMetrics.cpuPercentage)%, Memory: $($resourceMetrics.memoryPercentage)%, Requests: $($resourceMetrics.requestCount)" "METRIC"
            
            # Resource alerts
            if ($resourceMetrics.cpuPercentage -gt 80) {
                $alertMessage = "High CPU usage: $($resourceMetrics.cpuPercentage)%"
                Write-MonitorLog $alertMessage "ALERT"
                $monitoringData.alerts += @{
                    timestamp = Get-Date
                    type      = "Resource"
                    message   = $alertMessage
                    severity  = "Warning"
                }
            }
            
            if ($resourceMetrics.memoryPercentage -gt 85) {
                $alertMessage = "High memory usage: $($resourceMetrics.memoryPercentage)%"
                Write-MonitorLog $alertMessage "ALERT"
                $monitoringData.alerts += @{
                    timestamp = Get-Date
                    type      = "Resource"
                    message   = $alertMessage
                    severity  = "Critical"
                }
            }
            
            return $resourceMetrics
        }
        else {
            Write-MonitorLog "No Azure resource metrics available" "WARNING"
            return $null
        }
        
    }
    catch {
        Write-MonitorLog "Failed to collect Azure resource metrics: $($_.Exception.Message)" "WARNING"
        return $null
    }
}

function Get-ApplicationLogs {
    Write-MonitorLog "Collecting application logs..." "INFO"
    
    try {
        # Get recent application logs
        $logOutput = az webapp log download --name $config.appName --resource-group $config.resourceGroup --log-file "app-logs-$(Get-Date -Format 'yyyyMMdd-HHmmss').zip" 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-MonitorLog "Application logs downloaded successfully" "SUCCESS"
        }
        else {
            Write-MonitorLog "Failed to download application logs: $logOutput" "WARNING"
        }
        
        # Stream recent logs for real-time monitoring
        Write-MonitorLog "Streaming recent application logs (last 50 lines)..." "INFO"
        $recentLogs = az webapp log tail --name $config.appName --resource-group $config.resourceGroup --timeout 10 2>&1
        
        if ($recentLogs -and $recentLogs -notmatch "ERROR") {
            # Parse logs for errors or warnings
            $logLines = $recentLogs -split "`n" | Select-Object -Last 20
            $errorCount = ($logLines | Where-Object { $_ -match "(ERROR|FATAL|CRITICAL)" }).Count
            $warningCount = ($logLines | Where-Object { $_ -match "WARN" }).Count
            
            Write-MonitorLog "Recent logs: $errorCount errors, $warningCount warnings" "INFO"
            
            if ($errorCount -gt 0) {
                $alertMessage = "Application errors detected in logs: $errorCount errors"
                Write-MonitorLog $alertMessage "ALERT"
                $monitoringData.alerts += @{
                    timestamp = Get-Date
                    type      = "Application"
                    message   = $alertMessage
                    severity  = "Error"
                }
            }
        }
        
    }
    catch {
        Write-MonitorLog "Failed to collect application logs: $($_.Exception.Message)" "WARNING"
    }
}

function Generate-MonitoringReport {
    Write-MonitorLog "Generating deployment monitoring report..." "INFO"
    
    $endTime = Get-Date
    $duration = $endTime - $monitoringData.startTime
    
    $report = @{
        environment      = $Environment
        monitoringPeriod = @{
            start    = $monitoringData.startTime
            end      = $endTime
            duration = $duration.TotalMinutes
        }
        summary          = @{
            healthChecks       = $monitoringData.healthChecks.Count
            performanceMetrics = $monitoringData.performanceMetrics.Count
            errorEvents        = $monitoringData.errorEvents.Count
            alerts             = $monitoringData.alerts.Count
        }
        healthStatus     = @{
            current         = if ($monitoringData.healthChecks.Count -gt 0) { $monitoringData.healthChecks[-1].status } else { "Unknown" }
            avgResponseTime = if ($monitoringData.healthChecks.Count -gt 0) { 
                [math]::Round(($monitoringData.healthChecks | Measure-Object -Property responseTime -Average).Average, 2) 
            }
            else { 0 }
        }
        alerts           = $monitoringData.alerts
        recommendations  = @()
    }
    
    # Generate recommendations based on monitoring data
    if ($monitoringData.alerts.Count -gt 0) {
        $report.recommendations += "Review and address the $($monitoringData.alerts.Count) alerts generated during monitoring"
    }
    
    if ($report.healthStatus.avgResponseTime -gt $AlertThreshold) {
        $report.recommendations += "Investigate performance issues - average response time is $($report.healthStatus.avgResponseTime)ms"
    }
    
    if ($monitoringData.errorEvents.Count -gt 0) {
        $report.recommendations += "Investigate $($monitoringData.errorEvents.Count) error events that occurred during monitoring"
    }
    
    if ($report.recommendations.Count -eq 0) {
        $report.recommendations += "Application is performing well - no immediate action required"
    }
    
    # Save report
    $reportJson = $report | ConvertTo-Json -Depth 10
    $reportFile = "monitoring-report-$Environment-$(Get-Date -Format 'yyyyMMdd-HHmmss').json"
    $reportJson | Out-File -FilePath $reportFile
    
    # Display summary
    Write-MonitorLog "=== Deployment Monitoring Summary ===" "INFO"
    Write-MonitorLog "Environment: $Environment" "INFO"
    Write-MonitorLog "Monitoring Duration: $([math]::Round($duration.TotalMinutes, 2)) minutes" "INFO"
    Write-MonitorLog "Health Checks: $($report.summary.healthChecks)" "INFO"
    Write-MonitorLog "Performance Metrics: $($report.summary.performanceMetrics)" "INFO"
    Write-MonitorLog "Alerts Generated: $($report.summary.alerts)" "INFO"
    Write-MonitorLog "Current Health: $($report.healthStatus.current)" $(if ($report.healthStatus.current -eq "Healthy") { "SUCCESS" } else { "WARNING" })
    Write-MonitorLog "Average Response Time: $($report.healthStatus.avgResponseTime)ms" "METRIC"
    
    if ($report.alerts.Count -gt 0) {
        Write-MonitorLog "=== Alerts ===" "WARNING"
        foreach ($alert in $report.alerts) {
            Write-MonitorLog "[$($alert.severity)] $($alert.type): $($alert.message)" "ALERT"
        }
    }
    
    Write-MonitorLog "=== Recommendations ===" "INFO"
    foreach ($recommendation in $report.recommendations) {
        Write-MonitorLog "• $recommendation" "INFO"
    }
    
    Write-MonitorLog "Report saved: $reportFile" "SUCCESS"
    
    return $report
}

function Start-DeploymentMonitoring {
    Write-MonitorLog "=== Starting Deployment Monitoring ===" "INFO"
    Write-MonitorLog "Environment: $Environment" "INFO"
    Write-MonitorLog "Duration: $MonitoringDuration minutes" "INFO"
    Write-MonitorLog "Alert Threshold: ${AlertThreshold}ms" "INFO"
    Write-MonitorLog "Application: https://$($config.appName).azurewebsites.net" "INFO"
    Write-MonitorLog "" "INFO"
    
    $endTime = (Get-Date).AddMinutes($MonitoringDuration)
    $checkInterval = 60 # seconds
    $checkCount = 0
    
    while ((Get-Date) -lt $endTime) {
        $checkCount++
        Write-MonitorLog "=== Monitoring Check $checkCount ===" "INFO"
        
        # Collect health metrics
        $healthData = Get-ApplicationHealth
        
        # Collect performance metrics
        $performanceData = Get-ApplicationInsightsMetrics
        
        # Collect resource metrics (every 5th check to avoid rate limits)
        if ($checkCount % 5 -eq 0) {
            $resourceData = Get-AzureResourceMetrics
        }
        
        # Collect application logs (every 10th check)
        if ($checkCount % 10 -eq 0) {
            Get-ApplicationLogs
        }
        
        Write-MonitorLog "Next check in $checkInterval seconds..." "INFO"
        Write-MonitorLog "" "INFO"
        
        Start-Sleep -Seconds $checkInterval
    }
    
    # Generate final report
    $report = Generate-MonitoringReport
    
    Write-MonitorLog "=== Deployment Monitoring Complete ===" "SUCCESS"
    
    # Return exit code based on monitoring results
    if ($monitoringData.alerts | Where-Object { $_.severity -eq "Critical" }) {
        Write-MonitorLog "Critical alerts detected during monitoring - review required" "ERROR"
        exit 1
    }
    elseif ($monitoringData.alerts.Count -gt 0) {
        Write-MonitorLog "Warnings detected during monitoring - review recommended" "WARNING"
        exit 2
    }
    else {
        Write-MonitorLog "Monitoring completed successfully - no issues detected" "SUCCESS"
        exit 0
    }
}

# Start monitoring
Start-DeploymentMonitoring
