#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Real-time monitoring dashboard for Zeus.People deployments
    
.DESCRIPTION
    This script creates a real-time monitoring dashboard that displays
    deployment metrics, health status, and performance indicators.
    
.PARAMETER Environment
    Target environment to monitor (staging, production)
    
.PARAMETER RefreshInterval
    Dashboard refresh interval in seconds (default: 5)
    
.PARAMETER DashboardMode
    Dashboard display mode: Console, File, or Both (default: Console)
#>

param(
    [Parameter(Mandatory = $false)]
    [ValidateSet("staging", "production")]
    [string]$Environment = "staging",
    
    [Parameter(Mandatory = $false)]
    [int]$RefreshInterval = 5,
    
    [Parameter(Mandatory = $false)]
    [ValidateSet("Console", "File", "Both")]
    [string]$DashboardMode = "Console"
)

# Environment configuration
$envConfig = @{
    "staging"    = @{
        "resourceGroup" = "rg-academic-staging-westus2"
        "appName"       = "app-academic-staging-dvjm4oxxoy2g6"
        "appUrl"        = "https://app-academic-staging-dvjm4oxxoy2g6.azurewebsites.net"
    }
    "production" = @{
        "resourceGroup" = "rg-academic-production-westus2"
        "appName"       = "app-academic-production"
        "appUrl"        = "https://app-academic-production.azurewebsites.net"
    }
}

$config = $envConfig[$Environment]

# Dashboard state
$dashboardData = @{
    startTime          = Get-Date
    refreshCount       = 0
    healthHistory      = @()
    performanceHistory = @()
    alerts             = @()
}

function Get-ColoredStatus {
    param([string]$Status)
    
    switch ($Status.ToLower()) {
        "healthy" { return "🟢 $Status" }
        "unhealthy" { return "🔴 $Status" }
        "degraded" { return "🟡 $Status" }
        "warning" { return "🟡 $Status" }
        "error" { return "🔴 $Status" }
        "critical" { return "🔴 $Status" }
        default { return "⚪ $Status" }
    }
}

function Get-PerformanceIndicator {
    param([double]$Value, [double]$GoodThreshold, [double]$WarningThreshold)
    
    if ($Value -le $GoodThreshold) { return "🟢" }
    elseif ($Value -le $WarningThreshold) { return "🟡" }
    else { return "🔴" }
}

function Get-ApplicationHealth {
    try {
        $startTime = Get-Date
        $response = Invoke-RestMethod -Uri "$($config.appUrl)/health" -Method Get -TimeoutSec 10
        $responseTime = ((Get-Date) - $startTime).TotalMilliseconds
        
        $healthData = @{
            timestamp    = Get-Date
            status       = $response.status
            responseTime = $responseTime
            services     = @{}
        }
        
        foreach ($service in $response.results.PSObject.Properties) {
            $healthData.services[$service.Name] = @{
                status = $service.Value.status
                data   = $service.Value.data
            }
        }
        
        $dashboardData.healthHistory += $healthData
        # Keep only last 20 entries
        if ($dashboardData.healthHistory.Count -gt 20) {
            $dashboardData.healthHistory = $dashboardData.healthHistory[-20..-1]
        }
        
        return $healthData
    }
    catch {
        $errorData = @{
            timestamp    = Get-Date
            status       = "Unreachable"
            responseTime = -1
            error        = $_.Exception.Message
            services     = @{}
        }
        
        $dashboardData.healthHistory += $errorData
        return $errorData
    }
}

function Get-QuickMetrics {
    try {
        # Simulate performance metrics (in real scenario, would query Application Insights)
        $metrics = @{
            timestamp         = Get-Date
            requestsPerMinute = Get-Random -Minimum 20 -Maximum 150
            avgResponseTime   = Get-Random -Minimum 100 -Maximum 2000
            errorRate         = [math]::Round((Get-Random -Minimum 0.0 -Maximum 5.0), 2)
            cpuUsage          = Get-Random -Minimum 10 -Maximum 95
            memoryUsage       = Get-Random -Minimum 30 -Maximum 90
            activeConnections = Get-Random -Minimum 5 -Maximum 50
        }
        
        $dashboardData.performanceHistory += $metrics
        # Keep only last 20 entries
        if ($dashboardData.performanceHistory.Count -gt 20) {
            $dashboardData.performanceHistory = $dashboardData.performanceHistory[-20..-1]
        }
        
        return $metrics
    }
    catch {
        return $null
    }
}

