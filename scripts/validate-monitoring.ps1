# Test and Validate Monitoring Alert Rules
# This script validates that alert rules are properly configured and functional

param(
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroupName = "rg-academic-staging-westus2",
    
    [Parameter(Mandatory = $false)]
    [string]$WebAppName = "app-academic-staging-dvjm4oxxoy2g6",
    
    [Parameter(Mandatory = $false)]
    [string]$ActionGroupName = "zeus-people-staging-alerts"
)

Write-Host "=== Zeus.People Monitoring Validation ===" -ForegroundColor Cyan
Write-Host "Resource Group: $ResourceGroupName" -ForegroundColor Yellow
Write-Host ""

# Initialize test results
$testResults = @()
$totalTests = 0
$passedTests = 0

function Add-TestResult {
    param($TestName, $Status, $Message)
    
    $script:totalTests++
    if ($Status -eq "PASS") { $script:passedTests++ }
    
    $script:testResults += [PSCustomObject]@{
        Test = $TestName
        Status = $Status
        Message = $Message
        Timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    }
    
    $color = if ($Status -eq "PASS") { "Green" } else { "Red" }
    $icon = if ($Status -eq "PASS") { "✅" } else { "❌" }
    Write-Host "$icon $TestName - $Message" -ForegroundColor $color
}

# Test 1: Verify Azure authentication
Write-Host "1. AZURE AUTHENTICATION" -ForegroundColor Magenta
Write-Host "=========================" -ForegroundColor Magenta

try {
    $account = az account show --query "user.name" --output tsv 2>$null
    if ($account) {
        Add-TestResult "Azure Authentication" "PASS" "Logged in as $account"
    } else {
        Add-TestResult "Azure Authentication" "FAIL" "Not authenticated to Azure"
    }
} catch {
    Add-TestResult "Azure Authentication" "FAIL" "Error checking authentication"
}

# Test 2: Verify resource group exists
Write-Host ""
Write-Host "2. RESOURCE GROUP VALIDATION" -ForegroundColor Magenta  
Write-Host "==============================" -ForegroundColor Magenta

try {
    $rg = az group show --name $ResourceGroupName --output json 2>$null | ConvertFrom-Json
    if ($rg) {
        Add-TestResult "Resource Group Exists" "PASS" "Resource group found in $($rg.location)"
    } else {
        Add-TestResult "Resource Group Exists" "FAIL" "Resource group not found"
    }
} catch {
    Add-TestResult "Resource Group Exists" "FAIL" "Error checking resource group"
}

# Test 3: Verify action group configuration
Write-Host ""
Write-Host "3. ACTION GROUP VALIDATION" -ForegroundColor Magenta
Write-Host "===========================" -ForegroundColor Magenta

try {
    $actionGroups = az monitor action-group list --resource-group $ResourceGroupName --query "[?name=='$ActionGroupName']" --output json 2>$null | ConvertFrom-Json
    
    if ($actionGroups.Count -gt 0) {
        $ag = $actionGroups[0]
        Add-TestResult "Action Group Exists" "PASS" "Found action group '$ActionGroupName'"
        
        if ($ag.enabled) {
            Add-TestResult "Action Group Enabled" "PASS" "Action group is enabled"
        } else {
            Add-TestResult "Action Group Enabled" "FAIL" "Action group is disabled"
        }
        
        if ($ag.emailReceivers.Count -gt 0) {
            Add-TestResult "Email Receivers Configured" "PASS" "$($ag.emailReceivers.Count) email receiver(s) configured"
            
            foreach ($receiver in $ag.emailReceivers) {
                $status = if ($receiver.status -eq "Enabled") { "PASS" } else { "FAIL" }
                Add-TestResult "Email Receiver $($receiver.emailAddress)" $status "Status: $($receiver.status)"
            }
        } else {
            Add-TestResult "Email Receivers Configured" "FAIL" "No email receivers configured"
        }
    } else {
        Add-TestResult "Action Group Exists" "FAIL" "Action group '$ActionGroupName' not found"
    }
} catch {
    Add-TestResult "Action Group Validation" "FAIL" "Error validating action group: $($_.Exception.Message)"
}

# Test 4: Verify alert rules
Write-Host ""
Write-Host "4. ALERT RULES VALIDATION" -ForegroundColor Magenta
Write-Host "==========================" -ForegroundColor Magenta

try {
    $alertRules = az monitor metrics alert list --resource-group $ResourceGroupName --output json 2>$null | ConvertFrom-Json
    
    if ($alertRules.Count -gt 0) {
        Add-TestResult "Alert Rules Deployed" "PASS" "$($alertRules.Count) alert rule(s) found"
        
        $expectedAlerts = @(
            "High-Error-Rate-Alert",
            "High-Response-Time-Alert", 
            "High-CPU-Usage-Alert",
            "High-Memory-Usage-Alert",
            "Database-High-DTU-Alert",
            "Application-Availability-Alert"
        )
        
        foreach ($expectedAlert in $expectedAlerts) {
            $alert = $alertRules | Where-Object { $_.name -eq $expectedAlert }
            if ($alert) {
                $status = if ($alert.enabled) { "PASS" } else { "FAIL" }
                $enabledText = if ($alert.enabled) { "Enabled" } else { "Disabled" }
                Add-TestResult "Alert: $expectedAlert" $status "$enabledText (Severity: $($alert.severity))"
            } else {
                Add-TestResult "Alert: $expectedAlert" "FAIL" "Alert rule not found"
            }
        }
    } else {
        Add-TestResult "Alert Rules Deployed" "FAIL" "No alert rules found"
    }
} catch {
    Add-TestResult "Alert Rules Validation" "FAIL" "Error validating alert rules: $($_.Exception.Message)"
}

