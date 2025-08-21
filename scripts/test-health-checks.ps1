# Health Check Configuration Status Test Script
# Tests that health checks report configuration status correctly
#
# Prerequisites:
# - Application running with proper configuration
# - Health check endpoints accessible
# - Key Vault access configured

param(
    [Parameter(Mandatory = $false)]
    [string]$BaseUrl = "https://localhost:7001",
    
    [Parameter(Mandatory = $false)]
    [string]$Environment = "Development",
    
    [Parameter(Mandatory = $false)]
    [int]$TimeoutSeconds = 30
)

$ErrorActionPreference = "Stop"

# Logging function
function Write-Log {
    param(
        [string]$Message,
        [string]$Level = "INFO"
    )
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    Write-Host "[$timestamp] [$Level] $Message" -ForegroundColor $(
        switch ($Level) {
            "ERROR" { "Red" }
            "WARN" { "Yellow" }
            "SUCCESS" { "Green" }
            default { "White" }
        }
    )
}

Write-Log "Starting Health Check Configuration Status Tests"
Write-Log "Base URL: $BaseUrl"
Write-Log "Environment: $Environment"

$testResults = @{
    "TotalTests"  = 0
    "PassedTests" = 0
    "FailedTests" = 0
    "Results"     = @()
}

function Add-TestResult {
    param(
        [string]$TestName,
        [bool]$Passed,
        [string]$Message,
        [object]$Data = $null
    )
    
    $testResults.TotalTests++
    
    if ($Passed) {
        $testResults.PassedTests++
        Write-Log "$TestName - PASSED: $Message" -Level "SUCCESS"
    }
    else {
        $testResults.FailedTests++
        Write-Log "$TestName - FAILED: $Message" -Level "ERROR"
    }
    
    $testResults.Results += @{
        "TestName"  = $TestName
        "Passed"    = $Passed
        "Message"   = $Message
        "Data"      = $Data
        "Timestamp" = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    }
}

# Function to make HTTP requests with proper error handling
function Invoke-HealthCheckRequest {
    param(
        [string]$Url,
        [int]$TimeoutSec = 30
    )
    
    try {
        $response = Invoke-RestMethod -Uri $Url -Method Get -TimeoutSec $TimeoutSec -SkipCertificateCheck:$true
        return @{
            "Success"    = $true
            "Data"       = $response
            "StatusCode" = 200
            "Error"      = $null
        }
    }
    catch {
        $statusCode = if ($_.Exception.Response) { [int]$_.Exception.Response.StatusCode } else { 0 }
        return @{
            "Success"    = $false
            "Data"       = $null
            "StatusCode" = $statusCode
            "Error"      = $_.Exception.Message
        }
    }
}

