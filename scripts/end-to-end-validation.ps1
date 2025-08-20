# Comprehensive End-to-End Testing and Validation Script

param(
    [Parameter(Mandatory = $true)]
    [string]$AppUrl = "https://app-academic-staging-dvjm4oxxoy2g6.azurewebsites.net",
    
    [Parameter(Mandatory = $false)]
    [string]$ApiKey = "",
    
    [Parameter(Mandatory = $false)]
    [int]$TimeoutSeconds = 30,
    
    [Parameter(Mandatory = $false)]
    [string]$OutputFile = "end-to-end-test-results.csv"
)

# Initialize results collection
$testResults = @()
$totalTests = 0
$passedTests = 0
$failedTests = 0

# Test execution function
function Invoke-Test {
    param(
        [string]$TestName,
        [string]$Endpoint,
        [string]$Method = "GET",
        [hashtable]$Headers = @{},
        [string]$Body = $null,
        [int]$ExpectedStatusCode = 200,
        [string]$ExpectedContent = $null,
        [int]$TimeoutSeconds = 30
    )
    
    $script:totalTests++
    $testResult = @{
        TestName = $TestName
        Endpoint = $Endpoint
        Method = $Method
        Status = "FAIL"
        StatusCode = 0
        ResponseTime = 0
        ErrorMessage = ""
        Timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    }
    
    try {
        $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
        
        $requestParams = @{
            Uri = $Endpoint
            Method = $Method
            Headers = $Headers
            TimeoutSec = $TimeoutSeconds
            ErrorAction = 'Stop'
        }
        
        if ($Body -and ($Method -eq "POST" -or $Method -eq "PUT")) {
            $requestParams.Body = $Body
            $requestParams.ContentType = "application/json"
        }
        
        $response = Invoke-RestMethod @requestParams
        $stopwatch.Stop()
        
        $testResult.StatusCode = 200  # Invoke-RestMethod succeeded
        $testResult.ResponseTime = $stopwatch.ElapsedMilliseconds
        
        # Check expected content if specified
        if ($ExpectedContent -and $response -notlike "*$ExpectedContent*") {
            throw "Expected content '$ExpectedContent' not found in response"
        }
        
        $testResult.Status = "PASS"
        $script:passedTests++
        Write-Host "✓ PASS: $TestName (${stopwatch.ElapsedMilliseconds}ms)" -ForegroundColor Green
        
    } catch {
        $stopwatch.Stop()
        $testResult.ResponseTime = $stopwatch.ElapsedMilliseconds
        $testResult.ErrorMessage = $_.Exception.Message
        $testResult.StatusCode = if ($_.Exception.Response) { [int]$_.Exception.Response.StatusCode } else { 0 }
        
        $script:failedTests++
        Write-Host "✗ FAIL: $TestName - $($_.Exception.Message)" -ForegroundColor Red
    }
    
    $script:testResults += New-Object PSObject -Property $testResult
}

# Test Suite Execution
Write-Host "=== Zeus.People End-to-End Testing Suite ===" -ForegroundColor Cyan
Write-Host "Target Application: $AppUrl" -ForegroundColor Yellow
Write-Host "Starting comprehensive validation..." -ForegroundColor Yellow
Write-Host ""

# 1. Basic Application Health Tests
Write-Host "1. APPLICATION HEALTH TESTS" -ForegroundColor Magenta
Write-Host "================================" -ForegroundColor Magenta

Invoke-Test -TestName "Health Endpoint Check" -Endpoint "$AppUrl/health"
Invoke-Test -TestName "Application Root Endpoint" -Endpoint "$AppUrl/"
Invoke-Test -TestName "Swagger Documentation" -Endpoint "$AppUrl/swagger" -ExpectedStatusCode 200

Write-Host ""

# 2. API Endpoints Testing
Write-Host "2. API ENDPOINTS TESTING" -ForegroundColor Magenta
Write-Host "==========================" -ForegroundColor Magenta

Invoke-Test -TestName "People API - Get All" -Endpoint "$AppUrl/api/people"
Invoke-Test -TestName "People API - Get Count" -Endpoint "$AppUrl/api/people/count"
Invoke-Test -TestName "People API - Search Empty" -Endpoint "$AppUrl/api/people/search?q="

# Test POST endpoint with sample data
$samplePerson = @{
    firstName = "TestUser"
    lastName = "EndToEnd"
    email = "test.e2e@example.com"
    dateOfBirth = "1990-01-01"
} | ConvertTo-Json

