#!/usr/bin/env pwsh

# Test script to validate monitoring and observability implementation
# Based on .github\prompts\4.2-monitoring-and-observability-setup.prompt.md

Write-Host "🔍 Testing Monitoring and Observability Implementation" -ForegroundColor Cyan
Write-Host "=======================================================" -ForegroundColor Cyan

$passed = 0
$failed = 0

function Test-Component {
    param(
        [string]$TestName,
        [scriptblock]$TestLogic
    )
    
    Write-Host "`n🧪 Testing: $TestName" -ForegroundColor Yellow
    try {
        $result = & $TestLogic
        if ($result) {
            Write-Host "   ✅ PASS: $TestName" -ForegroundColor Green
            $script:passed++
        } else {
            Write-Host "   ❌ FAIL: $TestName" -ForegroundColor Red
            $script:failed++
        }
    } catch {
        Write-Host "   ❌ ERROR in $TestName`: $_" -ForegroundColor Red
        $script:failed++
    }
}

# Test 1: Verify Application Insights Configuration
Test-Component "Application Insights Configuration" {
    $configFile = "src/API/Configuration/MonitoringConfiguration.cs"
    if (Test-Path $configFile) {
        $content = Get-Content $configFile -Raw
        return ($content -match "AddApplicationInsightsTelemetry" -and 
                $content -match "AddComprehensiveMonitoring" -and
                $content -match "ICustomMetricsService" -and
                $content -match "IPerformanceMonitoringService")
    }
    return $false
}

# Test 2: Verify Serilog Configuration
Test-Component "Serilog Structured Logging" {
    $programFile = "src/API/Program.cs"
    if (Test-Path $programFile) {
        $content = Get-Content $programFile -Raw
        return ($content -match "UseSerilog" -and 
                $content -match "CreateBootstrapLogger")
    }
    return $false
}

# Test 3: Verify Custom Metrics Service
Test-Component "Custom Metrics Service Implementation" {
    $metricsFile = "src/API/Configuration/MetricsServices.cs"
    if (Test-Path $metricsFile) {
        $content = Get-Content $metricsFile -Raw
        return ($content -match "class CustomMetricsService" -and 
                $content -match "TrackBusinessEvent" -and
                $content -match "IncrementCounter" -and
                $content -match "TrackPerformance")
    }
    return $false
}

# Test 4: Verify Performance Monitoring Service
Test-Component "Performance Monitoring Service" {
    $metricsFile = "src/API/Configuration/MetricsServices.cs"
    if (Test-Path $metricsFile) {
        $content = Get-Content $metricsFile -Raw
        return ($content -match "class PerformanceMonitoringService" -and 
                $content -match "TrackMemoryUsage" -and
                $content -match "TrackThreadPoolMetrics" -and
                $content -match "TrackGCMetrics" -and
                $content -match "TrackHttpRequestMetrics")
    }
    return $false
}

# Test 5: Verify Performance Monitoring Middleware
Test-Component "Performance Monitoring Middleware" {
    $middlewareFile = "src/API/Middleware/PerformanceMonitoringMiddleware.cs"
    if (Test-Path $middlewareFile) {
        $content = Get-Content $middlewareFile -Raw
        return ($content -match "class PerformanceMonitoringMiddleware" -and 
                $content -match "TrackRequest" -and
                $content -match "Stopwatch" -and
                $content -match "X-Correlation-ID")
    }
    return $false
}

# Test 6: Verify Optional Dependency Pattern
Test-Component "Optional TelemetryClient Dependencies" {
    $middlewareFile = "src/API/Middleware/PerformanceMonitoringMiddleware.cs"
    $metricsFile = "src/API/Configuration/MetricsServices.cs"
    
    if ((Test-Path $middlewareFile) -and (Test-Path $metricsFile)) {
        $middlewareContent = Get-Content $middlewareFile -Raw
        $metricsContent = Get-Content $metricsFile -Raw
        
        return ($middlewareContent -match "TelemetryClient\?" -and 
                $middlewareContent -match "_telemetryClient\?" -and
                $metricsContent -match "TelemetryClient\?" -and
                $metricsContent -match "_telemetryClient\?")
    }
    return $false
}

# Test 7: Verify Application Build Success
Test-Component "Application Build Success" {
    Write-Host "   Building application..." -ForegroundColor Gray
    $buildResult = dotnet build --configuration Release --verbosity quiet --nologo 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   Build completed successfully" -ForegroundColor Gray
        return $true
    } else {
        Write-Host "   Build failed: $buildResult" -ForegroundColor Red
        return $false
    }
}

# Test 8: Verify Unit Tests Pass
Test-Component "Unit Tests Execution" {
    Write-Host "   Running unit tests..." -ForegroundColor Gray
    $testResult = dotnet test tests/Zeus.People.API.Tests/Zeus.People.API.Tests.csproj --configuration Release --verbosity quiet --nologo 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   All tests passed" -ForegroundColor Gray
        return $true
    } else {
        Write-Host "   Tests failed: $testResult" -ForegroundColor Red
        return $false
    }
}

# Test 9: Verify Infrastructure Templates
Test-Component "Azure Infrastructure Templates" {
    $alertsFile = "infra/monitoring/alert-rules.bicep"
    $dashboardFile = "infra/monitoring/dashboard.bicep"
    
    if ((Test-Path $alertsFile) -and (Test-Path $dashboardFile)) {
        $alertsContent = Get-Content $alertsFile -Raw
        $dashboardContent = Get-Content $dashboardFile -Raw
        
        return ($alertsContent -match "Microsoft.Insights/metricAlerts" -and
                $dashboardContent -match "Microsoft.Portal/dashboards")
    }
    return $false
}

# Test 10: Verify Deployment Scripts
Test-Component "Deployment Automation Scripts" {
    $deployFile = "scripts/deploy-monitoring.ps1"
    $testFile = "scripts/test-monitoring.ps1"
    
    return ((Test-Path $deployFile) -and (Test-Path $testFile))
}

# Test 11: Verify Application Startup with Monitoring
Test-Component "Application Startup with Monitoring" {
    Write-Host "   Starting application to verify monitoring initialization..." -ForegroundColor Gray
    
    # Start the application in background
    $process = Start-Process -FilePath "dotnet" -ArgumentList @("run", "--project", "src/API", "--configuration", "Release", "--no-build") -PassThru -WindowStyle Hidden
    
    Start-Sleep -Seconds 5
    
    try {
        # Test health endpoint
        $response = Invoke-RestMethod -Uri "http://localhost:5000/health" -Method Get -TimeoutSec 10
        $success = $response -ne $null
    } catch {
        Write-Host "   Health check failed: $_" -ForegroundColor Gray
        $success = $false
    } finally {
        # Stop the application
        if (-not $process.HasExited) {
            $process.Kill()
            $process.WaitForExit()
        }
    }
    
    return $success
}

Write-Host "`n📊 Test Results Summary" -ForegroundColor Cyan
Write-Host "======================" -ForegroundColor Cyan
Write-Host "✅ Passed: $passed" -ForegroundColor Green
Write-Host "❌ Failed: $failed" -ForegroundColor Red
Write-Host "📈 Success Rate: $([Math]::Round(($passed / ($passed + $failed)) * 100, 2))%" -ForegroundColor Cyan

if ($failed -eq 0) {
    Write-Host "`n🎉 All monitoring and observability tests passed!" -ForegroundColor Green
    Write-Host "✨ The implementation meets all requirements from the prompt." -ForegroundColor Green
    exit 0
} else {
    Write-Host "`n⚠️  Some tests failed. Please review and fix the issues." -ForegroundColor Yellow
    exit 1
}