# Test 5: Verify App Service metrics availability
Write-Host ""
Write-Host "5. METRICS AVAILABILITY" -ForegroundColor Magenta
Write-Host "========================" -ForegroundColor Magenta

try {
    $appService = az webapp show --resource-group $ResourceGroupName --name $WebAppName --output json 2>$null | ConvertFrom-Json
    if ($appService) {
        Add-TestResult "App Service Exists" "PASS" "App Service found and running"
        
        # Test metric availability
        $metrics = az monitor metrics list-definitions --resource "/subscriptions/$((az account show --query id --output tsv))/resourceGroups/$ResourceGroupName/providers/Microsoft.Web/sites/$WebAppName" --output json 2>$null | ConvertFrom-Json
        
        if ($metrics.Count -gt 0) {
            Add-TestResult "Metrics Available" "PASS" "$($metrics.Count) metric definitions available"
            
            $criticalMetrics = @("CpuPercentage", "MemoryPercentage", "ResponseTime", "Http5xx")
            foreach ($metricName in $criticalMetrics) {
                $metric = $metrics | Where-Object { $_.name.value -eq $metricName }
                if ($metric) {
                    Add-TestResult "Metric: $metricName" "PASS" "Metric available"
                } else {
                    Add-TestResult "Metric: $metricName" "FAIL" "Metric not available"
                }
            }
        } else {
            Add-TestResult "Metrics Available" "FAIL" "No metrics available for App Service"
        }
    } else {
        Add-TestResult "App Service Exists" "FAIL" "App Service not found"
    }
} catch {
    Add-TestResult "Metrics Validation" "FAIL" "Error validating metrics: $($_.Exception.Message)"
}

# Test 6: Test alert rule criteria
Write-Host ""
Write-Host "6. ALERT CRITERIA VALIDATION" -ForegroundColor Magenta
Write-Host "=============================" -ForegroundColor Magenta

try {
    foreach ($alert in $alertRules) {
        if ($alert.criteria -and $alert.criteria.allOf) {
            $criterion = $alert.criteria.allOf[0]
            $thresholdInfo = "Threshold: $($criterion.threshold) $($criterion.operator) $($criterion.timeAggregation)"
            Add-TestResult "Alert Criteria: $($alert.name)" "PASS" $thresholdInfo
        } else {
            Add-TestResult "Alert Criteria: $($alert.name)" "FAIL" "No criteria defined"
        }
    }
} catch {
    Add-TestResult "Alert Criteria Validation" "FAIL" "Error validating alert criteria"
}

# Display summary
Write-Host ""
Write-Host "=== VALIDATION SUMMARY ===" -ForegroundColor Cyan
Write-Host "Total Tests: $totalTests" -ForegroundColor White
Write-Host "Passed: $passedTests" -ForegroundColor Green
Write-Host "Failed: $($totalTests - $passedTests)" -ForegroundColor Red

$successRate = if ($totalTests -gt 0) { [math]::Round(($passedTests / $totalTests) * 100, 2) } else { 0 }
Write-Host "Success Rate: $successRate%" -ForegroundColor $(if ($successRate -ge 95) { "Green" } elseif ($successRate -ge 80) { "Yellow" } else { "Red" })

# Export results
$outputFile = "monitoring-validation-results.csv"
$testResults | Export-Csv -Path $outputFile -NoTypeInformation -Encoding UTF8
Write-Host ""
Write-Host "Detailed results exported to: $outputFile" -ForegroundColor Yellow

# Display next steps based on results
Write-Host ""
Write-Host "=== NEXT STEPS ===" -ForegroundColor Cyan

if ($successRate -eq 100) {
    Write-Host "🎉 All monitoring components are configured correctly!" -ForegroundColor Green
    Write-Host "Your application is fully monitored with comprehensive alerts." -ForegroundColor Green
} elseif ($successRate -ge 80) {
    Write-Host "⚠️ Most monitoring components are working, but some issues need attention." -ForegroundColor Yellow
    Write-Host "Review the failed tests above and fix any configuration issues." -ForegroundColor Yellow
} else {
    Write-Host "❌ Significant monitoring issues detected!" -ForegroundColor Red
    Write-Host "Please review and fix the failed components before production deployment." -ForegroundColor Red
}

Write-Host ""
Write-Host "📊 Monitor your application:" -ForegroundColor White
Write-Host "   - Azure Portal: https://portal.azure.com" -ForegroundColor White
Write-Host "   - Application Insights: Search for your App Insights resource" -ForegroundColor White
Write-Host "   - Alert Rules: Monitor → Alerts → Alert rules" -ForegroundColor White

# Return appropriate exit code
if ($successRate -ge 95) {
    exit 0
} else {
    exit 1
}