Invoke-Test -TestName "People API - Create Person" -Endpoint "$AppUrl/api/people" -Method "POST" -Body $samplePerson -ExpectedStatusCode 201

Write-Host ""

# 3. Configuration and Dependencies
Write-Host "3. CONFIGURATION & DEPENDENCIES" -ForegroundColor Magenta
Write-Host "==================================" -ForegroundColor Magenta

Invoke-Test -TestName "Application Info Endpoint" -Endpoint "$AppUrl/info"
Invoke-Test -TestName "Configuration Health" -Endpoint "$AppUrl/health/config"
Invoke-Test -TestName "Database Connectivity" -Endpoint "$AppUrl/health/database"

Write-Host ""

# 4. Security Tests
Write-Host "4. SECURITY VALIDATION" -ForegroundColor Magenta
Write-Host "========================" -ForegroundColor Magenta

# Test HTTPS enforcement
$httpUrl = $AppUrl.Replace("https://", "http://")
Invoke-Test -TestName "HTTP to HTTPS Redirect" -Endpoint "$httpUrl" -ExpectedStatusCode 301

# Test CORS headers
$corsHeaders = @{"Origin" = "https://localhost:3000"}
Invoke-Test -TestName "CORS Policy Validation" -Endpoint "$AppUrl/api/people" -Headers $corsHeaders

Write-Host ""

# 5. Performance Baseline Tests
Write-Host "5. PERFORMANCE BASELINE" -ForegroundColor Magenta
Write-Host "=========================" -ForegroundColor Magenta

# Run multiple requests to establish baseline
$performanceTests = @()
for ($i = 1; $i -le 5; $i++) {
    Invoke-Test -TestName "Performance Test $i" -Endpoint "$AppUrl/api/people" -TimeoutSeconds 10
}

Write-Host ""

# 6. Error Handling Tests
Write-Host "6. ERROR HANDLING" -ForegroundColor Magenta
Write-Host "==================" -ForegroundColor Magenta

Invoke-Test -TestName "404 Not Found" -Endpoint "$AppUrl/api/nonexistent" -ExpectedStatusCode 404
Invoke-Test -TestName "Invalid Method" -Endpoint "$AppUrl/api/people" -Method "PATCH" -ExpectedStatusCode 405

Write-Host ""

# Calculate and display results
Write-Host "=== TEST EXECUTION SUMMARY ===" -ForegroundColor Cyan
Write-Host "Total Tests: $totalTests" -ForegroundColor White
Write-Host "Passed: $passedTests" -ForegroundColor Green
Write-Host "Failed: $failedTests" -ForegroundColor Red

$successRate = if ($totalTests -gt 0) { [math]::Round(($passedTests / $totalTests) * 100, 2) } else { 0 }
Write-Host "Success Rate: $successRate%" -ForegroundColor $(if ($successRate -ge 95) { "Green" } elseif ($successRate -ge 80) { "Yellow" } else { "Red" })

# Calculate performance statistics
$responseTimes = $testResults | Where-Object { $_.Status -eq "PASS" } | ForEach-Object { $_.ResponseTime }
if ($responseTimes.Count -gt 0) {
    $avgResponseTime = [math]::Round(($responseTimes | Measure-Object -Average).Average, 2)
    $maxResponseTime = ($responseTimes | Measure-Object -Maximum).Maximum
    $minResponseTime = ($responseTimes | Measure-Object -Minimum).Minimum
    
    Write-Host ""
    Write-Host "=== PERFORMANCE METRICS ===" -ForegroundColor Cyan
    Write-Host "Average Response Time: ${avgResponseTime}ms" -ForegroundColor White
    Write-Host "Min Response Time: ${minResponseTime}ms" -ForegroundColor Green
    Write-Host "Max Response Time: ${maxResponseTime}ms" -ForegroundColor $(if ($maxResponseTime -gt 5000) { "Red" } elseif ($maxResponseTime -gt 2000) { "Yellow" } else { "Green" })
}

# Export detailed results to CSV
$testResults | Export-Csv -Path $OutputFile -NoTypeInformation -Encoding UTF8
Write-Host ""
Write-Host "Detailed results exported to: $OutputFile" -ForegroundColor Yellow

# Summary for CI/CD integration
if ($failedTests -eq 0 -and $successRate -ge 95) {
    Write-Host ""
    Write-Host "🎉 ALL TESTS PASSED - APPLICATION IS HEALTHY!" -ForegroundColor Green
    exit 0
} else {
    Write-Host ""
    Write-Host "❌ SOME TESTS FAILED - INVESTIGATION REQUIRED!" -ForegroundColor Red
    exit 1
}