try {
    # Test 1: Basic Health Check Endpoint Accessibility
    Write-Log "Test 1: Verifying basic health check endpoint accessibility..."
    
    $healthUrl = "$BaseUrl/health"
    $healthResponse = Invoke-HealthCheckRequest -Url $healthUrl -TimeoutSec $TimeoutSeconds
    
    if ($healthResponse.Success) {
        Add-TestResult "Health Check Endpoint Accessibility" $true "Health check endpoint is accessible"
        $healthData = $healthResponse.Data
    }
    else {
        Add-TestResult "Health Check Endpoint Accessibility" $false "Cannot access health check endpoint: $($healthResponse.Error)"
        throw "Cannot continue without accessible health endpoint"
    }
    
    # Test 2: Health Check Response Structure Validation
    Write-Log "Test 2: Validating health check response structure..."
    
    try {
        $requiredFields = @("status", "totalDuration", "timestamp", "results")
        $missingFields = @()
        
        foreach ($field in $requiredFields) {
            if (-not $healthData.PSObject.Properties.Name.Contains($field)) {
                $missingFields += $field
            }
        }
        
        if ($missingFields.Count -eq 0) {
            Add-TestResult "Health Check Response Structure" $true "All required fields present in health check response"
        }
        else {
            Add-TestResult "Health Check Response Structure" $false "Missing fields: $($missingFields -join ', ')"
        }
    }
    catch {
        Add-TestResult "Health Check Response Structure" $false "Error validating response structure: $($_.Exception.Message)"
    }
    
    # Test 3: Configuration Status Health Check Validation
    Write-Log "Test 3: Validating configuration status health check..."
    
    try {
        $configHealthCheck = $healthData.results."Configuration Status"
        
        if ($configHealthCheck) {
            $configStatus = $configHealthCheck.status
            $configDescription = $configHealthCheck.description
            
            if ($configStatus -eq "Healthy") {
                Add-TestResult "Configuration Status Health Check" $true "Configuration status is healthy: $configDescription"
            }
            elseif ($configStatus -eq "Degraded") {
                Add-TestResult "Configuration Status Health Check" $false "Configuration status is degraded: $configDescription"
            }
            elseif ($configStatus -eq "Unhealthy") {
                Add-TestResult "Configuration Status Health Check" $false "Configuration status is unhealthy: $configDescription"
            }
            else {
                Add-TestResult "Configuration Status Health Check" $false "Unknown configuration status: $configStatus"
            }
            
            # Log configuration details if available
            if ($configHealthCheck.data) {
                Write-Log "Configuration details: $($configHealthCheck.data | ConvertTo-Json -Compress)" -Level "INFO"
            }
        }
        else {
            Add-TestResult "Configuration Status Health Check" $false "Configuration status health check not found in response"
        }
    }
    catch {
        Add-TestResult "Configuration Status Health Check" $false "Error validating configuration status: $($_.Exception.Message)"
    }
    
    # Test 4: Database Configuration Health Check
    Write-Log "Test 4: Validating database configuration health check..."
    
    try {
        $dbHealthCheck = $healthData.results."Database Configuration"
        
        if ($dbHealthCheck) {
            $dbStatus = $dbHealthCheck.status
            $dbDescription = $dbHealthCheck.description
            
            if ($dbStatus -eq "Healthy") {
                Add-TestResult "Database Configuration Health Check" $true "Database configuration is healthy: $dbDescription"
            }
            else {
                Add-TestResult "Database Configuration Health Check" $false "Database configuration issues: $dbStatus - $dbDescription"
            }
        }
        else {
            Write-Log "Database configuration health check not found (may not be implemented)" -Level "WARN"
            Add-TestResult "Database Configuration Health Check" $true "Database health check not implemented (acceptable)"
        }
    }
    catch {
        Add-TestResult "Database Configuration Health Check" $false "Error validating database configuration: $($_.Exception.Message)"
    }
    
    # Test 5: Service Bus Configuration Health Check
    Write-Log "Test 5: Validating Service Bus configuration health check..."
    
    try {
        $sbHealthCheck = $healthData.results."Service Bus Configuration"
        
        if ($sbHealthCheck) {
            $sbStatus = $sbHealthCheck.status
            $sbDescription = $sbHealthCheck.description
            
            if ($sbStatus -eq "Healthy") {
                Add-TestResult "Service Bus Configuration Health Check" $true "Service Bus configuration is healthy: $sbDescription"
            }
            else {
                Add-TestResult "Service Bus Configuration Health Check" $false "Service Bus configuration issues: $sbStatus - $sbDescription"
            }
        }
        else {
            Write-Log "Service Bus configuration health check not found (may not be implemented)" -Level "WARN"
            Add-TestResult "Service Bus Configuration Health Check" $true "Service Bus health check not implemented (acceptable)"
        }
    }
    catch {
        Add-TestResult "Service Bus Configuration Health Check" $false "Error validating Service Bus configuration: $($_.Exception.Message)"
    }
    
    # Test 6: Key Vault Configuration Health Check
    Write-Log "Test 6: Validating Key Vault configuration health check..."
    
    try {
        $kvHealthCheck = $healthData.results."Key Vault Configuration"
        
        if ($kvHealthCheck) {
            $kvStatus = $kvHealthCheck.status
            $kvDescription = $kvHealthCheck.description
            
            if ($kvStatus -eq "Healthy") {
                Add-TestResult "Key Vault Configuration Health Check" $true "Key Vault configuration is healthy: $kvDescription"
            }
            else {
                Add-TestResult "Key Vault Configuration Health Check" $false "Key Vault configuration issues: $kvStatus - $kvDescription"
            }
        }
        else {
            Write-Log "Key Vault configuration health check not found (may not be implemented)" -Level "WARN"
            Add-TestResult "Key Vault Configuration Health Check" $true "Key Vault health check not implemented (acceptable)"
        }
    }
    catch {
        Add-TestResult "Key Vault Configuration Health Check" $false "Error validating Key Vault configuration: $($_.Exception.Message)"
    }
    
    # Test 7: Overall Health Status Validation
    Write-Log "Test 7: Validating overall health status..."
    
    try {
        $overallStatus = $healthData.status
        $validStatuses = @("Healthy", "Degraded", "Unhealthy")
        
        if ($validStatuses -contains $overallStatus) {
            if ($overallStatus -eq "Healthy") {
                Add-TestResult "Overall Health Status" $true "Overall health status is healthy"
            }
            elseif ($overallStatus -eq "Degraded") {
                Add-TestResult "Overall Health Status" $false "Overall health status is degraded - some issues present"
            }
            else {
                Add-TestResult "Overall Health Status" $false "Overall health status is unhealthy - critical issues present"
            }
        }
        else {
            Add-TestResult "Overall Health Status" $false "Invalid overall health status: $overallStatus"
        }
    }
    catch {
        Add-TestResult "Overall Health Status" $false "Error validating overall health status: $($_.Exception.Message)"
    }
    
    # Test 8: Health Check Response Time Validation
    Write-Log "Test 8: Validating health check response time..."
    
    try {
        $startTime = Get-Date
        $responseTimeTest = Invoke-HealthCheckRequest -Url $healthUrl -TimeoutSec 5
        $endTime = Get-Date
        $responseTime = ($endTime - $startTime).TotalMilliseconds
        
        if ($responseTimeTest.Success) {
            if ($responseTime -lt 5000) {
                # Less than 5 seconds
                Add-TestResult "Health Check Response Time" $true "Response time is acceptable: ${responseTime}ms"
            }
            else {
                Add-TestResult "Health Check Response Time" $false "Response time is too slow: ${responseTime}ms"
            }
        }
        else {
            Add-TestResult "Health Check Response Time" $false "Health check request failed during response time test"
        }
    }
    catch {
        Add-TestResult "Health Check Response Time" $false "Error measuring response time: $($_.Exception.Message)"
    }
    
    # Test 9: Health Check Tags Validation
    Write-Log "Test 9: Validating health check tags..."
    
    try {
        $hasConfigurationTags = $false
        
        foreach ($healthCheckName in $healthData.results.PSObject.Properties.Name) {
            $healthCheck = $healthData.results.$healthCheckName
            
            if ($healthCheck.tags -and $healthCheck.tags -contains "configuration") {
                $hasConfigurationTags = $true
                break
            }
        }
        
        if ($hasConfigurationTags) {
            Add-TestResult "Health Check Tags" $true "Configuration-related health checks are properly tagged"
        }
        else {
            Add-TestResult "Health Check Tags" $false "No configuration tags found in health checks"
        }
    }
    catch {
        Add-TestResult "Health Check Tags" $false "Error validating health check tags: $($_.Exception.Message)"
    }
    
    # Test 10: Configuration Error Detection
    Write-Log "Test 10: Testing configuration error detection..."
    
    try {
        # Look for any health checks that indicate configuration errors
        $configErrors = @()
        
        foreach ($healthCheckName in $healthData.results.PSObject.Properties.Name) {
            $healthCheck = $healthData.results.$healthCheckName
            
            if ($healthCheck.status -eq "Unhealthy" -and $healthCheck.description -match "configuration") {
                $configErrors += "$healthCheckName : $($healthCheck.description)"
            }
        }
        
        if ($configErrors.Count -eq 0) {
            Add-TestResult "Configuration Error Detection" $true "No configuration errors detected in health checks"
        }
        else {
            Add-TestResult "Configuration Error Detection" $false "Configuration errors detected: $($configErrors -join '; ')"
        }
    }
    catch {
        Add-TestResult "Configuration Error Detection" $false "Error checking for configuration errors: $($_.Exception.Message)"
    }
    
    # Generate comprehensive health check test report
    Write-Log ""
    Write-Log "HEALTH CHECK CONFIGURATION STATUS REPORT" -Level "SUCCESS"
    Write-Log "========================================" -Level "SUCCESS"
    Write-Log "Base URL: $BaseUrl" -Level "SUCCESS"
    Write-Log "Environment: $Environment" -Level "SUCCESS"
    Write-Log "Test Execution Time: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -Level "SUCCESS"
    Write-Log ""
    Write-Log "OVERALL HEALTH STATUS: $($healthData.status)" -Level $(if ($healthData.status -eq "Healthy") { "SUCCESS" } else { "WARN" })
    Write-Log ""
    Write-Log "TEST SUMMARY:" -Level "SUCCESS"
    Write-Log "Total Tests: $($testResults.TotalTests)" -Level "SUCCESS"
    Write-Log "Passed: $($testResults.PassedTests)" -Level "SUCCESS"
    Write-Log "Failed: $($testResults.FailedTests)" -Level "SUCCESS"
    Write-Log "Success Rate: $([Math]::Round(($testResults.PassedTests / $testResults.TotalTests) * 100, 2))%" -Level "SUCCESS"
    
    # Display available health checks
    Write-Log ""
    Write-Log "AVAILABLE HEALTH CHECKS:" -Level "SUCCESS"
    foreach ($healthCheckName in $healthData.results.PSObject.Properties.Name) {
        $healthCheck = $healthData.results.$healthCheckName
        $status = $healthCheck.status
        $description = $healthCheck.description
        $color = switch ($status) {
            "Healthy" { "SUCCESS" }
            "Degraded" { "WARN" }
            "Unhealthy" { "ERROR" }
            default { "INFO" }
        }
        Write-Log "[$status] $healthCheckName : $description" -Level $color
    }
    
    # Display detailed test results
    Write-Log ""
    Write-Log "DETAILED TEST RESULTS:" -Level "SUCCESS"
    foreach ($result in $testResults.Results) {
        $status = if ($result.Passed) { "PASS" } else { "FAIL" }
        $color = if ($result.Passed) { "SUCCESS" } else { "ERROR" }
        Write-Log "[$status] $($result.TestName): $($result.Message)" -Level $color
    }
    
    if ($testResults.FailedTests -gt 0) {
        Write-Log ""
        Write-Log "RECOMMENDATIONS:" -Level "WARN"
        foreach ($result in $testResults.Results) {
            if (-not $result.Passed) {
                Write-Log "- Address issue with $($result.TestName): $($result.Message)" -Level "WARN"
            }
        }
    }
    
    # Export test results and health data
    $reportFile = "health-check-results-$Environment-$(Get-Date -Format 'yyyyMMdd-HHmmss').json"
    $fullReport = @{
        "TestResults"     = $testResults
        "HealthCheckData" = $healthData
        "Environment"     = $Environment
        "BaseUrl"         = $BaseUrl
        "Timestamp"       = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    }
    
    $fullReport | ConvertTo-Json -Depth 10 | Out-File -FilePath $reportFile -Encoding UTF8
    Write-Log "Test results and health data exported to: $reportFile" -Level "SUCCESS"
    
    if ($testResults.FailedTests -eq 0 -and $healthData.status -eq "Healthy") {
        Write-Log "All health check tests passed and application is healthy!" -Level "SUCCESS"
        exit 0
    }
    elseif ($testResults.FailedTests -eq 0) {
        Write-Log "All health check tests passed but application health is: $($healthData.status)" -Level "WARN"
        exit 1
    }
    else {
        Write-Log "Some health check tests failed. Please review and address the issues." -Level "WARN"
        exit 1
    }
}
catch {
    Write-Log "Critical error during health check testing: $($_.Exception.Message)" -Level "ERROR"
    exit 1
}