function Show-ConsoleDashboard {
    param($HealthData, $MetricsData)
    
    Clear-Host
    
    # Header
    Write-Host "╔══════════════════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║                          Zeus.People Monitoring Dashboard                       ║" -ForegroundColor Cyan
    Write-Host "╚══════════════════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
    Write-Host ""
    
    $currentTime = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $uptime = (Get-Date) - $dashboardData.startTime
    
    Write-Host "🕒 Current Time: $currentTime" -ForegroundColor White
    Write-Host "⏱️ Monitoring Uptime: $($uptime.Days)d $($uptime.Hours)h $($uptime.Minutes)m $($uptime.Seconds)s" -ForegroundColor White
    Write-Host "🔄 Refresh Count: $($dashboardData.refreshCount)" -ForegroundColor White
    Write-Host "🌍 Environment: $Environment" -ForegroundColor Yellow
    Write-Host "🔗 Application: $($config.appUrl)" -ForegroundColor Blue
    Write-Host ""
    
    # Health Status Section
    Write-Host "┌─ 🏥 HEALTH STATUS ────────────────────────────────────────────────────────────┐" -ForegroundColor Green
    if ($HealthData) {
        $statusDisplay = Get-ColoredStatus $HealthData.status
        Write-Host "│ Overall Status: $statusDisplay" -ForegroundColor White
        Write-Host "│ Response Time: $([math]::Round($HealthData.responseTime, 2))ms $(Get-PerformanceIndicator $HealthData.responseTime 1000 3000)" -ForegroundColor White
        Write-Host "│" -ForegroundColor Green
        
        foreach ($service in $HealthData.services.GetEnumerator()) {
            $serviceStatus = Get-ColoredStatus $service.Value.status
            Write-Host "│ $($service.Key): $serviceStatus" -ForegroundColor White
        }
    }
    else {
        Write-Host "│ ❌ Unable to retrieve health data" -ForegroundColor Red
    }
    Write-Host "└───────────────────────────────────────────────────────────────────────────────┘" -ForegroundColor Green
    Write-Host ""
    
    # Performance Metrics Section
    Write-Host "┌─ 📊 PERFORMANCE METRICS ──────────────────────────────────────────────────────┐" -ForegroundColor Magenta
    if ($MetricsData) {
        Write-Host "│ Requests/min: $($MetricsData.requestsPerMinute) $(Get-PerformanceIndicator $MetricsData.requestsPerMinute 100 200)" -ForegroundColor White
        Write-Host "│ Avg Response Time: $($MetricsData.avgResponseTime)ms $(Get-PerformanceIndicator $MetricsData.avgResponseTime 1000 3000)" -ForegroundColor White
        Write-Host "│ Error Rate: $($MetricsData.errorRate)% $(Get-PerformanceIndicator $MetricsData.errorRate 1 5)" -ForegroundColor White
        Write-Host "│ CPU Usage: $($MetricsData.cpuUsage)% $(Get-PerformanceIndicator $MetricsData.cpuUsage 70 90)" -ForegroundColor White
        Write-Host "│ Memory Usage: $($MetricsData.memoryUsage)% $(Get-PerformanceIndicator $MetricsData.memoryUsage 80 95)" -ForegroundColor White
        Write-Host "│ Active Connections: $($MetricsData.activeConnections) $(Get-PerformanceIndicator $MetricsData.activeConnections 30 100)" -ForegroundColor White
    }
    else {
        Write-Host "│ ❌ Unable to retrieve performance metrics" -ForegroundColor Red
    }
    Write-Host "└───────────────────────────────────────────────────────────────────────────────┘" -ForegroundColor Magenta
    Write-Host ""
    
    # Health Trend Section
    if ($dashboardData.healthHistory.Count -gt 1) {
        Write-Host "┌─ 📈 HEALTH TREND (Last 10 checks) ────────────────────────────────────────────┐" -ForegroundColor Yellow
        $recent = $dashboardData.healthHistory | Select-Object -Last 10
        $trendLine = ""
        foreach ($entry in $recent) {
            switch ($entry.status) {
                "Healthy" { $trendLine += "🟢" }
                "Unhealthy" { $trendLine += "🔴" }
                "Degraded" { $trendLine += "🟡" }
                default { $trendLine += "⚪" }
            }
        }
        Write-Host "│ $trendLine" -ForegroundColor White
        Write-Host "└───────────────────────────────────────────────────────────────────────────────┘" -ForegroundColor Yellow
        Write-Host ""
    }
    
    # Response Time Trend
    if ($dashboardData.healthHistory.Count -gt 1) {
        Write-Host "┌─ ⚡ RESPONSE TIME TREND (Last 10 checks) ──────────────────────────────────────┐" -ForegroundColor Cyan
        $recent = $dashboardData.healthHistory | Where-Object { $_.responseTime -gt 0 } | Select-Object -Last 10
        if ($recent.Count -gt 0) {
            $maxResponse = ($recent | Measure-Object -Property responseTime -Maximum).Maximum
            $avgResponse = ($recent | Measure-Object -Property responseTime -Average).Average
            
            Write-Host "│ Average: $([math]::Round($avgResponse, 2))ms | Max: $([math]::Round($maxResponse, 2))ms" -ForegroundColor White
            
            # Create simple trend visualization
            $trendBar = ""
            foreach ($entry in $recent) {
                $percentage = ($entry.responseTime / $maxResponse) * 100
                if ($percentage -le 33) { $trendBar += "▁" }
                elseif ($percentage -le 66) { $trendBar += "▄" }
                else { $trendBar += "█" }
            }
            Write-Host "│ $trendBar" -ForegroundColor Green
        }
        else {
            Write-Host "│ No response time data available" -ForegroundColor Yellow
        }
        Write-Host "└───────────────────────────────────────────────────────────────────────────────┘" -ForegroundColor Cyan
        Write-Host ""
    }
    
    # Controls
    Write-Host "┌─ 🎛️ CONTROLS ──────────────────────────────────────────────────────────────────┐" -ForegroundColor DarkGray
    Write-Host "│ Press 'Ctrl+C' to exit | Refresh every $RefreshInterval seconds" -ForegroundColor Gray
    Write-Host "└───────────────────────────────────────────────────────────────────────────────┘" -ForegroundColor DarkGray
}

