# Quick Staging Deployment Test
# Simple local test to validate staging deployment readiness
# Duration: Quick validation test (5-10 minutes)

param(
    [Parameter(Mandatory = $false)]
    [string]$BaseUrl = "https://zeus-people-staging.azurewebsites.net",
    
    [Parameter(Mandatory = $false)]
    [switch]$SkipHealthCheck,
    
    [Parameter(Mandatory = $false)]
    [switch]$ShowDetails
)

$ErrorActionPreference = "Stop"

function Write-TestLog {
    param([string]$Message, [string]$Level = "INFO")
    $timestamp = Get-Date -Format "HH:mm:ss"
    $color = switch ($Level) {
        "SUCCESS" { "Green" }
        "ERROR" { "Red" }
        "WARNING" { "Yellow" }
        default { "White" }
    }
    Write-Host "[$timestamp] [$Level] $Message" -ForegroundColor $color
}

function Test-StagingReadiness {
    Write-TestLog "Starting quick staging deployment test..." "INFO"
    Write-TestLog "Target URL: $BaseUrl" "INFO"
    
    $testResults = @{
        HealthCheck    = $false
        ApiResponsive  = $false
        Authentication = $false
        OverallPass    = $false
    }
    
    try {
        # Test 1: Health Check
        if (-not $SkipHealthCheck) {
            Write-TestLog "Testing health endpoint..." "INFO"
            try {
                $healthResponse = Invoke-RestMethod -Uri "$BaseUrl/health" -Method Get -TimeoutSec 10
                if ($healthResponse.status -eq "Healthy") {
                    Write-TestLog "✓ Health check passed" "SUCCESS"
                    $testResults.HealthCheck = $true
                }
                else {
                    Write-TestLog "✗ Health check failed - Status: $($healthResponse.status)" "ERROR"
                }
            }
            catch {
                Write-TestLog "✗ Health endpoint not accessible: $($_.Exception.Message)" "ERROR"
            }
        }
        else {
            Write-TestLog "Skipping health check as requested" "WARNING"
            $testResults.HealthCheck = $true
        }
        
        # Test 2: API Responsiveness
        Write-TestLog "Testing API responsiveness..." "INFO"
        try {
            $academicsResponse = Invoke-RestMethod -Uri "$BaseUrl/api/academics" -Method Get -TimeoutSec 10
            if ($academicsResponse) {
                Write-TestLog "✓ API endpoints are responsive" "SUCCESS"
                $testResults.ApiResponsive = $true
            }
        }
        catch {
            Write-TestLog "✗ API endpoints not responsive: $($_.Exception.Message)" "ERROR"
        }
        
        # Test 3: Authentication (should return 401 for protected endpoints)
        Write-TestLog "Testing authentication..." "INFO"
        try {
            try {
                Invoke-RestMethod -Uri "$BaseUrl/api/academics/search" -Method Get -TimeoutSec 10
                Write-TestLog "✗ Authentication test failed - Expected 401 but got success" "ERROR"
            }
            catch {
                if ($_.Exception.Response.StatusCode -eq 401) {
                    Write-TestLog "✓ Authentication working correctly (401 returned)" "SUCCESS"
                    $testResults.Authentication = $true
                }
                else {
                    Write-TestLog "✗ Unexpected authentication response: $($_.Exception.Response.StatusCode)" "ERROR"
                }
            }
        }
        catch {
            Write-TestLog "✗ Authentication test failed: $($_.Exception.Message)" "ERROR"
        }
        
        # Overall assessment
        $passedTests = ($testResults.HealthCheck -and $testResults.ApiResponsive -and $testResults.Authentication)
        $testResults.OverallPass = $passedTests
        
        if ($passedTests) {
            Write-TestLog "✓ All staging readiness tests passed!" "SUCCESS"
            Write-TestLog "Staging environment appears ready for deployment" "SUCCESS"
        }
        else {
            Write-TestLog "✗ Some staging readiness tests failed" "ERROR"
            Write-TestLog "Review the issues above before proceeding with deployment" "WARNING"
        }
        
        return $testResults
    }
    catch {
        Write-TestLog "Staging readiness test failed: $($_.Exception.Message)" "ERROR"
        return $testResults
    }
}

# Run the test
$results = Test-StagingReadiness

# Summary
Write-TestLog "=== STAGING READINESS TEST SUMMARY ===" "INFO"
Write-TestLog "Health Check: $(if($results.HealthCheck){'✓ PASS'}else{'✗ FAIL'})" "INFO"
Write-TestLog "API Responsive: $(if($results.ApiResponsive){'✓ PASS'}else{'✗ FAIL'})" "INFO"
Write-TestLog "Authentication: $(if($results.Authentication){'✓ PASS'}else{'✗ FAIL'})" "INFO"
Write-TestLog "Overall Result: $(if($results.OverallPass){'✓ READY'}else{'✗ NOT READY'})" $(if ($results.OverallPass) { 'SUCCESS' }else { 'ERROR' })

if ($results.OverallPass) {
    exit 0
}
else {
    exit 1
}