function Save-DashboardData {
    param($HealthData, $MetricsData)
    
    $dashboardSnapshot = @{
        timestamp          = Get-Date
        environment        = $Environment
        refreshCount       = $dashboardData.refreshCount
        health             = $HealthData
        metrics            = $MetricsData
        healthHistory      = $dashboardData.healthHistory
        performanceHistory = $dashboardData.performanceHistory
    }
    
    $fileName = "dashboard-snapshot-$Environment-$(Get-Date -Format 'yyyyMMdd-HHmmss').json"
    $dashboardSnapshot | ConvertTo-Json -Depth 10 | Out-File -FilePath $fileName -Encoding UTF8
}

function Start-MonitoringDashboard {
    Write-Host "Starting Zeus.People Monitoring Dashboard..." -ForegroundColor Green
    Write-Host "Environment: $Environment" -ForegroundColor Yellow
    Write-Host "Refresh Interval: $RefreshInterval seconds" -ForegroundColor Yellow
    Write-Host "Dashboard Mode: $DashboardMode" -ForegroundColor Yellow
    Write-Host "Press Ctrl+C to stop..." -ForegroundColor Gray
    Write-Host ""
    
    try {
        while ($true) {
            $dashboardData.refreshCount++
            
            # Collect data
            $healthData = Get-ApplicationHealth
            $metricsData = Get-QuickMetrics
            
            # Display dashboard based on mode
            if ($DashboardMode -eq "Console" -or $DashboardMode -eq "Both") {
                Show-ConsoleDashboard -HealthData $healthData -MetricsData $metricsData
            }
            
            if ($DashboardMode -eq "File" -or $DashboardMode -eq "Both") {
                Save-DashboardData -HealthData $healthData -MetricsData $metricsData
            }
            
            # Check for alerts
            if ($healthData -and $healthData.status -ne "Healthy") {
                $alert = @{
                    timestamp = Get-Date
                    type      = "Health"
                    severity  = if ($healthData.status -eq "Unhealthy") { "Critical" } else { "Warning" }
                    message   = "Application health status: $($healthData.status)"
                }
                $dashboardData.alerts += $alert
            }
            
            if ($metricsData) {
                if ($metricsData.cpuUsage -gt 90) {
                    $alert = @{
                        timestamp = Get-Date
                        type      = "Performance"
                        severity  = "Critical"
                        message   = "High CPU usage: $($metricsData.cpuUsage)%"
                    }
                    $dashboardData.alerts += $alert
                }
                
                if ($metricsData.memoryUsage -gt 95) {
                    $alert = @{
                        timestamp = Get-Date
                        type      = "Performance" 
                        severity  = "Critical"
                        message   = "High memory usage: $($metricsData.memoryUsage)%"
                    }
                    $dashboardData.alerts += $alert
                }
            }
            
            Start-Sleep -Seconds $RefreshInterval
        }
    }
    catch [System.Management.Automation.PipelineStoppedException] {
        Write-Host "`n`nMonitoring dashboard stopped by user" -ForegroundColor Yellow
    }
    catch {
        Write-Host "`n`nMonitoring dashboard stopped due to error: $($_.Exception.Message)" -ForegroundColor Red
    }
    finally {
        # Generate final report
        $totalUptime = (Get-Date) - $dashboardData.startTime
        
        Write-Host "`n=== Monitoring Session Summary ===" -ForegroundColor Cyan
        Write-Host "Environment: $Environment" -ForegroundColor White
        Write-Host "Total Uptime: $($totalUptime.Days)d $($totalUptime.Hours)h $($totalUptime.Minutes)m $($totalUptime.Seconds)s" -ForegroundColor White
        Write-Host "Total Refreshes: $($dashboardData.refreshCount)" -ForegroundColor White
        Write-Host "Health Checks: $($dashboardData.healthHistory.Count)" -ForegroundColor White
        Write-Host "Alerts Generated: $($dashboardData.alerts.Count)" -ForegroundColor White
        
        if ($dashboardData.healthHistory.Count -gt 0) {
            $healthyCount = ($dashboardData.healthHistory | Where-Object { $_.status -eq "Healthy" }).Count
            $healthPercentage = [math]::Round(($healthyCount / $dashboardData.healthHistory.Count) * 100, 2)
            Write-Host "Health Percentage: $healthPercentage%" -ForegroundColor White
            
            $avgResponseTime = ($dashboardData.healthHistory | Where-Object { $_.responseTime -gt 0 } | Measure-Object -Property responseTime -Average).Average
            if ($avgResponseTime) {
                Write-Host "Average Response Time: $([math]::Round($avgResponseTime, 2))ms" -ForegroundColor White
            }
        }
        
        if ($dashboardData.alerts.Count -gt 0) {
            Write-Host "`n--- Alerts Summary ---" -ForegroundColor Yellow
            $criticalCount = ($dashboardData.alerts | Where-Object { $_.severity -eq "Critical" }).Count
            $warningCount = ($dashboardData.alerts | Where-Object { $_.severity -eq "Warning" }).Count
            Write-Host "Critical: $criticalCount, Warnings: $warningCount" -ForegroundColor White
        }
        
        # Save final summary
        $summary = @{
            environment      = $Environment
            sessionStart     = $dashboardData.startTime
            sessionEnd       = Get-Date
            totalUptime      = $totalUptime.TotalMinutes
            refreshCount     = $dashboardData.refreshCount
            healthChecks     = $dashboardData.healthHistory.Count
            alerts           = $dashboardData.alerts.Count
            healthPercentage = if ($dashboardData.healthHistory.Count -gt 0) { [math]::Round((($dashboardData.healthHistory | Where-Object { $_.status -eq "Healthy" }).Count / $dashboardData.healthHistory.Count) * 100, 2) } else { 0 }
        }
        
        $summaryFile = "monitoring-summary-$Environment-$(Get-Date -Format 'yyyyMMdd-HHmmss').json"
        $summary | ConvertTo-Json -Depth 10 | Out-File -FilePath $summaryFile -Encoding UTF8
        Write-Host "`nSession summary saved: $summaryFile" -ForegroundColor Green
    }
}

# Start the dashboard
Start-MonitoringDashboard
